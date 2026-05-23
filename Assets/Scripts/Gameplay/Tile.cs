using UnityEngine;
using System.Collections.Generic;

public class Tile : MonoBehaviour
{
    public static readonly List<Tile> ActiveTiles = new List<Tile>();

    [Header("Movement")]
    public float baseSpeed = 5f;

    [Header("Hit Accuracy Windows (percent of TILE height)")]
    [Tooltip("Perfect window as a percent of the tile height (measured from tile center).")]
    [Range(0.01f, 0.50f)]
    public float perfectPercentOfTileHeight = 0.12f;

    [Tooltip("Good window as a percent of the tile height (measured from the tile edges).")]
    [Range(0.00f, 1f)]
    public float goodPercentOfTileHeight = 0.20f;

    private bool hit = false;
    private Transform hitLine;

    [Header("Visuals")]
    public SpriteRenderer tileRenderer;

    private int laneIndex = -1;
    private Color laneColor = Color.white;
    private string noteType = "tap";
    private float durationBeats = 0f;

    [Header("Hold Note")]
    [Tooltip("Alpha of the hold body strip. 0 = invisible, 1 = fully opaque.")]
    [Range(0f, 1f)] public float holdBodyAlpha = 0.55f;
    [Tooltip("Bonus score awarded when the player completes a full hold.")]
    public int holdBonusScore = 200;

    // ── hold runtime state ─────────────────────────────────────────────────────
    private bool holdActive  = false;
    private float holdElapsed  = 0f;
    private float holdDuration = 0f;  // seconds
    private int   holdLaneIndex = -1;
    private SpriteRenderer holdBodyRenderer;

    // ── note type summary ──────────────────────────────────────────────────────
    // "tap"    — normal note.
    // "quick"  — 60% height, brighter color, smaller hit window.
    // "accent" — normal size, boosted color, bigger hit FX, particles on Good too.
    // "hold"   — head stays normal size; a body strip is generated above it.
    //            Head scoring is Perfect/Good/Miss (same timing window as tap).
    //            Holding after the head hit earns holdBonusScore; releasing early
    //            loses the bonus but does NOT cost an attempt.

    // Called by ChartSpawner / TileSpawner right after Instantiate + ApplyTileScale.
    public void Init(int lane, Color baseColor, string noteType = "tap", float durationBeats = 0f)
    {
        laneIndex          = lane;
        this.noteType      = noteType;
        this.durationBeats = durationBeats;
        laneColor          = ModifyColorForType(baseColor);

        // Cache head renderer NOW before BuildHoldBody adds a second child renderer.
        // This guarantees GetComponentInChildren finds the head, not the body.
        if (tileRenderer == null)
            tileRenderer = GetComponentInChildren<SpriteRenderer>();
        if (tileRenderer != null)
            tileRenderer.color = laneColor;

        ApplyTypeScale();
    }

    // ── visual variants ────────────────────────────────────────────────────────

    Color ModifyColorForType(Color baseColor)
    {
        Color.RGBToHSV(baseColor, out float h, out float s, out float v);
        switch (noteType)
        {
            case "quick":
                v = Mathf.Min(1f, v + 0.18f);
                s = Mathf.Max(0f, s - 0.08f);
                break;
            case "accent":
                v = Mathf.Min(1f, v + 0.30f);
                s = Mathf.Min(1f, s + 0.12f);
                break;
        }
        return Color.HSVToRGB(h, s, v);
    }

    void ApplyTypeScale()
    {
        switch (noteType)
        {
            case "quick":
            {
                Vector3 s = transform.localScale;
                s.y *= 0.60f;
                transform.localScale = s;
                break;
            }
            case "hold" when durationBeats > 0f:
                BuildHoldBody();
                break;
        }
    }

    // Generates a child strip that represents the hold body.
    // The head (tileRenderer) stays at normal tile height — only the body is added on top.
    void BuildHoldBody()
    {
        float tileH = TileSizing.CurrentTileHeightWorld;
        float tileW = TileSizing.CurrentTileWidthWorld;
        float gap   = tileH * 0.15f;               // mirrors spawnGapFraction default
        float bodyH = (tileH + gap) * durationBeats;
        if (bodyH < 0.001f) return;

        Vector3 ps = transform.localScale;
        if (ps.x < 0.0001f || ps.y < 0.0001f) return;

        var bodyGO = new GameObject("HoldBody");
        bodyGO.transform.SetParent(transform, worldPositionStays: false);

        var sr = bodyGO.AddComponent<SpriteRenderer>();
        Texture2D tex = Texture2D.whiteTexture;
        sr.sprite = Sprite.Create(tex,
            new Rect(0, 0, tex.width, tex.height),
            new Vector2(0.5f, 0.5f),
            (float)tex.width);  // PPU = tex.width → native world size = 1×1

        Color bodyColor = laneColor;
        bodyColor.a = holdBodyAlpha;
        sr.color = bodyColor;
        sr.sortingOrder = -1;   // render behind the head

        // Child world size = child.localScale × parent.localScale.
        // To achieve world dimensions (tileW, bodyH): localScale = (tileW/ps.x, bodyH/ps.y).
        bodyGO.transform.localScale = new Vector3(tileW / ps.x, bodyH / ps.y, 1f);

        // Body center must sit tileH/2 + bodyH/2 above the head center in world space.
        // Convert to local space by dividing by parent scale.
        bodyGO.transform.localPosition = new Vector3(
            0f,
            (tileH * 0.5f + bodyH * 0.5f) / ps.y,
            0.1f);  // slight Z so body renders behind head on the same layer

        holdBodyRenderer = sr;
        holdLaneIndex    = laneIndex;
        holdDuration     = durationBeats * (BeatManager.Instance != null
            ? BeatManager.Instance.SecondsPerBeat : 0.5f);
    }

    // ── Unity lifecycle ────────────────────────────────────────────────────────

    [Header("Hit FX")]
    public float hitDestroyDelay = 0.08f;
    public float hitScalePerfect = 1.35f;
    public float hitScaleGood    = 1.12f;

    [Tooltip("Particle prefab plays on PERFECT (and on GOOD for accent notes).")]
    public GameObject hitParticlePrefab;

    [Tooltip("TRUE = particles spawn at the hitline; FALSE = at the tile position.")]
    public bool spawnParticlesAtHitLine = true;

    [Tooltip("Fallback particle lifetime if the prefab doesn't self-destroy.")]
    public float particleLifetime = 1.0f;

    void Awake()
    {
        ActiveTiles.Add(this);
    }

    void OnDestroy()
    {
        ActiveTiles.Remove(this);
    }

    void Start()
    {
        // tileRenderer is usually already set by Init; this is a fallback.
        if (tileRenderer == null)
            tileRenderer = GetComponentInChildren<SpriteRenderer>();

        if (tileRenderer != null)
            tileRenderer.color = laneColor;
        else
            Debug.LogWarning("Tile: tileRenderer missing on " + gameObject.name);

        hitLine = GameManager.Instance.hitLine;
    }

    void Update()
    {
        if (GameManager.Instance.IsGameOver || GameManager.Instance.IsPaused)
            return;

        // BeatManager.bpm/120 keeps random-mode tiles matched to the beat frequency.
        // ChartSpawner.SpeedMultiplier adds a visual-only speed layer for level-mode speed events.
        float speedMultiplier = BeatManager.Instance.bpm / 120f * ChartSpawner.SpeedMultiplier;
        transform.Translate(Vector3.down * baseSpeed * speedMultiplier * Time.deltaTime);

        if (holdActive)
        {
            UpdateHold();
            return;  // skip normal miss detection while hold is in progress
        }

        // Auto-miss the instant the TOP of the head sprite clears the hitline.
        if (!hit && tileRenderer != null && hitLine != null)
        {
            if (tileRenderer.bounds.max.y <= hitLine.position.y)
                Miss();
        }
    }

    // ── hold state machine ─────────────────────────────────────────────────────

    void UpdateHold()
    {
        bool isHeld = holdLaneIndex >= 0
            && holdLaneIndex < InputManager.LaneHeld.Length
            && InputManager.LaneHeld[holdLaneIndex];

        if (!isHeld) { AbandonHold(); return; }

        holdElapsed += Time.deltaTime;
        if (holdElapsed >= holdDuration) FinalizeHold();
    }

    void StartHold()
    {
        holdActive  = true;
        holdElapsed = 0f;

        // After the brief white flash from PlayHitFX, hide the head.
        Invoke(nameof(HideHead), hitDestroyDelay);

        // Make the body slightly brighter to signal an active hold.
        if (holdBodyRenderer != null)
        {
            Color c = holdBodyRenderer.color;
            c.a = Mathf.Min(1f, holdBodyAlpha + 0.20f);
            holdBodyRenderer.color = c;
        }
    }

    void HideHead()
    {
        if (tileRenderer != null) tileRenderer.enabled = false;
    }

    void FinalizeHold()
    {
        holdActive = false;
        GameManager.Instance.RegisterHoldBonus(holdBonusScore);
        Destroy(gameObject, hitDestroyDelay);
    }

    // Early release — no attempt penalty, no bonus.
    void AbandonHold()
    {
        holdActive = false;
        Destroy(gameObject, hitDestroyDelay);
    }

    // ── hit detection ──────────────────────────────────────────────────────────

    public void Hit()
    {
        if (hit) return;
        if (GameManager.Instance.IsGameOver || GameManager.Instance.IsPaused) return;

        if (tileRenderer == null)
            tileRenderer = GetComponentInChildren<SpriteRenderer>();

        if (tileRenderer == null || hitLine == null) { Miss(); return; }

        float hitY    = hitLine.position.y;
        float topY    = tileRenderer.bounds.max.y;
        float bottomY = tileRenderer.bounds.min.y;

        if (hitY < bottomY || hitY > topY) { Miss(); return; }

        float tileHeight        = Mathf.Max(0.0001f, topY - bottomY);
        float perfectWindow     = tileHeight * perfectPercentOfTileHeight;
        float distToCenter      = Mathf.Abs(tileRenderer.bounds.center.y - hitY);

        hit = true;
        if (distToCenter <= perfectWindow) Perfect();
        else                               Good();
    }

    void Perfect()
    {
        GameManager.Instance.RegisterPerfect();
        float scale  = noteType == "accent" ? hitScalePerfect * 1.40f : hitScalePerfect;
        bool  isHold = noteType == "hold" && durationBeats > 0f;
        PlayHitFX(scale, destroyAfter: !isHold);

        if (hitParticlePrefab != null)
            SpawnParticle();

        if (isHold) StartHold();
    }

    void Good()
    {
        GameManager.Instance.RegisterGood();
        float scale  = noteType == "accent" ? hitScaleGood * 1.30f : hitScaleGood;
        bool  isHold = noteType == "hold" && durationBeats > 0f;
        PlayHitFX(scale, destroyAfter: !isHold);

        // Accent emits particles on Good hits too.
        if (noteType == "accent" && hitParticlePrefab != null)
            SpawnParticle();

        if (isHold) StartHold();
    }

    void Miss()
    {
        if (hit) return;
        if (GameManager.Instance.IsGameOver || GameManager.Instance.IsPaused) return;

        hit = true;
        GameManager.Instance.RegisterMiss();
        GameManager.Instance.MissTile();
        Destroy(gameObject);
    }

    void PlayHitFX(float scale, bool destroyAfter = true)
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        // Don't scale the whole tile for hold notes — scaling the parent would
        // also resize the body child, which looks wrong.
        if (noteType != "hold")
            transform.localScale *= scale;

        if (tileRenderer != null)
            tileRenderer.color = Color.white;

        if (destroyAfter) Destroy(gameObject, hitDestroyDelay);
    }

    void SpawnParticle()
    {
        Vector3 pos = (spawnParticlesAtHitLine && hitLine != null)
            ? hitLine.position : transform.position;
        GameObject fx = Instantiate(hitParticlePrefab, pos, Quaternion.identity);
        Destroy(fx, particleLifetime);
    }
}
