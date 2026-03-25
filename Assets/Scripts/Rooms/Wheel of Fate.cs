using UnityEngine;
using System.Collections;
using System;

public class WheelofFate : EventObject
{
    private bool used = false;
    private string uniqueWheelKey;

    protected void Start()
    {
        // 1. Generate a unique key for this specific wheel instance
        uniqueWheelKey = "WheelOfFate_" + Guid.NewGuid().ToString().Substring(0, 8);
    }

    protected override void PerformEvent(PlayerController player)
    {
        if (used) return; // Only allow one spin per wheel
        used = true;

        // 2. Start the spin. The effect is applied AT THE END of this coroutine.
        StartCoroutine(SpinRoutine(transform, 2.5f)); 
    }

    private IEnumerator SpinRoutine(Transform targetTransform, float duration)
    {
        Quaternion startRotation = targetTransform.rotation;
        
        // Let's make it spin 5 times plus a random extra amount so it doesn't 
        // always look like it lands on the exact same pixel.
        float extraSpin = UnityEngine.Random.Range(0f, 360f);
        Vector3 totalRotationAmount = new Vector3(0, 0, (360f * 5f) + extraSpin); 
        
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            
            float t = elapsedTime / duration;
            float easeOutT = 1f - Mathf.Pow(1f - t, 3); // Cubic Ease Out

            targetTransform.rotation = startRotation * Quaternion.Euler(totalRotationAmount * easeOutT);

            yield return null;
        }
        
        // Final snap
        targetTransform.rotation = startRotation * Quaternion.Euler(new Vector3(0, 0, extraSpin));

        // 3. NOW APPLY THE EFFECT (Suspense paid off!)
        ApplyWheelEffect();
    }

    private void ApplyWheelEffect()
    {
        string[] effects = new string[]
        {
          //  "Buff",
         //   "Debuff",
            "Hearts"
           // "Poison"
        };

        int randomIndex = UnityEngine.Random.Range(0, effects.Length);
        string selectedEffect = effects[randomIndex];
        Debug.Log($"[Wheel of Fate] Spin stopped! Result: {selectedEffect}");

        switch (selectedEffect)
        {
            case "Buff":
                // Get a random stat (Excluding complex ones like projectileSpeed if they aren't handled well)
                StatType buffStat = (StatType)UnityEngine.Random.Range(0, 5); 
                
                // Buff logic: Usually 10% to 50% (0.1f to 0.5f)
                float buffAmount = UnityEngine.Random.Range(0.1f, 0.5f);
                
                // We use ApplyBuff (Temporary) so it doesn't break the game permanently.
                // Duration: 30 seconds
                PlayerController.Instance.ApplyBuff(uniqueWheelKey, buffStat, buffAmount, 30f);
                break;

            case "Debuff":
                StatType debuffStat = (StatType)UnityEngine.Random.Range(0, 5);
                
                // Debuff logic: -10% to -30% (-0.1f to -0.3f)
                float debuffAmount = UnityEngine.Random.Range(0.1f, 0.3f);
                
                PlayerController.Instance.ApplyBuff(uniqueWheelKey, debuffStat, -debuffAmount, 30f);
                break;

            case "Hearts":
                // 1 to 3 temporary hearts (10 to 30 HP)
                int hearts = UnityEngine.Random.Range(1, 4);
                PlayerController.Instance.AddTemporaryHearts(hearts * 10f);
                break;

            case "Poison":
                // Triggers the StatusManager Poison DoT
                PlayerController.Instance.TryAddStatus(StatusType.Poison);
                break;
        }
    }
}