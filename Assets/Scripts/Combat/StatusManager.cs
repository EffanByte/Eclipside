using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public enum StatusType { None, Burn, Poison, Freeze, Confusion, Fragile }

public class StatusManager : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool showDebugLogs = true;

    // --- References ---
    private Rigidbody2D rb;
    private MonoBehaviour host;
    private SpriteRenderer spriteRenderer;
    
    // THE FIX: We store a pointer to the host's ReceiveDamage function
    private Action<DamageInfo> applyDamageCallback; 

    // --- State ---
    private Dictionary<StatusType, float> statusEndTimes = new Dictionary<StatusType, float>();
    private Dictionary<StatusType, Coroutine> statusCoroutines = new Dictionary<StatusType, Coroutine>();
    
    // --- Modifiers ---
    public float DamageTakenMultiplier { get; private set; } = 1f;
    public bool IsFrozen { get; private set; } = false;
    public bool IsConfused { get; private set; } = false;
    private bool freezeConfusionThawBonus = false;
    
    private Dictionary<SynergyPair, Action> synergyLibrary;

    // ---------------------------------------------------------
    // INITIALIZATION
    // ---------------------------------------------------------
    
    // Pass the function (Action) you want this script to call when it deals damage
    public void Initialize(Rigidbody2D targetRb, MonoBehaviour coroutineHost, Action<DamageInfo> onDamage, SpriteRenderer targetSprite)
    {
        rb = targetRb;
        host = coroutineHost;
        applyDamageCallback = onDamage; // Store the function pointer
        spriteRenderer = targetSprite;
        InitializeSynergies();
    }

    private void Update()
    {
        CleanupExpiredStatuses();
    }

    // ---------------------------------------------------------
    // PUBLIC API
    // ---------------------------------------------------------

    public void TryAddStatus(StatusType incoming)
    {
        if (incoming == StatusType.None) return;

        if (CheckAndTriggerSynergy(incoming)) return;

        ApplyBaseStatus(incoming);
    }

    public bool HasStatus(StatusType type)
    {
        return statusEndTimes.TryGetValue(type, out float endTime) && Time.time < endTime;
    }


    public void ClearStatus(StatusType type)
    {
        if (statusEndTimes.ContainsKey(type))
        {
            statusEndTimes.Remove(type);
            
            if (statusCoroutines.TryGetValue(type, out Coroutine co))
            {
                if (co != null) host.StopCoroutine(co);
                statusCoroutines.Remove(type);
            }

            if (type == StatusType.Fragile) DamageTakenMultiplier = 1f;
            if (type == StatusType.Freeze) IsFrozen = false;
            if (type == StatusType.Confusion) IsConfused = false;
        }
    }

    // ---------------------------------------------------------
    // SYNERGY LOGIC
    // ---------------------------------------------------------

    private void InitializeSynergies()
    {
        synergyLibrary = new Dictionary<SynergyPair, Action>();

        void AddSyn(StatusType a, StatusType b, Action act) 
        {
            synergyLibrary.Add(new SynergyPair(a, b), act);
        }

        AddSyn(StatusType.Burn, StatusType.Poison, () => {
            Log("SYNERGY: Burn + Poison (Explosion)");
            // Heavy DoT
            StartDot(StatusType.Poison, 5f, 3f);
        });

        AddSyn(StatusType.Burn, StatusType.Freeze, () => {
            Log("SYNERGY: Thermal Shock");
            // Deal instant damage via the callback
            DealDamageToHost(20f, DamageElement.True); 
            ApplyFragile(3f);
        });

        AddSyn(StatusType.Freeze, StatusType.Confusion, () => {
            Log("SYNERGY: Thaw Nightmare");
            ApplyFreeze(3f);
            freezeConfusionThawBonus = true;
        });
        
        // Add other synergies here...
    }

    private bool CheckAndTriggerSynergy(StatusType incoming)
    {
        List<StatusType> currentStatuses = new List<StatusType>(statusEndTimes.Keys);

        foreach (StatusType existing in currentStatuses)
        {
            if (!HasStatus(existing)) continue;

            SynergyPair pair = new SynergyPair(incoming, existing);

            if (synergyLibrary.ContainsKey(pair))
            {
                synergyLibrary[pair].Invoke();
                ClearStatus(existing);
                return true;
            }
        }
        return false;
    }

    // ---------------------------------------------------------
    // STATUS APPLICATIONS
    // ---------------------------------------------------------

    private void ApplyBaseStatus(StatusType type)
    {
        switch (type)
        {
            case StatusType.Burn: StartDot(StatusType.Burn, 2f, 3f); break; 
            case StatusType.Poison: StartDot(StatusType.Poison, 1f, 5f); break; 
            case StatusType.Freeze: ApplyFreeze(3f); break;
            case StatusType.Confusion: ApplyConfusion(3f); break;
            case StatusType.Fragile: ApplyFragile(3f); break;
        }
    }
        public StatusType GetStatusFromElement(DamageElement element)
    {
        return element switch
        {
            DamageElement.Fire => StatusType.Burn,
            DamageElement.Poison => StatusType.Poison,
            DamageElement.Ice => StatusType.Freeze,
            DamageElement.Psychic => StatusType.Confusion,
            _ => StatusType.None
        };
    }
    private void StartDot(StatusType type, float dps, float duration)
    {
        ClearStatus(type); 
        statusEndTimes[type] = Time.time + duration;
        statusCoroutines[type] = host.StartCoroutine(DotRoutine(type, dps, duration));
    }

    private IEnumerator DotRoutine(StatusType type, float dps, float duration)
    {
        float timer = 0f;
        while (timer < duration)
        {
            yield return new WaitForSeconds(1f);
            timer += 1f;
            
            // Deal Damage via Callback
            DealDamageToHost(dps, DamageElement.True); 
        }
        ClearStatus(type);
    }

    // Helper to send damage back to Player/Enemy
    private void DealDamageToHost(float amount, DamageElement element)
    {
        DamageInfo info = new DamageInfo(amount, element, AttackStyle.Environment, transform.position);
        
        // This calls 'ReceiveDamage' on whatever script initialized this manager
        applyDamageCallback?.Invoke(info);
    }

    private void ApplyFreeze(float duration)
    {
        statusEndTimes[StatusType.Freeze] = Time.time + duration;
        IsFrozen = true;
    }

    private void ApplyConfusion(float duration)
    {
        statusEndTimes[StatusType.Confusion] = Time.time + duration;
        IsConfused = true;
    }

    private void ApplyFragile(float duration)
    {
        statusEndTimes[StatusType.Fragile] = Time.time + duration;
        DamageTakenMultiplier = 1.2f;
    }
    public void ChangeDamageMultiplier(float amount)
    {
        DamageTakenMultiplier += amount;
    }
    private void CleanupExpiredStatuses()
    {
        List<StatusType> toRemove = new List<StatusType>();
        
        foreach (var kvp in statusEndTimes)
        {
            if (Time.time >= kvp.Value) toRemove.Add(kvp.Key);
        }

        foreach (var status in toRemove)
        {
            if (status == StatusType.Freeze && freezeConfusionThawBonus)
            {
                freezeConfusionThawBonus = false;
                ApplyConfusion(3f);
            }
            ClearStatus(status);
        }
    }

    private Color GetDamageFlashColor(DamageElement element)
    {
        return element switch
        {
            DamageElement.Fire => new Color(1f, 0.5f, 0f), 
            DamageElement.Poison => Color.green,
            DamageElement.Ice => Color.cyan,
            DamageElement.Psychic => new Color(0.6f, 0.2f, 0.8f),
            _ => Color.red
        };
    }
    public IEnumerator FlashSpriteRoutine(DamageElement element)
    {
        Color flashColor = GetDamageFlashColor(element);
        Color original = Color.white;
        spriteRenderer.color = flashColor;
        yield return new WaitForSeconds(0.1f);
        spriteRenderer.color = original;
    }

    private void Log(string msg) { if(showDebugLogs) Debug.Log($"[StatusManager] {msg}"); }
}