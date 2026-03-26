using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCharacterRuntime : MonoBehaviour
{
    private const string DefaultCharacterId = "D_Eryndor";
    private const string AstridId = "M_Astrid";
    private const string VeyloriaId = "M_Veyloria";
    private const string MaeliraId = "M_Maelira";
    private const string AylithId = "M_Aylith";
    private const string DravenId = "E_Draven";
    private const string ZorynId = "E_Zoryn";
    private const string KaelricId = "E_Kaelric";
    private const string CalyraId = "E_Calyra";
    private const string LunariaId = "E_Lunaria";

    private const float StandardHeartValue = 10f;
    private const float MaeliraMirageChance = 0.15f;
    private const float VeyloriaPoisonCritBonus = 25f;

    private PlayerController player;
    private PlayerHealth health;
    private StatusManager statusManager;
    private CharacterData activeCharacter;
    private float specialCooldownRemaining;
    private float aylithIceHeartDecayTimer;
    private float veyloriaWithdrawalRecoveryUntil = float.NegativeInfinity;
    private CharacterMirageDecoy activeMirage;
    private readonly HashSet<int> toxicEmbraceMarkedTargets = new HashSet<int>();
    private bool initialized;

    public void Initialize(PlayerController controller, PlayerHealth playerHealth, StatusManager playerStatusManager)
    {
        if (initialized)
        {
            return;
        }

        player = controller;
        health = playerHealth;
        statusManager = playerStatusManager;

        EnemyBase.OnEnemyKilled += HandleEnemyKilled;
        if (statusManager != null)
        {
            statusManager.OnStatusApplied += HandleStatusApplied;
            statusManager.OnStatusRemoved += HandleStatusRemoved;
        }

        initialized = true;
    }

    private void OnDestroy()
    {
        EnemyBase.OnEnemyKilled -= HandleEnemyKilled;

        if (statusManager != null)
        {
            statusManager.OnStatusApplied -= HandleStatusApplied;
            statusManager.OnStatusRemoved -= HandleStatusRemoved;
        }
    }

    public void ApplyEquippedCharacter()
    {
        EnsureCharacterSelection();

        SaveFile_Profile profile = SaveManager.Profile;
        string equippedId = profile.characters.equipped_character_id;
        activeCharacter = GameDatabase.Instance != null ? GameDatabase.Instance.GetCharacterByID(equippedId) : null;

        if (activeCharacter == null && GameDatabase.Instance != null)
        {
            activeCharacter = GameDatabase.Instance.GetCharacterByID(DefaultCharacterId);
        }

        if (activeCharacter == null)
        {
            Debug.LogWarning("[Character] Could not load equipped character data.");
            return;
        }

        ApplyCharacterStats();
        ApplyCharacterVisuals();

        specialCooldownRemaining = 0f;
        player.SetSpecialMeterNormalized(1f);

        if (activeCharacter.startingWeapon != null)
        {
            player.EquipWeapon(activeCharacter.startingWeapon);
        }

        Debug.Log($"[Character] Applied {activeCharacter.characterName} ({activeCharacter.characterID}).");
    }

    public void ManualUpdate(float deltaTime)
    {
        if (activeCharacter == null)
        {
            return;
        }

        if (specialCooldownRemaining > 0f)
        {
            specialCooldownRemaining = Mathf.Max(0f, specialCooldownRemaining - deltaTime);
        }

        float cooldown = Mathf.Max(0.01f, activeCharacter.specialCooldownSeconds);
        float fill = activeCharacter.specialCooldownSeconds <= 0f ? 1f : 1f - (specialCooldownRemaining / cooldown);
        player.SetSpecialMeterNormalized(fill);

        if (activeCharacter.characterID == AylithId)
        {
            UpdateAylithIceHearts(deltaTime);
        }
    }

    public bool HandlesSpecialCooldowns() => activeCharacter != null;

    public void TryActivateSpecial()
    {
        if (activeCharacter == null || specialCooldownRemaining > 0f || player.IsDead)
        {
            return;
        }

        if (!ActivateCharacterSpecial())
        {
            return;
        }

        specialCooldownRemaining = Mathf.Max(0f, activeCharacter.specialCooldownSeconds);
        player.SetSpecialMeterNormalized(0f);
        Debug.Log($"[Character] {activeCharacter.characterName} used {GetSpecialName(activeCharacter.characterID)}.");
    }

    public void NotifyDamageDealt(float damageAmount)
    {
    }

    public void NotifyWeaponHit(EnemyBase target, DamageInfo damageInfo)
    {
        if (activeCharacter == null || target == null)
        {
            return;
        }

        float burnBase = statusManager != null ? statusManager.GetBaseDuration(StatusType.Burn) : 3f;
        float poisonBase = statusManager != null ? statusManager.GetBaseDuration(StatusType.Poison) : 5f;
        float freezeBase = statusManager != null ? statusManager.GetBaseDuration(StatusType.Freeze) : 3f;
        float confusionBase = statusManager != null ? statusManager.GetBaseDuration(StatusType.Confusion) : 3f;

        switch (activeCharacter.characterID)
        {
            case AstridId:
                if (player.currentWeapon is LightMeleeWeapon && Random.value <= 0.30f)
                {
                    target.TryAddStatus(StatusType.Burn, GetOutgoingStatusDuration(StatusType.Burn, burnBase));
                }
                break;

            case VeyloriaId:
                if (damageInfo.isCritical)
                {
                    target.TryAddStatus(StatusType.Poison, GetOutgoingStatusDuration(StatusType.Poison, poisonBase));
                }
                break;

            case MaeliraId:
                if (Random.value <= 0.25f)
                {
                    target.TryAddStatus(StatusType.Confusion, GetOutgoingStatusDuration(StatusType.Confusion, confusionBase));
                }
                break;

            case AylithId:
                if (Random.value <= 0.30f)
                {
                    target.TryAddStatus(StatusType.Freeze, GetOutgoingStatusDuration(StatusType.Freeze, freezeBase));
                }
                break;
        }
    }

    public void NotifyDamageTaken(DamageInfo damageInfo, float finalDamage, bool isStatusDamage)
    {
        if (activeCharacter == null || isStatusDamage)
        {
            return;
        }

        if (activeCharacter.characterID == MaeliraId && activeMirage == null && Random.value <= MaeliraMirageChance)
        {
            SpawnMirage();
        }
    }

    public void NotifyDashStarted()
    {
    }

    public void NotifyDashEnded()
    {
    }

    public void NotifyWaveTransition()
    {
    }

    public void NotifyWeaponEquipped(WeaponData weapon)
    {
    }

    public bool CanEquipWeapon(WeaponData weapon)
    {
        if (activeCharacter == null || !activeCharacter.IsWeaponLocked)
        {
            return true;
        }

        return weapon == activeCharacter.startingWeapon;
    }

    public string GetActiveCharacterName()
    {
        return activeCharacter != null ? activeCharacter.characterName : "Unknown";
    }

    public float GetCharacterDamageMultiplierForWeapon(WeaponData weapon)
    {
        if (activeCharacter == null)
        {
            return 1f;
        }

        float multiplier = 1f;

        if (activeCharacter.characterID == VeyloriaId && IsVeyloriaWithdrawalActive() && weapon is LightMeleeWeapon)
        {
            multiplier *= 0.85f;
        }

        if (activeCharacter.characterID == MaeliraId && activeMirage != null)
        {
            multiplier *= 0.90f;
        }

        return multiplier;
    }

    public float GetCharacterAttackSpeedMultiplierForWeapon(WeaponData weapon)
    {
        if (activeCharacter == null)
        {
            return 1f;
        }

        float multiplier = 1f;

        if (activeCharacter.characterID == VeyloriaId && IsVeyloriaWithdrawalActive())
        {
            multiplier *= 0.90f;
        }

        return multiplier;
    }

    public float GetCharacterCriticalChanceForWeapon(WeaponData weapon, float currentChance)
    {
        if (activeCharacter == null)
        {
            return currentChance;
        }

        float finalChance = currentChance;

        if (activeCharacter.characterID == VeyloriaId)
        {
            if (IsVeyloriaWithdrawalActive())
            {
                finalChance *= 0.85f;
            }

            if (statusManager != null && statusManager.HasStatus(StatusType.Poison))
            {
                finalChance += VeyloriaPoisonCritBonus;
            }
        }

        return finalChance;
    }

    public float GetProjectileSpeedMultiplier()
    {
        return 1f;
    }

    public float GetDashDistanceMultiplier()
    {
        return 1f;
    }

    public float GetIncomingDamageMultiplier(DamageInfo damageInfo, bool isStatusDamage)
    {
        if (activeCharacter == null)
        {
            return 1f;
        }

        float multiplier = 1f;

        if (activeCharacter.defense > 0f)
        {
            multiplier *= Mathf.Max(0.01f, 1f - activeCharacter.defense);
        }

        if (isStatusDamage)
        {
            multiplier *= activeCharacter.statusDamageTakenMultiplier;
        }
        else if (damageInfo.style == AttackStyle.Ranged)
        {
            multiplier *= activeCharacter.projectileDamageTakenMultiplier;
        }
        else
        {
            multiplier *= activeCharacter.contactDamageTakenMultiplier;
        }

        switch (damageInfo.element)
        {
            case DamageElement.Fire:
                multiplier *= activeCharacter.fireDamageTakenMultiplier;
                break;
            case DamageElement.Poison:
                multiplier *= activeCharacter.poisonDamageTakenMultiplier;
                break;
            case DamageElement.Ice:
                multiplier *= activeCharacter.iceDamageTakenMultiplier;
                break;
            case DamageElement.Magic:
                multiplier *= activeCharacter.magicDamageTakenMultiplier;
                break;
            case DamageElement.Physical:
                multiplier *= activeCharacter.physicalDamageTakenMultiplier;
                break;
        }

        if (damageInfo.style == AttackStyle.MeleeHeavy)
        {
            multiplier *= activeCharacter.heavyDamageTakenMultiplier;
        }

        if (activeCharacter.characterID == VeyloriaId && statusManager != null && statusManager.HasStatus(StatusType.Poison) && damageInfo.element == DamageElement.Poison)
        {
            multiplier *= 0.50f;
        }

        if (activeCharacter.characterID == MaeliraId && activeMirage != null)
        {
            multiplier *= 1.10f;
        }

        return Mathf.Max(0.01f, multiplier);
    }

    public float GetOutgoingStatusDuration(StatusType statusType, float baseDuration)
    {
        float duration = baseDuration;

        if (activeCharacter != null)
        {
            duration = (duration * Mathf.Max(0.01f, activeCharacter.outgoingStatusDurationMultiplier)) + activeCharacter.outgoingStatusDurationBonusSeconds;
        }

        if (activeCharacter != null && activeCharacter.characterID == VeyloriaId && statusType == StatusType.Poison && statusManager != null && statusManager.HasStatus(StatusType.Poison))
        {
            duration *= 1.25f;
        }

        return Mathf.Max(0.1f, duration);
    }

    public bool TryHandleHealing(float amount)
    {
        if (activeCharacter == null || activeCharacter.characterID != AylithId)
        {
            return false;
        }

        float remaining = Mathf.Max(0f, amount);
        float missingPermanentHealth = Mathf.Max(0f, health.GetMaxHealth() - health.GetCurrentHealth());
        float healToPermanent = Mathf.Min(remaining, missingPermanentHealth);

        if (healToPermanent > 0f)
        {
            health.Heal(healToPermanent);
            remaining -= healToPermanent;
        }

        if (remaining > 0f)
        {
            health.AddTemporaryHeartsCapped(remaining, activeCharacter.temporaryHealthCap);
        }

        return true;
    }

    public bool TryHandleMaxHealthGain(float amount)
    {
        if (activeCharacter == null || activeCharacter.characterID != AylithId || amount <= 0f)
        {
            return false;
        }

        health.AddTemporaryHeartsCapped(amount, activeCharacter.temporaryHealthCap);
        return true;
    }

    public void ResolveMirage(CharacterMirageDecoy mirage, bool destroyedPrematurely, Vector3 position)
    {
        if (mirage == null || mirage != activeMirage)
        {
            return;
        }

        activeMirage = null;
        RetargetEnemies(player.transform);

        if (destroyedPrematurely)
        {
            foreach (EnemyBase enemy in GetEnemiesInRadius(position, 2.25f))
            {
                enemy.TryAddStatus(StatusType.Confusion, 2f);
            }
        }
        else
        {
            player.ApplyBuff("Maelira_AfterimageLag", StatType.Speed, -0.15f, 1.25f);
        }
    }

    private void EnsureCharacterSelection()
    {
        SaveFile_Profile profile = SaveManager.Profile;

        if (!profile.characters.owned_character_ids.Contains(DefaultCharacterId))
        {
            profile.characters.owned_character_ids.Add(DefaultCharacterId);
        }

        if (string.IsNullOrWhiteSpace(profile.characters.equipped_character_id))
        {
            profile.characters.equipped_character_id = DefaultCharacterId;
        }

        if (!profile.characters.owned_character_ids.Contains(profile.characters.equipped_character_id))
        {
            profile.characters.equipped_character_id = DefaultCharacterId;
        }

        SaveManager.SaveProfile();
    }

    private void ApplyCharacterStats()
    {
        if (activeCharacter == null)
        {
            return;
        }

        player.ApplyCharacterBaseStats(activeCharacter);
        aylithIceHeartDecayTimer = 0f;
    }

    private void ApplyCharacterVisuals()
    {
        if (activeCharacter == null)
        {
            return;
        }

        SpriteRenderer spriteRenderer = player.GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null && activeCharacter.inGameSprite != null)
        {
            spriteRenderer.sprite = activeCharacter.inGameSprite;
        }

        if (player.anim != null && activeCharacter.animatorOverride != null)
        {
            player.anim.runtimeAnimatorController = activeCharacter.animatorOverride;
        }
    }

    private void UpdateAylithIceHearts(float deltaTime)
    {
        if (activeCharacter == null || activeCharacter.temporaryHealthDecayInterval <= 0f)
        {
            return;
        }

        if (health.GetTemporaryHealth() <= 0f)
        {
            aylithIceHeartDecayTimer = 0f;
            return;
        }

        aylithIceHeartDecayTimer += deltaTime;
        while (aylithIceHeartDecayTimer >= activeCharacter.temporaryHealthDecayInterval)
        {
            aylithIceHeartDecayTimer -= activeCharacter.temporaryHealthDecayInterval;
            health.RemoveTemporaryHearts(StandardHeartValue);
        }
    }

    private bool ActivateCharacterSpecial()
    {
        switch (activeCharacter.characterID)
        {
            case AstridId:
                return ActivateAstridSpecial();
            case VeyloriaId:
                return ActivateVeyloriaSpecial();
            case MaeliraId:
                return ActivateMaeliraSpecial();
            case AylithId:
                return ActivateAylithSpecial();
            case DravenId:
                return ActivateDravenSpecial();
            case ZorynId:
                return ActivateZorynSpecial();
            case KaelricId:
                return ActivateKaelricSpecial();
            case CalyraId:
                return ActivateCalyraSpecial();
            case LunariaId:
                return ActivateLunariaSpecial();
            default:
                return ActivateEryndorSpecial();
        }
    }

    private bool ActivateAstridSpecial()
    {
        List<EnemyBase> enemies = GetEnemiesInRadius(player.transform.position, 2.75f);
        foreach (EnemyBase enemy in enemies)
        {
            DealDamage(enemy, player.GetBaseDamage() * 1.8f, DamageElement.Fire, AttackStyle.MeleeLight, 2f);
            enemy.TryAddStatus(StatusType.Burn, GetOutgoingStatusDuration(StatusType.Burn, 5f));
        }

        return true;
    }

    private bool ActivateVeyloriaSpecial()
    {
        EnemyBase target = GetNearestEnemy(player.transform.position, 5.5f, player.GetLastMovementDirection());
        if (target == null)
        {
            return false;
        }

        Vector2 pullDestination = (Vector2)player.transform.position + (player.GetLastMovementDirection().normalized * 0.75f);
        target.transform.position = pullDestination;
        DealDamage(target, player.GetBaseDamage() * 2f, DamageElement.Poison, AttackStyle.MeleeLight, 1.5f);
        target.TryAddStatus(StatusType.Poison, GetOutgoingStatusDuration(StatusType.Poison, 6f));
        StartCoroutine(TrackToxicEmbraceTarget(target, 6f));
        return true;
    }

    private bool ActivateMaeliraSpecial()
    {
        Vector2 origin = player.transform.position;
        Vector2 direction = player.GetLastMovementDirection().normalized;
        foreach (EnemyBase enemy in GetEnemiesInRadius(origin, 5.5f))
        {
            Vector2 offset = (Vector2)enemy.transform.position - origin;
            if (offset.sqrMagnitude > 1f && Vector2.Dot(direction, offset.normalized) < 0.15f)
            {
                continue;
            }

            bool wasConfused = enemy.HasStatus(StatusType.Confusion);
            float damage = player.GetBaseDamage() * (wasConfused ? 2.4f : 1.9f);
            DealDamage(enemy, damage, DamageElement.Physical, AttackStyle.MeleeHeavy, 3.5f);

            if (wasConfused || Random.value <= 0.35f)
            {
                enemy.TryAddStatus(StatusType.Confusion, 4f);
            }

            if (wasConfused)
            {
                enemy.ApplyTemporaryMoveSpeedMultiplier(0.70f, 1.25f);
            }
        }

        return true;
    }

    private bool ActivateAylithSpecial()
    {
        foreach (EnemyBase enemy in FindActiveEnemies())
        {
            if (enemy.GetMaxHealth() <= 30f)
            {
                DealDamage(enemy, 9999f, DamageElement.Ice, AttackStyle.Ranged);
            }
            else
            {
                enemy.TryAddStatus(StatusType.Freeze, 5f);
                enemy.ApplyTemporaryMoveSpeedMultiplier(0.60f, 5f);
            }
        }

        return true;
    }

    private bool ActivateDravenSpecial()
    {
        player.ApplyBuff("Draven_Fury_Damage", StatType.BaseDamage, 0.25f, 20f);
        player.ApplyBuff("Draven_Fury_Attack", StatType.AttackSpeed, 0.20f, 20f);
        player.ApplyBuff("Draven_Fury_Speed", StatType.Speed, 0.15f, 20f);
        player.ApplyBuff("Draven_Fury_Health", StatType.MaxHealth, StandardHeartValue, 20f);
        player.Heal(StandardHeartValue);
        return true;
    }

    private bool ActivateZorynSpecial()
    {
        CreateSupportZone(player.transform.position, 2.5f, 10f, 2f, 0.15f);
        return true;
    }

    private bool ActivateKaelricSpecial()
    {
        StartCoroutine(HeroicQuakeRoutine());
        return true;
    }

    private bool ActivateCalyraSpecial()
    {
        StartCoroutine(AgileWhirlwindRoutine());
        return true;
    }

    private bool ActivateLunariaSpecial()
    {
        foreach (EnemyBase enemy in GetEnemiesInRadius(player.transform.position, 2.25f))
        {
            DealDamage(enemy, player.GetBaseDamage() * 1.3f, DamageElement.Physical, AttackStyle.MeleeLight, 1.5f);
        }

        foreach (EnemyBase enemy in GetEnemiesInRadius(player.transform.position, 3.5f))
        {
            DealDamage(enemy, player.GetBaseDamage() * 1.15f, DamageElement.Magic, AttackStyle.Ranged);
            enemy.ApplyTemporaryMoveSpeedMultiplier(0.70f, 2f);
        }

        return true;
    }

    private bool ActivateEryndorSpecial()
    {
        foreach (EnemyBase enemy in GetEnemiesInRadius(player.transform.position, 2.75f))
        {
            DealDamage(enemy, player.GetBaseDamage() * 1.6f, DamageElement.True, AttackStyle.Environment, 2f);

            if (enemy.GetMaxHealth() <= 30f)
            {
                enemy.ForceStun(0.75f);
            }
            else
            {
                enemy.ApplyTemporaryMoveSpeedMultiplier(0.70f, 1.5f);
            }
        }

        return true;
    }

    private IEnumerator HeroicQuakeRoutine()
    {
        float duration = 3f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            foreach (EnemyBase enemy in GetEnemiesInRadius(player.transform.position, 2.8f))
            {
                DealDamage(enemy, player.GetBaseDamage() * 0.7f, DamageElement.Physical, AttackStyle.MeleeHeavy, 2.5f);
                if (enemy.GetMaxHealth() <= 25f)
                {
                    enemy.ForceStun(0.5f);
                }
            }

            elapsed += 0.5f;
            yield return new WaitForSeconds(0.5f);
        }
    }

    private IEnumerator AgileWhirlwindRoutine()
    {
        float elapsed = 0f;
        float duration = 2f;

        while (elapsed < duration)
        {
            foreach (EnemyBase enemy in GetEnemiesInRadius(player.transform.position, 3.2f))
            {
                Vector2 newPosition = Vector2.MoveTowards(enemy.transform.position, player.transform.position, 0.75f);
                enemy.transform.position = newPosition;
                DealDamage(enemy, player.GetBaseDamage() * 0.55f, DamageElement.Physical, AttackStyle.MeleeLight);
            }

            elapsed += 0.35f;
            yield return new WaitForSeconds(0.35f);
        }
    }

    private IEnumerator TrackToxicEmbraceTarget(EnemyBase target, float duration)
    {
        int instanceId = target.GetInstanceID();
        toxicEmbraceMarkedTargets.Add(instanceId);
        yield return new WaitForSeconds(duration);
        toxicEmbraceMarkedTargets.Remove(instanceId);
    }

    private void SpawnMirage()
    {
        if (activeMirage != null)
        {
            return;
        }

        GameObject mirageObject = new GameObject("MaeliraMirage");
        mirageObject.transform.position = player.transform.position;
        CharacterMirageDecoy mirage = mirageObject.AddComponent<CharacterMirageDecoy>();
        mirage.Initialize(this, 4f, 0.6f);
        activeMirage = mirage;
        RetargetEnemies(mirage.transform);
    }

    private void CreateAshZone(Vector3 position)
    {
        GameObject zoneObject = new GameObject("AstridAshZone");
        zoneObject.transform.position = position;
        AstridAshZone zone = zoneObject.AddComponent<AstridAshZone>();
        zone.Initialize(this, player, 5f, 1.5f, 2f, 0.90f);
    }

    private void CreateSupportZone(Vector3 position, float radius, float duration, float healPerSecond, float defenseReduction)
    {
        GameObject zoneObject = new GameObject("ZorynRestorationZone");
        zoneObject.transform.position = position;
        CharacterSupportZone zone = zoneObject.AddComponent<CharacterSupportZone>();
        zone.Initialize(player, radius, duration, healPerSecond, defenseReduction);
    }

    private void HandleEnemyKilled(EnemyBase enemy)
    {
        if (activeCharacter == null || enemy == null)
        {
            return;
        }

        int instanceId = enemy.GetInstanceID();

        if (activeCharacter.characterID == AstridId && enemy.HasStatus(StatusType.Burn))
        {
            CreateAshZone(enemy.transform.position);
        }

        if (activeCharacter.characterID == VeyloriaId && toxicEmbraceMarkedTargets.Remove(instanceId))
        {
            foreach (EnemyBase nearbyEnemy in GetEnemiesInRadius(enemy.transform.position, 2.5f))
            {
                nearbyEnemy.TryAddStatus(StatusType.Poison, GetOutgoingStatusDuration(StatusType.Poison, 4f));
            }
        }
    }

    private void HandleStatusApplied(StatusType statusType)
    {
        if (activeCharacter == null || activeCharacter.characterID != VeyloriaId)
        {
            return;
        }

        if (statusType == StatusType.Poison)
        {
            veyloriaWithdrawalRecoveryUntil = Time.time + 1f;
        }
    }

    private void HandleStatusRemoved(StatusType statusType)
    {
    }

    private bool IsVeyloriaWithdrawalActive()
    {
        if (activeCharacter == null || activeCharacter.characterID != VeyloriaId)
        {
            return false;
        }

        bool isPoisoned = statusManager != null && statusManager.HasStatus(StatusType.Poison);
        return !isPoisoned || Time.time < veyloriaWithdrawalRecoveryUntil;
    }

    private List<EnemyBase> FindActiveEnemies()
    {
        EnemyBase[] allEnemies = FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
        List<EnemyBase> results = new List<EnemyBase>(allEnemies.Length);

        foreach (EnemyBase enemy in allEnemies)
        {
            if (enemy != null && enemy.gameObject.activeInHierarchy)
            {
                results.Add(enemy);
            }
        }

        return results;
    }

    private List<EnemyBase> GetEnemiesInRadius(Vector3 origin, float radius)
    {
        List<EnemyBase> results = new List<EnemyBase>();
        float radiusSqr = radius * radius;

        foreach (EnemyBase enemy in FindActiveEnemies())
        {
            if ((enemy.transform.position - origin).sqrMagnitude <= radiusSqr)
            {
                results.Add(enemy);
            }
        }

        return results;
    }

    private EnemyBase GetNearestEnemy(Vector3 origin, float maxDistance, Vector2 preferredDirection)
    {
        EnemyBase best = null;
        float bestDistance = maxDistance * maxDistance;
        Vector2 preferred = preferredDirection.sqrMagnitude > 0.001f ? preferredDirection.normalized : Vector2.right;

        foreach (EnemyBase enemy in FindActiveEnemies())
        {
            Vector2 offset = enemy.transform.position - origin;
            float sqrDistance = offset.sqrMagnitude;
            if (sqrDistance > bestDistance)
            {
                continue;
            }

            if (offset.sqrMagnitude > 0.01f && Vector2.Dot(preferred, offset.normalized) < -0.2f)
            {
                continue;
            }

            best = enemy;
            bestDistance = sqrDistance;
        }

        return best;
    }

    private void DealDamage(EnemyBase enemy, float amount, DamageElement element, AttackStyle style, float knockback = 0f)
    {
        if (enemy == null)
        {
            return;
        }

        DamageInfo damageInfo = new DamageInfo(
            amount,
            element,
            style,
            player.transform.position,
            knockback);

        enemy.ReceiveDamage(damageInfo);
    }

    private void RetargetEnemies(Transform target)
    {
        foreach (EnemyBase enemy in FindActiveEnemies())
        {
            enemy.SetTargetOverride(target);
        }
    }

    private string GetSpecialName(string characterId)
    {
        switch (characterId)
        {
            case AstridId:
                return "Fiery Tempest";
            case VeyloriaId:
                return "Toxic Embrace";
            case MaeliraId:
                return "Dissonant Impact";
            case AylithId:
                return "Below Zero";
            case DravenId:
                return "Hero's Fury";
            case ZorynId:
                return "Flame of Restoration";
            case KaelricId:
                return "Heroic Quake";
            case CalyraId:
                return "Agile Whirlwind";
            case LunariaId:
                return "Arcane Fury";
            default:
                return "Fragment Impact";
        }
    }
}
