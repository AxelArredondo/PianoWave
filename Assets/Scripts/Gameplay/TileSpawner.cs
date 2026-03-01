using UnityEngine;

public class TileSpawner : MonoBehaviour
{
    [Header("Refs")]
    public GameObject tilePrefab;
    public Transform[] lanes;

    [Tooltip("Drag your LaneContainer (with LaneLayout) here so we can read LaneStepWorld.")]
    public LaneLayout laneLayout;

    Camera cam;

    [Header("Tile size as % of screen (primary rule)")]
    [Range(0.01f, 0.50f)]
    public float tileWidthPercentOfScreen = 0.20f;

    [Header("Optional: prevent overlap with lane spacing")]
    [Range(0.50f, 0.99f)]
    public float maxPercentOfLaneStep = 0.98f;

    [Header("Vertical size tweak (prevents overlap)")]
    [Range(0.4f, 1f)]
    public float tileHeightMultiplier = 0.80f;

    [Header("Stop ultrawide pancakes")]
    [Range(0.5f, 4f)]
    public float maxWidthToHeightRatio = 1.4f;

    void Awake()
    {
        cam = Camera.main;
    }

    void OnEnable()
    {
        BeatManager.OnBeat += SpawnTile;
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

        int laneIndex = Random.Range(0, lanes.Length);
        GameObject tile = Instantiate(tilePrefab, lanes[laneIndex].position, Quaternion.identity);

        ApplyTileSize(tile);
    }

    void ApplyTileSize(GameObject tile)
    {
        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;
        float screenWidthWorld = halfWidth * 2f;

        float desiredWidth = screenWidthWorld * tileWidthPercentOfScreen;

        if (laneLayout != null && laneLayout.LaneStepWorld > 0.0001f)
        {
            float laneCap = laneLayout.LaneStepWorld * maxPercentOfLaneStep;
            desiredWidth = Mathf.Min(desiredWidth, laneCap);
        }

        TileSizing.CurrentTileWidthWorld = desiredWidth;

        if (laneLayout != null)
            TileSizing.CurrentLaneStepWorld = laneLayout.LaneStepWorld;

        float currentWidth = GetTileWorldWidth(tile);
        float currentHeight = GetTileWorldHeight(tile);
        if (currentWidth <= 0.0001f || currentHeight <= 0.0001f) return;

        float maxAllowedWidth = currentHeight * maxWidthToHeightRatio;
        desiredWidth = Mathf.Min(desiredWidth, maxAllowedWidth);

        float scaleFactorX = desiredWidth / currentWidth;

        Vector3 s = tile.transform.localScale;
        s.x *= scaleFactorX;          // width only
        s.y *= tileHeightMultiplier;  // reduce height a bit
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