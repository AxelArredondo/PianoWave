using UnityEngine;
using TMPro;

// Draws one receptor "slot" per lane at the hitline.
//
// Each receptor is built from 5 SpriteRenderers at runtime (no prefab required):
//   Fill       — semi-transparent lane-colour square
//   BorderTop / BorderBottom / BorderLeft / BorderRight — bright outline strips
//
// Receptors reposition + resize every LateUpdate so they stay aligned on any screen.
// Call PulseReceptor(lane) from InputManager when a lane is pressed.
//
// ── MANUAL UNITY SETUP ────────────────────────────────────────────────────────
//  1. Create empty GameObject in PianoWave_Main → name it "HitReceptors"
//  2. Add this component
//  3. Assign: laneLayout, hitLine (same Transform that GameManager.hitLine uses)
//  4. Optional: assign laneColors (or leave blank — defaults match TileSpawner)
//  5. Set sortingOrder so receptors sit above the background but below tiles.
//     Recommended: use a value LOWER than tiles (tiles default to 0 → try -1 here,
//     or match whatever sorting layer you use for tiles and set order to -1).
//  6. For a neon glow look: create a Material using "Sprites/Default" shader,
//     set Blending to Additive, assign to glowMaterial. Otherwise leave null.
//  7. Tutorial hint: the script auto-creates a world-space text. To use your own
//     TextMeshProUGUI instead, assign hintText in the Inspector.
// ─────────────────────────────────────────────────────────────────────────────

[DefaultExecutionOrder(200)] // runs after LaneLayout (0) and HitLineFitter (100)
public class HitReceptorController : MonoBehaviour
{
    public static HitReceptorController Instance;

    [Header("Refs")]
    public LaneLayout laneLayout;
    public Transform hitLine;

    [Header("Lane Colors (leave empty to use TileSpawner defaults)")]
    public Color[] laneColors;

    [Header("Receptor Size")]
    [Tooltip("Receptor width as a fraction of the current tile width.")]
    [Range(0.1f, 1.0f)] public float widthFraction  = 1.0f;
    [Tooltip("Receptor height as a fraction of the current tile height.")]
    [Range(0.1f, 1.5f)] public float heightFraction = 0.55f;
    [Tooltip("World-units thick for the outline border strips.")]
    public float borderThicknessWorld = 0.06f;

    [Header("Receptor Appearance")]
    [Range(0f, 1f)] public float fillAlpha    = 0.10f;
    [Range(0f, 1f)] public float outlineAlpha = 0.80f;
    [Tooltip("Optional additive/glow material for all receptor sprites. " +
             "Leave null to use the default sprite material.")]
    public Material glowMaterial;
    public int sortingOrder = -1;
    public string sortingLayerName = "";

    [Header("Pulse Animation")]
    [Range(1f, 2f)] public float pulseScalePeak = 1.20f;
    public float pulseDuration = 0.18f;

    [Header("Tutorial Hint")]
    public bool showTutorialHint = true;
    public float hintDuration    = 4f;
    [Tooltip("Assign a TextMeshProUGUI from your UI canvas, or leave null to " +
             "auto-create a world-space hint above the receptors.")]
    public TextMeshProUGUI hintText;

    // ── runtime ───────────────────────────────────────────────────────────────

    struct Receptor
    {
        public GameObject root;
        public SpriteRenderer fill;
        public SpriteRenderer borderTop, borderBottom, borderLeft, borderRight;
    }

    Receptor[] receptors;
    float[]    pulseTimers;   // counts down from pulseDuration → 0

    // tutorial
    GameObject hintRoot;
    float      hintTimer;
    bool       hintAlive;

    // ── lifecycle ─────────────────────────────────────────────────────────────

    void Awake()
    {
        Instance = this;

        if (laneColors == null || laneColors.Length == 0)
            BuildDefaultColors();
    }

    void Start()
    {
        if (laneLayout == null)
            laneLayout = FindFirstObjectByType<LaneLayout>();
        if (hitLine == null && GameManager.Instance != null)
            hitLine = GameManager.Instance.hitLine;

        int count = laneLayout != null ? laneLayout.lanes.Length : 4;
        receptors   = new Receptor[count];
        pulseTimers = new float[count];

        for (int i = 0; i < count; i++)
            receptors[i] = BuildReceptor(i);

        if (showTutorialHint)
            SpawnHint();
    }

    void Update()
    {
        // Advance pulse timers and apply scale.
        for (int i = 0; i < pulseTimers.Length; i++)
        {
            if (pulseTimers[i] <= 0f) continue;
            pulseTimers[i] -= Time.deltaTime;

            float t     = Mathf.Clamp01(pulseTimers[i] / pulseDuration);
            float scale = Mathf.Lerp(1f, pulseScalePeak, t);
            receptors[i].root.transform.localScale = Vector3.one * scale;

            // Boost outline alpha during pulse.
            float alphaBoost = Mathf.Lerp(outlineAlpha, 1f, t);
            SetOutlineAlpha(i, alphaBoost);

            if (pulseTimers[i] <= 0f)
            {
                receptors[i].root.transform.localScale = Vector3.one;
                SetOutlineAlpha(i, outlineAlpha);
            }
        }

        // Tutorial hint fade-out.
        if (hintAlive)
        {
            hintTimer -= Time.deltaTime;
            if (hintTimer <= 0f)
            {
                hintAlive = false;
                if (hintRoot != null) Destroy(hintRoot);
                if (hintText != null) hintText.gameObject.SetActive(false);
            }
            else if (hintTimer < 1f)
            {
                float a = hintTimer;
                if (hintText != null) { Color c = hintText.color; c.a = a; hintText.color = c; }
            }
        }
    }

    void LateUpdate()
    {
        if (laneLayout == null || hitLine == null) return;

        float tileW      = TileSizing.CurrentTileWidthWorld  * widthFraction;
        float tileH      = TileSizing.CurrentTileHeightWorld;
        float receptorH  = Mathf.Max(0.05f, tileH * heightFraction);
        float border     = Mathf.Min(borderThicknessWorld, receptorH * 0.25f);
        float hitY       = hitLine.position.y;

        for (int i = 0; i < receptors.Length && i < laneLayout.lanes.Length; i++)
        {
            float laneX = laneLayout.lanes[i].position.x;

            receptors[i].root.transform.position = new Vector3(laneX, hitY, 0f);

            // Fill — full receptor area.
            SetSpriteSize(receptors[i].fill,         tileW,          receptorH,         0f, 0f);

            // Border strips — centred on their respective edges.
            SetSpriteSize(receptors[i].borderTop,    tileW,          border,             0f, (receptorH - border) * 0.5f);
            SetSpriteSize(receptors[i].borderBottom, tileW,          border,             0f, -(receptorH - border) * 0.5f);
            SetSpriteSize(receptors[i].borderLeft,   border,         receptorH,          -(tileW - border) * 0.5f, 0f);
            SetSpriteSize(receptors[i].borderRight,  border,         receptorH,          (tileW - border) * 0.5f, 0f);
        }
    }

    // ── public API ────────────────────────────────────────────────────────────

    public void PulseReceptor(int laneIndex)
    {
        if (laneIndex < 0 || laneIndex >= pulseTimers.Length) return;
        pulseTimers[laneIndex] = pulseDuration;
    }

    // ── building ──────────────────────────────────────────────────────────────

    Receptor BuildReceptor(int laneIndex)
    {
        Color baseColor = GetLaneColor(laneIndex);

        var r = new Receptor();
        r.root = new GameObject($"Receptor_{laneIndex}");
        r.root.transform.SetParent(transform, worldPositionStays: false);

        r.fill        = MakeSprite(r.root, "Fill",        baseColor, fillAlpha);
        r.borderTop   = MakeSprite(r.root, "BorderTop",   baseColor, outlineAlpha);
        r.borderBottom= MakeSprite(r.root, "BorderBottom",baseColor, outlineAlpha);
        r.borderLeft  = MakeSprite(r.root, "BorderLeft",  baseColor, outlineAlpha);
        r.borderRight = MakeSprite(r.root, "BorderRight", baseColor, outlineAlpha);

        return r;
    }

    SpriteRenderer MakeSprite(GameObject parent, string childName, Color baseColor, float alpha)
    {
        var go = new GameObject(childName);
        go.transform.SetParent(parent.transform, worldPositionStays: false);

        var sr    = go.AddComponent<SpriteRenderer>();
        var tex   = Texture2D.whiteTexture;
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                                  new Vector2(0.5f, 0.5f), 1f);

        Color c = baseColor;
        c.a     = alpha;
        sr.color = c;

        if (glowMaterial != null) sr.material = glowMaterial;

        if (!string.IsNullOrEmpty(sortingLayerName)) sr.sortingLayerName = sortingLayerName;
        sr.sortingOrder = sortingOrder;

        return sr;
    }

    // Sets a child sprite's local position and scale to match world dimensions.
    // (All receptors share scale=1 on the root, so local scale == world scale.)
    static void SetSpriteSize(SpriteRenderer sr, float w, float h, float localX, float localY)
    {
        if (sr == null) return;
        sr.transform.localPosition = new Vector3(localX, localY, 0f);
        sr.transform.localScale    = new Vector3(w, h, 1f);
    }

    void SetOutlineAlpha(int i, float alpha)
    {
        SetAlpha(receptors[i].borderTop,    alpha);
        SetAlpha(receptors[i].borderBottom, alpha);
        SetAlpha(receptors[i].borderLeft,   alpha);
        SetAlpha(receptors[i].borderRight,  alpha);
    }

    static void SetAlpha(SpriteRenderer sr, float alpha)
    {
        if (sr == null) return;
        Color c = sr.color;
        c.a     = alpha;
        sr.color = c;
    }

    // ── tutorial hint ─────────────────────────────────────────────────────────

    void SpawnHint()
    {
        hintTimer = hintDuration;
        hintAlive = true;

        if (hintText != null)
        {
            hintText.text = "Hit notes when they reach the glowing targets";
            hintText.gameObject.SetActive(true);
            return;
        }

        // Auto-create a world-space canvas text above the playfield.
        hintRoot = new GameObject("TutorialHint");
        hintRoot.transform.SetParent(transform, worldPositionStays: false);

        var canvas = hintRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 50;

        var canvasRT         = hintRoot.GetComponent<RectTransform>();
        canvasRT.sizeDelta   = new Vector2(8f, 1f);
        canvasRT.localScale  = Vector3.one * 0.01f; // 1 unit = 100 canvas px

        // Position it above the hitline, near top-third of screen.
        float topY = Camera.main != null ? Camera.main.transform.position.y + Camera.main.orthographicSize * 0.5f : 2f;
        hintRoot.transform.position = new Vector3(0f, topY, 0f);

        var textGO  = new GameObject("Text");
        textGO.transform.SetParent(hintRoot.transform, worldPositionStays: false);
        hintText    = textGO.AddComponent<TextMeshProUGUI>();
        hintText.text      = "Hit notes when they reach the glowing targets";
        hintText.fontSize  = 32;
        hintText.alignment = TextAlignmentOptions.Center;
        hintText.color     = new Color(1f, 1f, 1f, 1f);

        var rt          = textGO.GetComponent<RectTransform>();
        rt.sizeDelta    = new Vector2(800f, 100f);
        rt.localPosition= Vector3.zero;
    }

    // ── colors ────────────────────────────────────────────────────────────────

    Color GetLaneColor(int i)
    {
        if (laneColors != null && i < laneColors.Length) return laneColors[i];
        return DefaultColor(i);
    }

    static Color DefaultColor(int i)
    {
        Color c = Color.white;
        switch (i)
        {
            case 0: ColorUtility.TryParseHtmlString("#35E0FF", out c); break;
            case 1: ColorUtility.TryParseHtmlString("#FF3DAA", out c); break;
            case 2: ColorUtility.TryParseHtmlString("#A66BFF", out c); break;
            case 3: ColorUtility.TryParseHtmlString("#FF9A3D", out c); break;
        }
        return c;
    }

    void BuildDefaultColors()
    {
        laneColors = new Color[4];
        for (int i = 0; i < 4; i++) laneColors[i] = DefaultColor(i);
    }
}
