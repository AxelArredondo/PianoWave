using UnityEngine;

/// <summary>
/// Single source of truth for responsive playfield sizing.
/// Detects phone / tablet / PC from raw screen aspect and publishes
/// Fraction and PCColumnFraction for all layout scripts to read.
///
/// Add this component to any persistent GameObject (e.g. the same one
/// that holds PlayfieldViewport or CameraScaler).
/// </summary>
[DefaultExecutionOrder(-100)]
public class PlayfieldLayout : MonoBehaviour
{
    public static PlayfieldLayout Instance { get; private set; }

    [Header("Platform Detection  (screen aspect = width / height)")]
    [Tooltip("Screen aspect strictly below this → phone portrait tier.")]
    public float phoneMaxAspect = 0.65f;

    [Tooltip("Screen aspect strictly above this → PC/widescreen column mode. " +
             "Must match PlayfieldViewport.widescreenThreshold if PlayfieldLayout is absent.")]
    public float widescreenThreshold = 0.75f;

    [Header("Playfield Fraction  (fraction of camera world width)")]
    [Tooltip("Phone portrait: desired total tile area / screen width.")]
    [Range(0.5f, 1.0f)] public float phoneFraction = 0.90f;

    [Tooltip("Tablet / near-square portrait: desired total tile area / screen width.")]
    [Range(0.3f, 1.0f)] public float tabletFraction = 0.70f;

    [Tooltip("PC: desired total tile area / viewport column width (not the full screen).")]
    [Range(0.5f, 1.0f)] public float pcFraction = 0.90f;

    [Header("PC Viewport Column")]
    [Tooltip("PC only: gameplay column width as a fraction of physical screen width. " +
             "Actual playfield screen fraction = pcColumnFraction × pcFraction.")]
    [Range(0.25f, 0.85f)] public float pcColumnFraction = 0.45f;

    // ── Static outputs read by LaneLayout, PlayfieldViewport ──────────────

    /// <summary>Fraction of the camera's visible world width the playfield occupies.</summary>
    public static float Fraction { get; private set; } = 0.90f;

    /// <summary>PC only: gameplay column width as a fraction of physical screen width.</summary>
    public static float PCColumnFraction { get; private set; } = 0.45f;

    /// <summary>True when the screen is wide enough to trigger PC column mode.</summary>
    public static bool IsWidescreen { get; private set; }

    int lastW, lastH;

    void Awake()
    {
        Instance = this;
        Compute();
    }

    void Update()
    {
        if (Screen.width == lastW && Screen.height == lastH) return;
        Compute();
    }

    void Compute()
    {
        lastW = Screen.width;
        lastH = Screen.height;

        float aspect = (float)Screen.width / Screen.height;
        PCColumnFraction = pcColumnFraction;

        if (aspect > widescreenThreshold)
        {
            IsWidescreen = true;
            // Combine column width and tile fraction so LaneLayout works against
            // the full screen width rather than a restricted camera viewport.
            Fraction = pcFraction * pcColumnFraction;
        }
        else if (aspect >= phoneMaxAspect)
        {
            IsWidescreen = false;
            Fraction = tabletFraction;
        }
        else
        {
            IsWidescreen = false;
            Fraction = phoneFraction;
        }
    }
}
