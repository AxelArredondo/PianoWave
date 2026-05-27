using UnityEngine;

public class TileSpawner : MonoBehaviour
{
    [Header("Refs")]
    public GameObject tilePrefab;
    public Transform[] lanes;

    [Tooltip("Drag your LaneContainer (with LaneLayout) here so we can read LaneStepWorld.")]
    public LaneLayout laneLayout;

    [Header("Lane Colors")]
    [Tooltip("One entry per lane — index must match the lanes[] array.")]
    public Color[] laneColors;

    Camera cam;

    [Header("Tile width")]
    [Tooltip("Fraction of lane step used as tile width. Match LaneLayout.tileToStepRatio.")]
    [Range(0.50f, 0.99f)]
    public float maxPercentOfLaneStep = 0.98f;

    [Tooltip("Fallback tile width as fraction of screen width — only used when no LaneLayout is assigned.")]
    [Range(0.01f, 0.50f)]
    public float tileWidthPercentOfScreen = 0.20f;

    [Header("Tile aspect ratio")]
    [Tooltip("height = width * tileAspectRatio. 1.4 gives a tall piano-tile shape (0.7 w:h).")]
    public float tileAspectRatio = 1.4f;

    [Header("Spawn position")]
    [Tooltip("Extra world-units added to the lane Y when spawning. Positive = higher on screen.")]
    public float spawnHeightOffset = 0f;

    [Header("Spawn spacing")]
    [Tooltip("Visual gap between consecutive tiles as a fraction of tile height. " +
             "0 = tiles touch edge-to-edge; 0.5 = half a tile of space between them. " +
             "Also controls scroll speed: larger gap = faster tiles.")]
    [Range(0f, 2f)]
    public float spawnGapFraction = 0.15f;

    // Estimated Y of the previous tile's top edge at the NEXT beat (after it has fallen).
    // Used only to prevent overlap when two beats fire in rapid succession.
    float globalTopEdgeY = float.MinValue;

    void Awake()
    {
        cam = Camera.main;
        if (laneColors == null || laneColors.Length == 0)
            InitDefaultLaneColors();
    }

    void Reset() => InitDefaultLaneColors();

    void InitDefaultLaneColors()
    {
        laneColors = new Color[4];
        ColorUtility.TryParseHtmlString("#35E0FF", out laneColors[0]);
        ColorUtility.TryParseHtmlString("#FF3DAA", out laneColors[1]);
        ColorUtility.TryParseHtmlString("#A66BFF", out laneColors[2]);
        ColorUtility.TryParseHtmlString("#FF9A3D", out laneColors[3]);
    }

    void OnEnable()
    {
        BeatManager.OnBeat += SpawnTile;
        globalTopEdgeY = float.MinValue; // reset on game restart
    }

    void OnDisable()
    {
        BeatManager.OnBeat -= SpawnTile;
    }

    void Start()
    {
        // Give the difficulty manager its lane count before the first beat fires.
        int numLanes = (lanes != null) ? lanes.Length : 4;
        RandomDifficultyManager.Instance?.Initialize(numLanes);
    }

    void SpawnTile()
    {
        if (GameSettings.Instance != null && GameSettings.Instance.Mode == GameMode.LevelMode) return;
        if (GameManager.Instance.IsGameOver) return;
        if (tilePrefab == null || lanes == null || lanes.Length == 0) return;
        if (cam == null) cam = Camera.main;

        // Compute actual dimensions first so the tracker uses the real tile height,
        // not a stale cached value from a previous (possibly wrongly-sized) tile.
        float desiredWidth = ComputeDesiredWidth();
        float tileH        = desiredWidth * tileAspectRatio;
        float gap          = tileH * spawnGapFraction;
        float baseSpd      = (tileH + gap) * 2f;

        // Publish sizes before anything reads TileSizing.
        TileSizing.TileAspectRatio        = tileAspectRatio;
        TileSizing.CurrentTileWidthWorld  = desiredWidth;
        TileSizing.CurrentTileHeightWorld = tileH;
        if (laneLayout != null)
            TileSizing.CurrentLaneStepWorld = laneLayout.LaneStepWorld;

        // Ask the difficulty manager what to spawn this beat.
        // Falls back to a plain random tap tile if no manager is present.
        int[]  spawnLanes;
        string noteType      = "tap";
        float  durationBeats = 0f;

        var mgr = RandomDifficultyManager.Instance;
        if (mgr != null && mgr.enabled)
        {
            mgr.AdvanceBeat();
            SpawnDecision decision = mgr.GetDecision();
            if (decision.lanes == null || decision.lanes.Length == 0) return; // rest beat
            spawnLanes    = decision.lanes;
            noteType      = decision.noteType;
            durationBeats = decision.durationBeats;
        }
        else
        {
            spawnLanes = new int[] { Random.Range(0, lanes.Length) };
        }

        // All tiles in a single beat share the same Y. The overlap guard fires once
        // and protects against two beats firing in rapid succession.
        // All lane transforms share the same Y, so lanes[0] is representative.
        float laneY      = lanes[0].position.y + spawnHeightOffset;
        float minCentreY = globalTopEdgeY + gap + tileH * 0.5f;
        float spawnY     = Mathf.Max(laneY, minCentreY);
        globalTopEdgeY   = (spawnY + tileH * 0.5f) - (tileH + gap);

        foreach (int laneIndex in spawnLanes)
        {
            if (laneIndex < 0 || laneIndex >= lanes.Length) continue;

            Vector3 spawnPos = lanes[laneIndex].position;
            spawnPos.y       = spawnY;

            GameObject tileObj = Instantiate(tilePrefab, spawnPos, Quaternion.identity);
            ApplyTileScale(tileObj, desiredWidth, tileH);

            var tileComp = tileObj.GetComponent<Tile>();
            if (tileComp != null)
            {
                tileComp.baseSpeed = baseSpd;
                Color laneColor = (laneColors != null && laneIndex < laneColors.Length)
                    ? laneColors[laneIndex] : Color.white;
                tileComp.Init(laneIndex, laneColor, noteType, durationBeats);
            }
        }
    }

    float ComputeDesiredWidth()
    {
        if (laneLayout != null && laneLayout.LaneStepWorld > 0.0001f)
            return laneLayout.LaneStepWorld * maxPercentOfLaneStep;

        float halfWidth = cam.orthographicSize * cam.aspect;
        return halfWidth * 2f * tileWidthPercentOfScreen;
    }

    void ApplyTileScale(GameObject tile, float desiredWidth, float desiredHeight)
    {
        float nativeWidth  = GetTileWorldWidth(tile);
        float nativeHeight = GetTileWorldHeight(tile);
        if (nativeWidth <= 0.0001f || nativeHeight <= 0.0001f) return;

        Vector3 s = tile.transform.localScale;
        s.x *= desiredWidth  / nativeWidth;
        s.y *= desiredHeight / nativeHeight;
        tile.transform.localScale = s;
    }

    float GetTileWorldWidth(GameObject tile)
    {
        var sr = tile.GetComponentInChildren<SpriteRenderer>();
        if (sr != null) return sr.bounds.size.x;

        var col = tile.GetComponentInChildren<Collider2D>();
        if (col != null) return col.bounds.size.x;

        return 1f;
    }

    float GetTileWorldHeight(GameObject tile)
    {
        var sr = tile.GetComponentInChildren<SpriteRenderer>();
        if (sr != null) return sr.bounds.size.y;

        var col = tile.GetComponentInChildren<Collider2D>();
        if (col != null) return col.bounds.size.y;

        return 1f;
    }
}
