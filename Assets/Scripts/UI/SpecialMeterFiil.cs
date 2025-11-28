using UnityEngine;
using UnityEngine.UI;

public class SpecialMeterFill : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform indicator;

    [Header("Value Settings")]
    [SerializeField] private float minValue = 0f;
    [SerializeField] private float maxValue = 100f;

    // 👇 Add this slider in Inspector
    [SerializeField, Range(0f, 100f)]
    private float inspectorValue = 0f;

    [Header("Position Settings")]
    [SerializeField] private float startX = 185f;
    [SerializeField] private float endX = 0f;

    private float currentValue;

    private void Awake()
    {
        if (indicator == null)
            indicator = GetComponent<RectTransform>();

        SetValue(inspectorValue);
    }

    private void OnValidate()
    {
        // Update in real time when sliding in Inspector
        SetValue(inspectorValue);
    }

    public void SetValue(float value)
    {
        currentValue = Mathf.Clamp(value, minValue, maxValue);
        inspectorValue = currentValue;   // keep inspector in sync
        UpdatePosition();
    }

    public void ResetMeter() => SetValue(minValue);
    public void FillMeter() => SetValue(maxValue);
    public void AddValue(float delta) => SetValue(currentValue + delta);

    private void UpdatePosition()
    {
        if (indicator == null) return;

        float t = Mathf.InverseLerp(minValue, maxValue, currentValue);
        float x = Mathf.Lerp(startX, endX, t);

        Vector2 pos = indicator.anchoredPosition;
        pos.x = x;
        indicator.anchoredPosition = pos;
    }
}
