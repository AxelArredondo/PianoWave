using UnityEngine;

public class LaneGuides : MonoBehaviour
{
    [Header("Refs")]
    public LaneLayout laneLayout;

    [Tooltip("A prefab OR scene object that has the same SpriteRenderer as your falling tiles.")]
    public GameObject tilePrefabForWidth;

    [Header("Visuals")]
    public bool showGuides = true;
    public float guideTopPadding = 0.5f;
    public float guideBottomPadding = 0.5f;
    public float guideThicknessPixels = 3f;
    public Color guideColor = new Color(1f, 1f, 1f, 0.25f);
    public int guideSortingOrder = 1000;
    public string guideSortingLayerName = "";

    [Header("Width matching")]
    [Tooltip("If your tiles are scaled dynamically, set this to the typical tile width in world units instead.")]
    public bool usePrefabSpriteWidth = true;

    [Tooltip("Fallback/override: tile width in world units (used if prefab missing or usePrefabSpriteWidth=false).")]
    public float tileWidthWorldOverride = 1.0f;

    // Public results you can use elsewhere (HitLineFitter)
    public float LeftEdgeWorld { get; private set; }
    public float RightEdgeWorld { get; private set; }

    Camera cam;
    GameObject[] lines;
    int lastW, lastH;
    float lastOrtho;

    void Awake()
    {
        cam = Camera.main;
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
        if (!showGuides) return;
        if (cam == null) cam = Camera.main;

        if (Screen.width != lastW || Screen.height != lastH || !Mathf.Approximately(cam.orthographicSize, lastOrtho))
        {
            RebuildIfNeeded();
            UpdateGuides();
            Cache();
        }
    }

    void Cache()
    {
        lastW = Screen.width;
        lastH = Screen.height;
        lastOrtho = cam != null ? cam.orthographicSize : 0f;
    }

    void RebuildIfNeeded()
    {
        if (!showGuides || laneLayout == null || laneLayout.lanes == null) return;

        int laneCount = laneLayout.lanes.Length;
        if (laneCount < 1) return;

        int boundaryCount = laneCount + 1; // 4 lanes => 5 boundaries

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

    float GetTileWidthWorld()
    {
        if (usePrefabSpriteWidth && tilePrefabForWidth != null)
        {
            var sr = tilePrefabForWidth.GetComponentInChildren<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
            {
                // bounds.size includes prefab scale
                return sr.bounds.size.x;
            }
        }

        return Mathf.Max(0.0001f, tileWidthWorldOverride);
    }

    void UpdateGuides()
    {
        if (!showGuides || lines == null || cam == null || laneLayout == null || laneLayout.lanes == null) return;
        if (!cam.orthographic) return;

        int laneCount = laneLayout.lanes.Length;
        if (laneCount < 1) return;

        float tileW = GetTileWidthWorld();
        float halfTile = tileW * 0.5f;

        // Compute boundary positions based on lane centers + tile half width
        float leftMostCenterX = laneLayout.lanes[0].position.x;
        float rightMostCenterX = laneLayout.lanes[laneCount - 1].position.x;

        LeftEdgeWorld = leftMostCenterX - halfTile;
        RightEdgeWorld = rightMostCenterX + halfTile;

        float span = RightEdgeWorld - LeftEdgeWorld;
        float step = (laneCount == 0) ? span : span / laneCount; // N+1 boundaries => N segments

        // Vertical placement & thickness
        float halfH = cam.orthographicSize;
        float topY = cam.transform.position.y + halfH - guideTopPadding;
        float bottomY = cam.transform.position.y - halfH + guideBottomPadding;
        float height = Mathf.Max(0.1f, topY - bottomY);

        float worldPerPixel = (halfH * 2f) / Screen.height;
        float thicknessWorld = worldPerPixel * guideThicknessPixels;

        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i] == null) continue;

            float x = LeftEdgeWorld + step * i;
            lines[i].transform.position = new Vector3(x, (topY + bottomY) * 0.5f, 0f);
            lines[i].transform.localScale = new Vector3(thicknessWorld, height, 1f);

            var sr = lines[i].GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = guideColor;
        }
    }
}