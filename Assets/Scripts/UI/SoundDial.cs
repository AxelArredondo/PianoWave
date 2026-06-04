using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

/// Rotary dial driven by drag distance rather than pointer angle.
/// Dragging right or up increases the value; left or down decreases it.
/// This avoids the instability that occurs when dragging near the center.
/// Attach to the SoundDial GameObject. Pivot must be (0.5, 0.5).
[RequireComponent(typeof(RectTransform))]
public class SoundDial : MonoBehaviour, IDragHandler
{
    [Header("Rotation Range")]
    [Tooltip("Dial rotation (deg) at value 0. Negative = CCW from 12 o'clock.")]
    public float minAngle = -135f;
    [Tooltip("Dial rotation (deg) at value 1. Positive = CW from 12 o'clock.")]
    public float maxAngle = 135f;

    [Header("Sensitivity")]
    [Tooltip("Degrees rotated per pixel dragged. Raise for faster, lower for finer control.")]
    public float sensitivity = 0.5f;

    [Header("Value")]
    [Range(0f, 1f)]
    public float value = 0.5f;

    [Header("Events")]
    public UnityEvent<float> onValueChanged;

    RectTransform _rt;

    void Awake() => _rt = GetComponent<RectTransform>();
    void Start() => ApplyRotation(value);

    public void OnDrag(PointerEventData e)
    {
        // Right drag or upward drag both rotate CW; combine for diagonal support.
        float delta = (e.delta.x - e.delta.y) * sensitivity;

        float dialAngle = Mathf.Lerp(minAngle, maxAngle, value);
        dialAngle = Mathf.Clamp(dialAngle + delta, minAngle, maxAngle);

        float newValue = Mathf.InverseLerp(minAngle, maxAngle, dialAngle);
        if (Mathf.Approximately(newValue, value)) return;

        value = newValue;
        ApplyRotation(value);
        onValueChanged?.Invoke(value);
    }

    // Sets value and rotation without firing onValueChanged — use when syncing from an external source.
    public void SetValueWithoutNotify(float normalizedValue)
    {
        value = Mathf.Clamp01(normalizedValue);
        ApplyRotation(value);
    }

    // Unity Z rotation is CCW-positive, so we negate the CW angle.
    void ApplyRotation(float normalizedValue)
    {
        float angle = Mathf.Lerp(minAngle, maxAngle, normalizedValue);
        _rt.localRotation = Quaternion.Euler(0f, 0f, -angle);
    }
}
