using UnityEngine;

public class SlidingMeterIndicator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform indicator;   // The circle

    [Header("Value Settings")]
    [SerializeField] private float minValue = 0f;
    [SerializeField] private float maxValue = 100f;

    [Header("Position Settings")]
    [SerializeField] private float startX = 185f;       // X when meter = minValue
    [SerializeField] private float endX = 0f;           // X when meter = maxValue

    private float currentValue;

    private void Awake()
    {
        // If not assigned, assume this script is on the indicator itself
        if (indicator == null)
            indicator = GetComponent<RectTransform>();

        SetValue(minValue); // start empty
    }

    /// <summary>
    /// Set meter value in [minValue, maxValue] and update position.
    /// </summary>
    public void SetValue(float value)
    {
        currentValue = Mathf.Clamp(value, minValue, maxValue);
        UpdatePosition();
    }

    /// <summary>
    /// Clear the meter (move circle back to startX).
    /// </summary>
    public void ResetMeter()
    {
        SetValue(minValue);
    }

    /// <summary>
    /// Fill the meter completely (move circle to endX).
    /// </summary>
    public void FillMeter()
    {
        SetValue(maxValue);
    }

    /// <summary>
    /// Add to the current meter value (can be negative to subtract).
    /// </summary>
    public void AddValue(float delta)
    {
        SetValue(currentValue + delta);
    }

    private void UpdatePosition()
    {
        if (indicator == null) return;

        // Normalize value to 0–1
        float t = Mathf.InverseLerp(minValue, maxValue, currentValue);

        // Lerp X from startX → endX
        float x = Mathf.Lerp(startX, endX, t);

        Vector2 pos = indicator.anchoredPosition;
        pos.x = x;
        indicator.anchoredPosition = pos;
    }
}
