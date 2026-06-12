using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// Attach to TVAntennaLeft and TVAntennaRight.
/// Requires the RectTransform pivot to be set to (0.5, 0) — bottom-center.
/// Rotation convention: 0 = straight up, positive = leans left (CCW), negative = leans right (CW).
[RequireComponent(typeof(Image))]
public class AntennaController : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    public enum Side { Left, Right }

    [Header("Config")]
    public Side side;

    [Header("Rotation Limits")]
    [Tooltip("Minimum allowed Z rotation in degrees.\n0 = up  |  +90 = left  |  -90 = right  |  ±180 = down")]
    [Range(-180f, 180f)] public float minAngleZ = 0f;
    [Tooltip("Maximum allowed Z rotation in degrees. Must be greater than Min Angle Z.")]
    [Range(-180f, 180f)] public float maxAngleZ = 90f;

    [Header("Static Audio")]
    [Tooltip("Must match the clip name in AudioManager's Ambient SFX list.")]
    public string staticClipName = "static";

    [Header("Static Intensity Ranges")]
    [Range(0f, 1f)]   public float staticNoiseMin  = 0.04f;
    [Range(0f, 1f)]   public float staticNoiseMax  = 0.50f;
    [Range(0f, 1f)]   public float crtAlphaMin     = 0.30f;
    [Range(0f, 1f)]   public float crtAlphaMax     = 0.90f;
    [Range(0f, 0.4f)] public float crtFlickerMin   = 0.05f;
    [Range(0f, 0.4f)] public float crtFlickerMax   = 0.30f;

    float _targetZ;
    float _currentZ;
    RectTransform _rt;
    MainMenuUISetup _uiSetup;

    static AntennaController _leftInstance;
    static AntennaController _rightInstance;

    void Awake()
    {
        _rt      = GetComponent<RectTransform>();
        _uiSetup = FindFirstObjectByType<MainMenuUISetup>();

        if (side == Side.Left) _leftInstance  = this;
        else                   _rightInstance = this;
    }

    void Start()
    {
        // Spawn at a random correct (tuned) rotation within the allowed range.
        _targetZ  = Random.Range(minAngleZ, maxAngleZ);
        _currentZ = _targetZ;
        ApplyRotation();
    }

    void OnDestroy()
    {
        if (_leftInstance  == this) _leftInstance  = null;
        if (_rightInstance == this) _rightInstance = null;
    }

    public void OnBeginDrag(PointerEventData e) { }

    public void OnDrag(PointerEventData e)
    {
        Vector2 pivotScreen = RectTransformUtility.WorldToScreenPoint(e.pressEventCamera, _rt.position);
        Vector2 dir = e.position - pivotScreen;
        if (dir.sqrMagnitude < 4f) return;

        // Atan2(x, y) = clockwise angle from "up". Negate for Unity's CCW Z convention.
        float angle = -Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg;
        _currentZ = Mathf.Clamp(angle, minAngleZ, maxAngleZ);

        ApplyRotation();
        UpdateStatic();
    }

    void ApplyRotation()
    {
        Vector3 euler = _rt.localEulerAngles;
        euler.z = _currentZ;
        _rt.localEulerAngles = euler;
    }

    void UpdateStatic()
    {
        if (_leftInstance == null || _rightInstance == null) return;

        float leftRange  = Mathf.Max(_leftInstance.maxAngleZ  - _leftInstance.minAngleZ,  0.001f);
        float rightRange = Mathf.Max(_rightInstance.maxAngleZ - _rightInstance.minAngleZ, 0.001f);

        float leftErr  = Mathf.Abs(_leftInstance._currentZ  - _leftInstance._targetZ)  / leftRange;
        float rightErr = Mathf.Abs(_rightInstance._currentZ - _rightInstance._targetZ) / rightRange;

        // Both antennas must be correct — use the worst error.
        float t = Mathf.Max(leftErr, rightErr);

        _uiSetup?.SetStaticIntensity(t, crtAlphaMin, crtAlphaMax, crtFlickerMin, crtFlickerMax);

        AudioManager.Instance?.SetAmbientVolume(
            staticClipName,
            Mathf.Lerp(staticNoiseMin, staticNoiseMax, t));
    }
}
