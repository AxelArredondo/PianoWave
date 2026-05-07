using UnityEngine;

[DefaultExecutionOrder(0)]
public class LaneLayout : MonoBehaviour
{
    public Transform[] lanes;
    public float yPosition = 4f;

    [Header("Responsive Sizing  (driven by PlayfieldLayout)")]
    [Tooltip("Fallback fraction of screen width when PlayfieldLayout is not in the scene.")]
    [Range(0.3f, 1.0f)] public float fallbackFraction = 0.85f;

    [Tooltip("Must match TileSpawner.maxPercentOfLaneStep. " +
             "Used to back-compute lane step from the desired total tile area.")]
    [Range(0.5f, 0.99f)] public float tileToStepRatio = 0.98f;

    /// <summary>World-space distance between adjacent lane centres, updated every frame.</summary>
    public float LaneStepWorld { get; private set; }

    Camera cam;
    int lastW, lastH;
    float lastOrtho;
    float lastFraction;

    void Start()
    {
        cam = Camera.main;
        ApplyLayout();
        CacheState();
    }

    void Update()
    {
        float fraction = PlayfieldLayout.Instance != null ? PlayfieldLayout.Fraction : fallbackFraction;
        if (Screen.width == lastW && Screen.height == lastH &&
            cam != null && Mathf.Approximately(cam.orthographicSize, lastOrtho) &&
            Mathf.Approximately(fraction, lastFraction))
            return;

        ApplyLayout();
        CacheState();
    }

    void CacheState()
    {
        lastW = Screen.width;
        lastH = Screen.height;
        lastOrtho = cam != null ? cam.orthographicSize : 0f;
        lastFraction = PlayfieldLayout.Instance != null ? PlayfieldLayout.Fraction : fallbackFraction;
    }

    void ApplyLayout()
    {
        if (cam == null || lanes == null || lanes.Length == 0) return;

        float screenW = cam.orthographicSize * cam.aspect * 2f;
        float fraction = PlayfieldLayout.Instance != null ? PlayfieldLayout.Fraction : fallbackFraction;
        int n = lanes.Length;

        // Solve for lane step so that total tile area = screenW * fraction.
        // total tile area = span + tileWidth
        //                 = step*(n-1) + step*tileToStepRatio
        //                 = step * (n - 1 + tileToStepRatio)
        float denom = (n - 1) + tileToStepRatio;
        float step  = (n <= 1 || denom <= 0f) ? 0f : screenW * fraction / denom;
        float span  = step * (n - 1);

        LaneStepWorld = step;

        // Keep TileSizing current from frame 1 so LaneGuides, HitLineFitter,
        // and background are correctly sized before the first tile spawns.
        TileSizing.CurrentTileWidthWorld  = step * tileToStepRatio;
        TileSizing.CurrentTileHeightWorld = TileSizing.CurrentTileWidthWorld * TileSizing.TileAspectRatio;
        TileSizing.CurrentLaneStepWorld   = step;

        float startX = -span * 0.5f;

        for (int i = 0; i < lanes.Length; i++)
        {
            Vector3 p = lanes[i].position;
            p.x = startX + step * i;
            p.y = yPosition;
            lanes[i].position = p;
        }
    }
}
