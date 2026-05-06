using UnityEngine;

public class TileSpawner : MonoBehaviour
{
    [Header("Refs")]
    public GameObject tilePrefab;
    public Transform[] lanes;

    [Tooltip("Drag your LaneContainer (with LaneLayout) here so we can read LaneStepWorld.")]
    public LaneLayout laneLayout;

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

    void SpawnTile()
    {
        if (GameManager.Instance.IsGameOver) return;
        if (tilePrefab == null || lanes == null || lanes.Length == 0) return;
        if (cam == null) cam = Camera.main;

        // Compute actual dimensions first so the tracker uses the real tile height,
        // not a stale cached value from a previous (possibly wrongly-sized) tile.
        float desiredWidth  = ComputeDesiredWidth();
        float tileH         = desiredWidth * tileAspectRatio;
        float gap           = tileH * spawnGapFraction;

        // Publish sizes before anything reads TileSizing.
        TileSizing.TileAspectRatio        = tileAspectRatio;
        TileSizing.CurrentTileWidthWorld  = desiredWidth;
        TileSizing.CurrentTileHeightWorld = tileH;
        if (laneLayout != null)
            TileSizing.CurrentLaneStepWorld = laneLayout.LaneStepWorld;

        int laneIndex = Random.Range(0, lanes.Length);
        float laneY   = lanes[laneIndex].position.y + spawnHeightOffset;

        // globalTopEdgeY stores where the previous tile's top will be at the NEXT beat
        // (spawn-time top minus one beat's worth of falling = minus (tileH + gap)).
        // This means in steady state minCentreY == laneY, so all tiles spawn at laneY
        // and are naturally spaced by their fall distance. The Mathf.Max only matters
        // when two beats fire in rapid succession (overlap guard).
        float minCentreY = globalTopEdgeY + gap + tileH * 0.5f;
        float spawnY     = Mathf.Max(laneY, minCentreY);

        // Advance tracker: record this tile's top edge minus one beat of falling.
        // fall per beat = baseSpeed × 0.5 = (tileH + gap) × 2 × 0.5 = (tileH + gap)
        globalTopEdgeY = (spawnY + tileH * 0.5f) - (tileH + gap);

        Vector3 spawnPos = lanes[laneIndex].position;
        spawnPos.y       = spawnY;
        GameObject tile  = Instantiate(tilePrefab, spawnPos, Quaternion.identity);

        ApplyTileScale(tile, desiredWidth, tileH);

        // Set speed so the tile falls by exactly (tileH + gap) per beat, making the
        // visual gap between consecutive tiles equal to spawnGapFraction × tileH on
        // every screen size. Tile.Update computes: move = baseSpeed × (bpm/120) × dt,
        // so fall/beat = baseSpeed × 0.5 → baseSpeed = (tileH + gap) × 2.
        var tileComp = tile.GetComponent<Tile>();
        if (tileComp != null)
            tileComp.baseSpeed = (tileH + gap) * 2f;
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
