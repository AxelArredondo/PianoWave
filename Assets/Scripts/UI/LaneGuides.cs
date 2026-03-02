using UnityEngine;

[DefaultExecutionOrder(50)]
public class LaneGuides : MonoBehaviour
{
    [Header("Refs")]
    public LaneLayout laneLayout;
    public Camera cam;

    [Tooltip("Optional: assign your hitline Transform so guides can stop there.")]
    public Transform hitLine;

    [Header("Visuals")]
    public bool showGuides = true;
    public Color guideColor = new Color(1f, 1f, 1f, 0.25f);
    public int guideSortingOrder = 1000;
    public string guideSortingLayerName = "";

    [Header("Guide size")]
    public float guideThicknessPixels = 3f;

    [Tooltip("If OFF: guides go to bottom of camera view. If ON: guides stop at hitline Y.")]
    public bool stopGuidesAtHitLine = true;

    public float topPaddingWorld = 0.5f;
    public float bottomPaddingWorld = 0.5f;

    [Header("Alignment fine-tune")]
    [Tooltip("Positive shrinks inward, negative expands outward. Use this to compensate for bevel/transparent padding.")]
    public float edgeInsetWorld = 0.00f;

    // Public boundaries for HitLineFitter
    public float LeftEdgeWorld { get; private set; }
    public float RightEdgeWorld { get; private set; }

    GameObject[] lines;
    int lastW, lastH;
    float lastOrtho;

    void Awake()
    {
        if (cam == null) cam = Camera.main;
        if (laneLayout == null) laneLayout = GetComponent<LaneLayout>();
    }

    void Start()
    {
        RebuildIfNeeded();
        UpdateGuides();
        Cache();
    }

    void LateUpdate()
    {
        if (!showGuides) { SetActive(false); return; }
        SetActive(true);

        if (cam == null) cam = Camera.main;

        // Update if resolution or camera size changes (camera scaler)
        if (Screen.width != lastW || Screen.height != lastH || !Mathf.Approximately(cam.orthographicSize, lastOrtho))
        {
            RebuildIfNeeded();
            UpdateGuides();
            Cache();
        }
        else
        {
            // Still update Y range if hitline moves
            UpdateGuides();
        }
    }

    void Cache()
    {
        lastW = Screen.width;
        lastH = Screen.height;
        lastOrtho = cam != null ? cam.orthographicSize : 0f;
    }

    void SetActive(bool active)
    {
        if (lines == null) return;
        for (int i = 0; i < lines.Length; i++)
            if (lines[i] != null && lines[i].activeSelf != active)
                lines[i].SetActive(active);
    }

    void RebuildIfNeeded()
    {
        if (laneLayout == null || laneLayout.lanes == null) return;

        int laneCount = laneLayout.lanes.Length;
        if (laneCount < 1) return;

        int boundaryCount = laneCount + 1; // 4 lanes -> 5 boundary lines

        if (lines != null && lines.Length == boundaryCount) return;

        // Destroy old
        if (lines != null)
        {
            for (int i = 0; i < lines.Length; i++)
                if (lines[i] != null) Destroy(lines[i]);
        }

        lines = new GameObject[boundaryCount];

        for (int i = 0; i < boundaryCount; i++)
        {
            var g = new GameObject($"LaneBoundary_{i}");
            g.transform.SetParent(transform, worldPositionStays: true);

            var sr = g.AddComponent<SpriteRenderer>();
            sr.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1),
                new Vector2(0.5f, 0.5f), 1f);
            sr.color = guideColor;

            if (!string.IsNullOrEmpty(guideSortingLayerName))
                sr.sortingLayerName = guideSortingLayerName;

            sr.sortingOrder = guideSortingOrder;

            lines[i] = g;
        }
    }

    void UpdateGuides()
    {
        if (!showGuides || lines == null || cam == null || laneLayout == null || laneLayout.lanes == null) return;
        if (!cam.orthographic) return;

        int laneCount = laneLayout.lanes.Length;
        if (laneCount < 1) return;

        // Use the TRUE tile width (what spawner decided), not prefab sprite bounds
        float tileW = Mathf.Max(0.0001f, TileSizing.CurrentTileWidthWorld);
        float halfTile = tileW * 0.5f;

        float leftMostCenterX = laneLayout.lanes[0].position.x;
        float rightMostCenterX = laneLayout.lanes[laneCount - 1].position.x;

        // Boundaries at tile edges (+ optional inset)
        LeftEdgeWorld = (leftMostCenterX - halfTile) + edgeInsetWorld;
        RightEdgeWorld = (rightMostCenterX + halfTile) - edgeInsetWorld;

        float span = RightEdgeWorld - LeftEdgeWorld;
        float step = span / laneCount; // laneCount segments between laneCount+1 boundaries

        // Vertical extents
        float halfH = cam.orthographicSize;
        float topY = cam.transform.position.y + halfH - topPaddingWorld;

        float bottomY;
        if (stopGuidesAtHitLine && hitLine != null)
            bottomY = hitLine.position.y;
        else
            bottomY = cam.transform.position.y - halfH + bottomPaddingWorld;

        float height = Mathf.Max(0.1f, topY - bottomY);

        // Thickness in pixels -> world
        float worldPerPixel = (halfH * 2f) / Screen.height;
        float thicknessWorld = worldPerPixel * guideThicknessPixels;

        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i] == null) continue;

            float x = LeftEdgeWorld + step * i;
            lines[i].transform.position = new Vector3(x, bottomY + height * 0.5f, 0f);
            lines[i].transform.localScale = new Vector3(thicknessWorld, height, 1f);

            var sr = lines[i].GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = guideColor;
        }
    }
}