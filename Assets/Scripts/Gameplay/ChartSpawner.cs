using UnityEngine;

// Reads a JSON chart and spawns tiles so they arrive at the hitline on the correct beat.
//
// Timing model:
//   - gameTimer starts at 0 when the scene loads and runs while unpaused.
//   - travelBeats = (spawnY - hitlineY) / (tileH + gap)  — how many beats a tile takes to fall.
//   - musicStartDelay = travelBeats * SecondsPerBeat
//   - Music begins after musicStartDelay seconds of pre-roll so chart beats = audio beats.
//   - A note with beat=N spawns at gameTimer = N * SecondsPerBeat.
//     It arrives at the hitline at gameTimer = (N + travelBeats) * SecondsPerBeat
//     = musicStartDelay + N * SecondsPerBeat — which is audio time N * SecondsPerBeat. ✓
//   - Once music is playing, audioSource.time drives spawn decisions (audio-synced, drift-free).
//
// SpeedMultiplier: a visual-only speed scalar updated by speedEvents. It does NOT change
//   BeatManager.bpm or audio playback — only how fast tiles fall on screen.
//   Tile.Update multiplies baseSpeed by (BeatManager.bpm/120) * SpeedMultiplier.
//   In random mode SpeedMultiplier is never changed, so it stays at 1.0.
//
// To add more songs: drop another JSON under Resources/Charts/ and set ChartResourcePath in GameSettings.
public class ChartSpawner : MonoBehaviour
{
    public static ChartSpawner Instance;

    [Header("Chart (set via GameSettings or fallback below)")]
    [Tooltip("Fallback resource path if GameSettings is absent. e.g. 'Charts/Level1'")]
    public string chartResourcePath = "Charts/Level1";

    [Header("Refs — mirror TileSpawner values exactly")]
    public GameObject tilePrefab;
    public Transform[] lanes;
    public LaneLayout laneLayout;

    [Header("Lane Colors")]
    public Color[] laneColors;

    [Header("Tile sizing — keep in sync with TileSpawner")]
    [Range(0.50f, 0.99f)] public float maxPercentOfLaneStep = 0.98f;
    public float tileAspectRatio = 1.4f;
    public float spawnHeightOffset = 0f;
    [Range(0f, 2f)] public float spawnGapFraction = 0.15f;

    // ── visual speed multiplier (level mode only) ──────────────────────────────
    // Updated by speedEvents in the chart. Tile.Update reads this.
    // Always 1.0 in random mode (ChartSpawner is disabled there).
    public static float SpeedMultiplier = 1f;

    // ── runtime state ──────────────────────────────────────────────────────────
    ChartData chart;
    float secondsPerBeat;
    float musicStartDelay;   // seconds of pre-roll before audio begins
    float gameTimer;         // accumulates while unpaused
    bool  musicStarted;
    bool  initialized;       // deferred to first Update so LaneLayout is settled
    int   noteIndex;
    int   speedIndex;
    int   bgIndex;
    Camera cam;

    // ── lifecycle ──────────────────────────────────────────────────────────────

    void Awake()
    {
        Instance = this;
        cam = Camera.main;
        if (laneColors == null || laneColors.Length == 0) InitDefaultColors();
    }

    void Start()
    {
        bool isLevelMode = GameSettings.Instance != null && GameSettings.Instance.Mode == GameMode.LevelMode;
        if (!isLevelMode) { enabled = false; return; }

        string path = (GameSettings.Instance != null && !string.IsNullOrEmpty(GameSettings.Instance.ChartResourcePath))
            ? GameSettings.Instance.ChartResourcePath
            : chartResourcePath;

        TextAsset json = Resources.Load<TextAsset>(path);
        if (json == null)
        {
            Debug.LogError($"ChartSpawner: JSON not found at Resources/{path}");
            enabled = false;
            return;
        }

        chart = JsonUtility.FromJson<ChartData>(json.text);
        secondsPerBeat = 60f / chart.bpm;

        if (BeatManager.Instance != null)
            BeatManager.Instance.bpm = chart.bpm;

        // Load the matching audio clip from AudioManager's library.
        AudioManager.Instance?.SetMusicByName(chart.audioClipName);

        gameTimer        = 0f;
        noteIndex        = 0;
        speedIndex       = 0;
        bgIndex          = 0;
        SpeedMultiplier  = 1f;
        musicStarted     = false;
        initialized      = false;
    }

    void Update()
    {
        if (GameManager.Instance.IsGameOver || GameManager.Instance.IsPaused) return;

        // Deferred init: LaneLayout and HitLineFitter are settled after first frame.
        if (!initialized) { ComputeTiming(); initialized = true; }

        gameTimer += Time.deltaTime;

        // Start music after pre-roll so note beats align with audio beats.
        if (!musicStarted && gameTimer >= musicStartDelay)
        {
            musicStarted = true;
            AudioManager.Instance?.PlayFromStart();
        }

        float currentTime = GetCurrentTime();

        // Spawn all notes whose spawn time has arrived.
        // A note at beat N spawns at gameTime = N * secondsPerBeat (see timing model above).
        while (noteIndex < chart.notes.Length)
        {
            float adjustedBeat = chart.notes[noteIndex].beat - chart.songOffsetBeats;
            if (currentTime < adjustedBeat * secondsPerBeat) break;
            SpawnNote(chart.notes[noteIndex]);
            noteIndex++;
        }

        // Process speed events — update the visual speed multiplier.
        // Does NOT touch BeatManager.bpm or audio; no sync risk.
        if (chart.speedEvents != null)
        {
            while (speedIndex < chart.speedEvents.Length)
            {
                float adjustedBeat = chart.speedEvents[speedIndex].beat - chart.songOffsetBeats;
                if (currentTime < adjustedBeat * secondsPerBeat) break;
                SpeedMultiplier = chart.speedEvents[speedIndex].bpm / chart.bpm;
                speedIndex++;
            }
        }

        // Process background events — notify BackgroundController to switch theme.
        if (chart.backgroundEvents != null)
        {
            while (bgIndex < chart.backgroundEvents.Length)
            {
                float adjustedBeat = chart.backgroundEvents[bgIndex].beat - chart.songOffsetBeats;
                if (currentTime < adjustedBeat * secondsPerBeat) break;
                BackgroundController.Instance?.SetTheme(chart.backgroundEvents[bgIndex].theme);
                bgIndex++;
            }
        }
    }

    // ── timing helpers ─────────────────────────────────────────────────────────

    void ComputeTiming()
    {
        float desiredWidth = laneLayout != null && laneLayout.LaneStepWorld > 0.0001f
            ? laneLayout.LaneStepWorld * maxPercentOfLaneStep : 1f;
        float tileH = desiredWidth * tileAspectRatio;
        float gap   = tileH * spawnGapFraction;

        float spawnY   = (lanes != null && lanes.Length > 0) ? lanes[0].position.y + spawnHeightOffset : 4f;
        float hitlineY = (GameManager.Instance != null && GameManager.Instance.hitLine != null)
            ? GameManager.Instance.hitLine.position.y : -3f;

        float travelDist  = Mathf.Max(tileH, spawnY - hitlineY);
        float travelBeats = travelDist / (tileH + gap);
        musicStartDelay   = travelBeats * secondsPerBeat;
    }

    // Once music is playing use audioSource.time as the authoritative clock (drift-free).
    // During pre-roll use gameTimer offset so spawn logic stays continuous across the transition.
    float GetCurrentTime()
    {
        if (musicStarted && AudioManager.Instance?.musicSource != null &&
            AudioManager.Instance.musicSource.isPlaying)
            return AudioManager.Instance.musicSource.time + musicStartDelay;

        return gameTimer;
    }

    // ── spawning ───────────────────────────────────────────────────────────────

    void SpawnNote(ChartNote note)
    {
        if (tilePrefab == null || lanes == null) return;
        if (cam == null) cam = Camera.main;

        float desiredWidth = ComputeDesiredWidth();
        float tileH        = desiredWidth * tileAspectRatio;
        float gap          = tileH * spawnGapFraction;
        float baseSpeed    = (tileH + gap) * 2f;

        TileSizing.TileAspectRatio        = tileAspectRatio;
        TileSizing.CurrentTileWidthWorld  = desiredWidth;
        TileSizing.CurrentTileHeightWorld = tileH;
        if (laneLayout != null) TileSizing.CurrentLaneStepWorld = laneLayout.LaneStepWorld;

        foreach (int laneIndex in note.lanes)
        {
            if (laneIndex < 0 || laneIndex >= lanes.Length) continue;

            Vector3 spawnPos = lanes[laneIndex].position;
            spawnPos.y += spawnHeightOffset;

            GameObject tileObj = Instantiate(tilePrefab, spawnPos, Quaternion.identity);
            ApplyTileScale(tileObj, desiredWidth, tileH);

            Tile tile = tileObj.GetComponent<Tile>();
            if (tile != null)
            {
                tile.baseSpeed = baseSpeed;
                Color c = (laneColors != null && laneIndex < laneColors.Length)
                    ? laneColors[laneIndex] : Color.white;
                tile.Init(laneIndex, c, note.noteType, note.durationBeats);
            }
        }
    }

    // ── sizing helpers (mirrored from TileSpawner) ─────────────────────────────

    float ComputeDesiredWidth()
    {
        if (laneLayout != null && laneLayout.LaneStepWorld > 0.0001f)
            return laneLayout.LaneStepWorld * maxPercentOfLaneStep;
        float halfW = cam.orthographicSize * cam.aspect;
        return halfW * 2f * 0.20f;
    }

    void ApplyTileScale(GameObject tile, float w, float h)
    {
        float nw = GetNativeWidth(tile), nh = GetNativeHeight(tile);
        if (nw <= 0.0001f || nh <= 0.0001f) return;
        Vector3 s = tile.transform.localScale;
        s.x *= w / nw;
        s.y *= h / nh;
        tile.transform.localScale = s;
    }

    float GetNativeWidth(GameObject tile)
    {
        var sr = tile.GetComponentInChildren<SpriteRenderer>();
        if (sr != null) return sr.bounds.size.x;
        var col = tile.GetComponentInChildren<Collider2D>();
        if (col != null) return col.bounds.size.x;
        return 1f;
    }

    float GetNativeHeight(GameObject tile)
    {
        var sr = tile.GetComponentInChildren<SpriteRenderer>();
        if (sr != null) return sr.bounds.size.y;
        var col = tile.GetComponentInChildren<Collider2D>();
        if (col != null) return col.bounds.size.y;
        return 1f;
    }

    void InitDefaultColors()
    {
        laneColors = new Color[4];
        ColorUtility.TryParseHtmlString("#35E0FF", out laneColors[0]);
        ColorUtility.TryParseHtmlString("#FF3DAA", out laneColors[1]);
        ColorUtility.TryParseHtmlString("#A66BFF", out laneColors[2]);
        ColorUtility.TryParseHtmlString("#FF9A3D", out laneColors[3]);
    }
}
