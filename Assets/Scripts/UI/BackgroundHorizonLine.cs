using UnityEngine;

/// <summary>
/// Single source of truth for the background horizon world-space Y.
/// Set horizonFraction to match the ground grid's heightFraction so the
/// grid's top edge meets the city/sun's bottom edge on every screen size.
///
/// Animate horizonFraction at runtime to move the entire horizon (parallax,
/// day-night shift, etc.) — all anchored sprites follow automatically.
/// </summary>
[DefaultExecutionOrder(100)]
public class BackgroundHorizonLine : MonoBehaviour
{
    public static BackgroundHorizonLine Instance { get; private set; }

    [Tooltip("Horizon height as a fraction of camera height from the bottom. " +
             "0.35 = 35% up from the camera bottom edge. " +
             "Match this to the ground grid's heightFraction in WideBackgroundFiller.")]
    [Range(0f, 1f)]
    public float horizonFraction = 0.35f;

    [Tooltip("Draw an orange gizmo line at the horizon in the Scene view.")]
    public bool showGizmo = true;

    /// <summary>World-space Y of the horizon. Updated every LateUpdate.</summary>
    public static float WorldY { get; private set; }

    void Awake()
    {
        Instance = this;
        Compute();
    }

    void LateUpdate() => Compute();

    void Compute()
    {
        Camera cam = Camera.main;
        if (cam == null) return;
        float camBottom = cam.transform.position.y - cam.orthographicSize;
        WorldY = camBottom + cam.orthographicSize * 2f * horizonFraction;
    }

    void OnDrawGizmos()
    {
        if (!showGizmo) return;
        Camera cam = Camera.main;
        if (cam == null) return;
        Compute();
        float halfW = cam.orthographicSize * cam.aspect;
        float cx = cam.transform.position.x;
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.85f);
        Gizmos.DrawLine(new Vector3(cx - halfW, WorldY, 0f), new Vector3(cx + halfW, WorldY, 0f));
    }
}
