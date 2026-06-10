using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

/// <summary>
/// Temporary debug tool. Enable via Inspector checkbox.
/// On a miss: pauses the game, shows a diagnostic overlay, draws lane/tile highlights.
/// Resume with the on-screen button or a 3-finger tap.
/// </summary>
public class TapDebugMode : MonoBehaviour
{
    public static TapDebugMode Instance { get; private set; }

    [Header("Debug Mode")]
    public bool debugMode = false;

    [Header("Scene Refs  (auto-found if blank)")]
    [SerializeField] private LaneLayout laneLayout;

    // ── tap context ────────────────────────────────────────────────────────────

    public struct TapContext
    {
        public Vector2 screenPos;
        public Vector3 worldPos;
        public int     lane;
        public bool    tileFound;
        public Vector3 tilePos;
        public float   tileTopY;
        public float   tileBotY;
        public float   hitLineY;
        public float   distTileToHitLine;
        public string  result;   // "Perfect" / "Good" / "Miss" / "NoTile" / "NoLane"
        public string  reason;
    }

    TapContext _lastCtx;
    float      _lastTapTime = -999f;
    bool       _debugPaused;

    // ── overlay ────────────────────────────────────────────────────────────────

    Canvas          _canvas;
    TextMeshProUGUI _infoText;

    // ── world-space debug visuals ──────────────────────────────────────────────

    SpriteRenderer   _laneHighlight;
    SpriteRenderer   _tileHighlight;
    SpriteRenderer[] _centerLines;
    SpriteRenderer[] _boundaryLines;
    bool             _visualsBuilt;

    // ── lifecycle ──────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (laneLayout == null) laneLayout = FindFirstObjectByType<LaneLayout>();

        BuildOverlay();
        BuildWorldVisuals();

        _canvas.gameObject.SetActive(false);
        SetLinesActive(debugMode);
    }

    void OnDestroy()
    {
        if (_debugPaused) Time.timeScale = 1f;
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        if (!debugMode) return;

        if (_visualsBuilt) UpdateLines();

        // Three-finger tap resumes (works at timeScale = 0 because Update is unscaled)
        if (_debugPaused && Touchscreen.current != null)
        {
            int began = 0;
            foreach (var t in Touchscreen.current.touches)
                if (t.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Began)
                    began++;
            if (began >= 3) ResumeFromDebug();
        }
    }

    // ── public API ─────────────────────────────────────────────────────────────

    /// Called by InputManager on every touch. Stores context, logs, updates highlights.
    public void RecordTapContext(TapContext ctx)
    {
        _lastCtx     = ctx;
        _lastTapTime = Time.unscaledTime;

        Debug.Log(
            $"[TapDebug] screen=({ctx.screenPos.x:F0},{ctx.screenPos.y:F0}) " +
            $"world=({ctx.worldPos.x:F3},{ctx.worldPos.y:F3}) " +
            $"lane={ctx.lane} tileFound={ctx.tileFound} " +
            $"result={ctx.result ?? "—"} | {ctx.reason ?? "—"}");

        if (!debugMode) return;
        HighlightLane(ctx.lane);
        HighlightTile(ctx.tileFound, ctx.tilePos, ctx.tileTopY, ctx.tileBotY);
    }

    /// Called by GameManager.MissTile() when debugMode is on.
    public void TriggerDebugPause()
    {
        if (_debugPaused) return;
        _debugPaused   = true;
        Time.timeScale = 0f;
        RefreshOverlay();
        _canvas.gameObject.SetActive(true);
    }

    public void ResumeFromDebug()
    {
        _debugPaused   = false;
        Time.timeScale = 1f;
        _canvas.gameObject.SetActive(false);
        ClearHighlights();
    }

    // ── overlay ────────────────────────────────────────────────────────────────

    void BuildOverlay()
    {
        var cGO = new GameObject("TapDebugCanvas");
        cGO.transform.SetParent(transform);

        _canvas              = cGO.AddComponent<Canvas>();
        _canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 200;

        var scaler                 = cGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight  = 0.5f;

        cGO.AddComponent<GraphicRaycaster>();

        // Dark background panel
        var bg    = MakePanel(cGO.transform, "BgPanel",
                              new Color(0f, 0f, 0f, 0.85f),
                              new Vector2(0.03f, 0.08f), new Vector2(0.97f, 0.93f));

        // Info text (leaves 80px at bottom for the button)
        var tGO   = new GameObject("InfoText");
        tGO.transform.SetParent(bg.transform, false);
        _infoText            = tGO.AddComponent<TextMeshProUGUI>();
        _infoText.fontSize   = 26f;
        _infoText.color      = Color.white;
        _infoText.alignment  = TextAlignmentOptions.TopLeft;
        _infoText.text       = "";
        var tr               = tGO.GetComponent<RectTransform>();
        tr.anchorMin         = Vector2.zero;
        tr.anchorMax         = Vector2.one;
        tr.offsetMin         = new Vector2(24f, 80f);
        tr.offsetMax         = new Vector2(-24f, -24f);

        // Resume button
        var btn   = MakePanel(bg.transform, "ResumeBtn",
                              new Color(0.15f, 0.78f, 0.15f, 1f),
                              new Vector2(0.15f, 0f), new Vector2(0.85f, 0f));
        var brt   = btn.GetComponent<RectTransform>();
        brt.offsetMin = new Vector2(0f, 10f);
        brt.offsetMax = new Vector2(0f, 72f);

        var b     = btn.AddComponent<Button>();
        b.targetGraphic = btn.GetComponent<Image>();
        b.onClick.AddListener(ResumeFromDebug);

        var lGO   = new GameObject("Label");
        lGO.transform.SetParent(btn.transform, false);
        var lbl              = lGO.AddComponent<TextMeshProUGUI>();
        lbl.text             = "RESUME  (or 3-finger tap)";
        lbl.fontSize         = 22f;
        lbl.color            = Color.black;
        lbl.fontStyle        = FontStyles.Bold;
        lbl.alignment        = TextAlignmentOptions.Center;
        var lr               = lGO.GetComponent<RectTransform>();
        lr.anchorMin         = Vector2.zero;
        lr.anchorMax         = Vector2.one;
        lr.offsetMin         = lr.offsetMax = Vector2.zero;
    }

    static GameObject MakePanel(Transform parent, string name, Color color,
                                 Vector2 ancMin, Vector2 ancMax)
    {
        var go        = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img       = go.AddComponent<Image>();
        img.color     = color;
        var rt        = go.GetComponent<RectTransform>();
        rt.anchorMin  = ancMin;
        rt.anchorMax  = ancMax;
        rt.offsetMin  = rt.offsetMax = Vector2.zero;
        return go;
    }

    void RefreshOverlay()
    {
        if (_infoText == null) return;
        var c = _lastCtx;

        bool isMiss   = c.result == "Miss" || c.result == "NoTile" || c.result == "NoLane";
        string rc     = isMiss ? "<color=#FF6060>" : "<color=#80FF80>";
        string end    = "</color>";
        bool recent   = (Time.unscaledTime - _lastTapTime) < 0.5f;
        string origin = recent
            ? "tap-initiated"
            : "<color=#FFAA40>auto-miss (last tap data shown)</color>";

        string laneStr = c.lane >= 0 ? c.lane.ToString() : "<color=#FF6060>NONE</color>";

        string tileBlock = c.tileFound
            ? $"Tile pos:     ({c.tilePos.x:F3}, {c.tilePos.y:F3})\n" +
              $"Tile bounds:  top={c.tileTopY:F3}  bot={c.tileBotY:F3}\n" +
              $"HitLine Y:    {c.hitLineY:F3}\n" +
              $"Dist to hit:  {c.distTileToHitLine:F3}\n"
            : $"HitLine Y:    {c.hitLineY:F3}\n" +
              $"Tile:         <color=#FF6060>NOT FOUND</color>\n";

        _infoText.text =
            $"<b><size=28>TAP DEBUG</size></b>  <size=20>{origin}</size>\n\n" +
            $"Screen:   ({c.screenPos.x:F0}, {c.screenPos.y:F0})\n" +
            $"World:    ({c.worldPos.x:F3}, {c.worldPos.y:F3})\n" +
            $"Lane:     {laneStr}\n" +
            tileBlock +
            $"\nResult:   {rc}<b>{c.result ?? "—"}</b>{end}\n" +
            $"Reason:   {rc}{c.reason ?? "—"}{end}";
    }

    // ── world-space visuals ────────────────────────────────────────────────────

    void BuildWorldVisuals()
    {
        if (laneLayout == null) return;
        int n = laneLayout.lanes.Length;

        _laneHighlight = MakeSR("_DbgLaneHL",  new Color(1f, 1f, 0f, 0.18f),       15);
        _tileHighlight = MakeSR("_DbgTileHL",  new Color(1f, 0.3f, 0f, 0.55f),     16);

        _centerLines   = new SpriteRenderer[n];
        _boundaryLines = new SpriteRenderer[n + 1];

        for (int i = 0; i < n; i++)
            _centerLines[i]   = MakeSR($"_DbgCenter{i}",  new Color(0f, 1f, 1f, 0.40f),  14);
        for (int i = 0; i <= n; i++)
            _boundaryLines[i] = MakeSR($"_DbgBound{i}",   new Color(1f, 0.55f, 0f, 0.55f), 14);

        _visualsBuilt = true;
    }

    SpriteRenderer MakeSR(string name, Color color, int order)
    {
        var go          = new GameObject(name);
        go.transform.SetParent(transform);
        var sr          = go.AddComponent<SpriteRenderer>();
        var tex         = Texture2D.whiteTexture;
        sr.sprite       = Sprite.Create(tex,
                              new Rect(0, 0, tex.width, tex.height),
                              new Vector2(0.5f, 0.5f), tex.width);
        sr.color        = color;
        sr.sortingOrder = order;
        go.SetActive(false);
        return sr;
    }

    void UpdateLines()
    {
        if (laneLayout == null || Camera.main == null) return;

        var   cam    = Camera.main;
        float halfH  = cam.orthographicSize;
        float camY   = cam.transform.position.y;
        float height = halfH * 2f;
        float step   = laneLayout.LaneStepWorld;
        float wpx    = height / Screen.height;
        float thin   = wpx * 2f;
        float thick  = wpx * 4f;

        for (int i = 0; i < laneLayout.lanes.Length && i < _centerLines.Length; i++)
        {
            var sr = _centerLines[i];
            if (!sr) continue;
            sr.transform.position   = new Vector3(laneLayout.lanes[i].position.x, camY, 0f);
            sr.transform.localScale = new Vector3(thin, height, 1f);
            sr.gameObject.SetActive(true);
        }

        float leftEdge = laneLayout.lanes[0].position.x - step * 0.5f;
        for (int i = 0; i < _boundaryLines.Length; i++)
        {
            var sr = _boundaryLines[i];
            if (!sr) continue;
            sr.transform.position   = new Vector3(leftEdge + step * i, camY, 0f);
            sr.transform.localScale = new Vector3(thick, height, 1f);
            sr.gameObject.SetActive(true);
        }
    }

    void SetLinesActive(bool on)
    {
        if (_centerLines   != null) foreach (var sr in _centerLines)   if (sr) sr.gameObject.SetActive(on);
        if (_boundaryLines != null) foreach (var sr in _boundaryLines) if (sr) sr.gameObject.SetActive(on);
    }

    void HighlightLane(int laneIndex)
    {
        if (!_laneHighlight || laneLayout == null || Camera.main == null) return;
        if (laneIndex < 0 || laneIndex >= laneLayout.lanes.Length)
        { _laneHighlight.gameObject.SetActive(false); return; }

        var cam = Camera.main;
        _laneHighlight.transform.position   = new Vector3(
            laneLayout.lanes[laneIndex].position.x, cam.transform.position.y, 0f);
        _laneHighlight.transform.localScale = new Vector3(
            laneLayout.LaneStepWorld, cam.orthographicSize * 2f, 1f);
        _laneHighlight.gameObject.SetActive(true);
    }

    void HighlightTile(bool found, Vector3 pos, float topY, float botY)
    {
        if (!_tileHighlight) return;
        if (!found) { _tileHighlight.gameObject.SetActive(false); return; }

        float h = Mathf.Max(0.05f, topY - botY) * 1.2f;
        float w = TileSizing.CurrentTileWidthWorld * 1.2f;
        _tileHighlight.transform.position   = new Vector3(pos.x, (topY + botY) * 0.5f, 0f);
        _tileHighlight.transform.localScale = new Vector3(w, h, 1f);
        _tileHighlight.gameObject.SetActive(true);
    }

    void ClearHighlights()
    {
        if (_laneHighlight) _laneHighlight.gameObject.SetActive(false);
        if (_tileHighlight) _tileHighlight.gameObject.SetActive(false);
    }
}
