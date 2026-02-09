using UnityEngine;
using System.Collections;
public class WheelofFate : EventObject
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    protected override void PerformEvent(PlayerController player)
    {
        StartCoroutine(SpinRoutine(transform, 2f)); // Spin for 2 seconds
        ApplyWheelEffect();
    }

    private IEnumerator SpinRoutine(Transform targetTransform, float duration)
    {
        // 1. Setup: Remember where we started
        Quaternion startRotation = targetTransform.rotation;
        
        // 2. Define the Spin: 
        // We want to rotate 360 degrees * 5 times (1800 degrees total)
        // adding this to the start ensures we land back at the "Original Angle" visually.
        Vector3 totalRotationAmount = new Vector3(0, 0, 360f * 5f); 
        
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            
            // 3. Calculate "t" (0.0 to 1.0)
            float t = elapsedTime / duration;

            // 4. Apply "Ease Out Cubic" math
            // This makes 't' change quickly at first and slowly at the end.
            // Formula: 1 - (1 - t)^3
            float easeOutT = 1f - Mathf.Pow(1f - t, 3);

            // 5. Apply the rotation based on the eased time
            // LerpUnclamped allows us to go beyond 360 easily
            targetTransform.rotation = startRotation * Quaternion.Euler(totalRotationAmount * easeOutT);

            yield return null; // Wait for next frame
        }
        // 6. Finish: Snap exactly back to original rotation to fix any tiny float errors
        targetTransform.rotation = startRotation;
    }

    public void ApplyWheelEffect()
    {
        // 1. Define possible effects
        string[] effects = new string[]
        {
            "Buff",
            "Debuff",
            "Hearts",
            "Poison"
        };

        // 2. Randomly select an effect
        int randomIndex = Random.Range(0, effects.Length);
        string selectedEffect = effects[randomIndex];
        Debug.Log($"Wheel of Fate Result: {selectedEffect}");

        // 3. Apply the effect to the player
        switch (selectedEffect)
        {
            case "Buff":
                PlayerController.Instance.ApplyPermanentBuff((StatType)Random.Range(0, System.Enum.GetValues(typeof(StatType)).Length), Random.Range(5, 26)); // Example buff for 10 seconds
                break;
            case "Debuff":
                PlayerController.Instance.ApplyPermanentBuff((StatType)Random.Range(0, System.Enum.GetValues(typeof(StatType)).Length), -Random.Range(5, 26)); // Example debuff for 10 seconds
                break;
            case "Hearts":
                PlayerController.Instance.AddTemporaryHearts(Random.Range(1, 4)); // Example: Add 1-3 temporary hearts
                break;
            case "Poison":
                PlayerController.Instance.TryAddStatus(StatusType.Poison); // Example: Apply poison for 5-25 seconds
                break;
            case "Nothing Happens":
                // No action needed
                break;
        }
    }
}
