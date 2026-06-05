using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// Attach to the Canvas GameObject.
/// Drag your art assets into the fields below in the Inspector.
public class MainMenuUISetup : MonoBehaviour
{
    // ── BACKGROUND LAYER ────────────────────────────────────────────────────────
    [Header("Background Layer")]
    [Tooltip("DRAG → MainBackgroundImage\nYour main synthwave background sprite. Covers the full screen.")]
    public Image mainBackgroundImage;

    [Tooltip("DRAG → ForegroundFXImage\nOptional foreground FX layer (particles, scanlines, stars). Pulses slowly.")]
    public Image foregroundFXImage;

    [Header("Background Position")]
    [Tooltip("Shifts all background layers up (positive) or down (negative) in canvas units. " +
             "Set images to anchoredPosition Y = 0 in the scene; tune here.")]
    public float heightOffset = 0f;

    // ── TV FRAME ────────────────────────────────────────────────────────────────
    [Header("TV Frame")]
    [Tooltip("DRAG → CRTFrameImage\nYour retro CRT TV frame sprite. Covers the TVLayer RectTransform.")]
    public Image crtFrameImage;

    [Tooltip("DRAG → CRTScreenOverlayImage\nScanlines / glare overlay inside the TV screen.\nRaycast Target is forced OFF automatically so buttons still work.")]
    public Image crtScreenOverlayImage;

    [Tooltip("DRAG → CRTScreenOverlayImageFull\nFull-alpha overlay, hidden by default. Shown at full opacity when power is on.")]
    public Image crtScreenOverlayImageFull;

    // ── LOGO ────────────────────────────────────────────────────────────────────
    [Header("Logo / Title")]
    [Tooltip("DRAG → PianoWaveLogoImage\nYour PianoWave logo sprite. Placed at the top of the TV screen area.")]
    public Image logoImage;

    // ── BUTTONS ─────────────────────────────────────────────────────────────────
    [Header("Buttons")]
    [Tooltip("DRAG → LevelsButton\nThe button that starts Level 1 (LevelMode). Must have a Button component.")]
    public Button levelsButton;

    [Tooltip("DRAG → EndlessButton\nThe button that starts Endless mode (RandomMode). Must have a Button component.")]
    public Button endlessButton;

    [Tooltip("DRAG → SettingsButton\nThe button that opens the Settings panel. Must have a Button component.")]
    public Button settingsButton;

    // ── LOGO PULSE ──────────────────────────────────────────────────────────────
    [Header("Logo Pulse")]
    [Range(0.2f, 1f)]  public float logoPulseMin   = 0.65f;
    [Range(0.5f, 3f)]  public float logoPulseSpeed = 0.80f;

    // ── FOREGROUND FX PULSE ─────────────────────────────────────────────────────
    [Header("Foreground FX Pulse")]
    [Range(0f, 1f)]   public float fxPulseMin   = 0.00f;
    [Range(0f, 1f)]   public float fxPulseMax   = 0.45f;
    [Range(0.1f, 1f)] public float fxPulseSpeed = 0.35f;

    // ── CRT FLICKER ─────────────────────────────────────────────────────────────
    [Header("CRT Overlay Flicker")]
    [Range(0f, 1f)]    public float crtBaseAlpha       = 0.55f;
    [Range(0f, 0.3f)]  public float crtFlickerStrength = 0.08f;
    [Range(0.05f, 0.5f)] public float crtFlickerInterval = 0.12f;

    float _crtBaseAlphaDefault;
    float _crtFlickerStrengthDefault;

    // ── BUTTON GLOW ─────────────────────────────────────────────────────────────
    [Header("Button Glow — Main Menu (optional)")]
    [Tooltip("DRAG → a separate glow Image child inside LevelsButton (not the button image itself).")]
    public Image levelsButtonGlow;
    [Tooltip("DRAG → a separate glow Image child inside EndlessButton.")]
    public Image endlessButtonGlow;
    [Tooltip("DRAG → a separate glow Image child inside SettingsButton.")]
    public Image settingsButtonGlow;

    [Header("Button Glow — Level Select (optional)")]
    [Tooltip("DRAG → a separate glow Image child inside Level1Button.")]
    public Image level1ButtonGlow;
    [Tooltip("DRAG → a separate glow Image child inside BackButton.")]
    public Image backButtonGlow;

    [Header("Button Glow Settings")]
    [Range(0f, 1f)]  public float glowIdleAlpha  = 0.00f;
    [Range(0f, 1f)]  public float glowHoverAlpha = 0.65f;
    [Range(1f, 15f)] public float glowFadeSpeed  = 8f;

    // ────────────────────────────────────────────────────────────────────────────

    void Awake()
    {
        if (crtScreenOverlayImage != null)
            crtScreenOverlayImage.raycastTarget = false;

        if (crtScreenOverlayImageFull != null)
        {
            crtScreenOverlayImageFull.raycastTarget = false;
            var c = crtScreenOverlayImageFull.color;
            c.a = 0f;
            crtScreenOverlayImageFull.color = c;
        }
    }

    void Start()
    {
        _crtBaseAlphaDefault = crtBaseAlpha;
        _crtFlickerStrengthDefault = crtFlickerStrength;
        LogInspectorGuide();
        ApplyHeightOffset();
        StartAnimations();
        SetupButtonGlows();
    }

    // Called by PowerButton when toggled. When on, flicker range is [0.9, 1.0].
    public void SetCRTPoweredOn(bool on)
    {
        if (on)
        {
            crtBaseAlpha       = 0.95f;
            crtFlickerStrength = 0.05f;
        }
        else
        {
            crtBaseAlpha       = _crtBaseAlphaDefault;
            crtFlickerStrength = _crtFlickerStrengthDefault;
        }

        if (crtScreenOverlayImageFull != null)
        {
            var c = crtScreenOverlayImageFull.color;
            c.a = on ? 1f : 0f;
            crtScreenOverlayImageFull.color = c;
            crtScreenOverlayImageFull.raycastTarget = on;
        }
    }

    void ApplyHeightOffset()
    {
        ShiftImage(foregroundFXImage);
    }

    void ShiftImage(Graphic g)
    {
        if (g == null) return;
        var pos = g.rectTransform.anchoredPosition;
        pos.y = heightOffset;
        g.rectTransform.anchoredPosition = pos;
    }

    void LogInspectorGuide()
    {
        Debug.Log(
            "[MainMenuUISetup] Inspector asset assignment:\n" +
            "  mainBackgroundImage   ← MainBackgroundImage   (full-screen synthwave BG)\n" +
            "  foregroundFXImage     ← ForegroundFXImage     (optional FX layer, can be null)\n" +
            "  crtFrameImage         ← CRTFrameImage         (retro TV border sprite)\n" +
            "  crtScreenOverlayImage ← CRTScreenOverlayImage (scanlines, auto raycast=OFF)\n" +
            "  logoImage             ← PianoWaveLogoImage    (your logo sprite)\n" +
            "  levelsButton          ← LevelsButton          (opens Level Select panel)\n" +
            "  endlessButton         ← EndlessButton         (starts RandomMode)\n" +
            "  settingsButton        ← SettingsButton        (opens Settings panel)\n" +
            "  level1ButtonGlow      ← Glow image inside Level1Button (optional)\n" +
            "  backButtonGlow        ← Glow image inside BackButton   (optional)"
        );
    }

    void StartAnimations()
    {
        if (logoImage != null)
            StartCoroutine(PulseAlpha(logoImage, logoPulseMin, 1f, logoPulseSpeed));

        if (foregroundFXImage != null)
            StartCoroutine(PulseAlpha(foregroundFXImage, fxPulseMin, fxPulseMax, fxPulseSpeed));

        if (crtScreenOverlayImage != null)
            StartCoroutine(CRTFlicker());
    }

    IEnumerator PulseAlpha(Graphic g, float min, float max, float speed)
    {
        float t = 0f;
        while (true)
        {
            t += Time.deltaTime * speed;
            Color c = g.color;
            c.a = Mathf.Lerp(min, max, (Mathf.Sin(t) + 1f) * 0.5f);
            g.color = c;
            yield return null;
        }
    }

    IEnumerator CRTFlicker()
    {
        while (true)
        {
            Color c = crtScreenOverlayImage.color;
            c.a = Mathf.Clamp01(crtBaseAlpha + Random.Range(-crtFlickerStrength, crtFlickerStrength));
            crtScreenOverlayImage.color = c;
            yield return new WaitForSeconds(crtFlickerInterval * Random.Range(0.5f, 1.5f));
        }
    }

    void SetupButtonGlows()
    {
        WireGlow(levelsButton, levelsButtonGlow);
        WireGlow(endlessButton, endlessButtonGlow);
        WireGlow(settingsButton, settingsButtonGlow);
        WireGlow(FindButton("Level1Button"), level1ButtonGlow);
        WireGlow(FindButton("BackButton"),   backButtonGlow);
    }

    Button FindButton(string n)
    {
        foreach (var b in FindObjectsByType<Button>(FindObjectsSortMode.None))
            if (b.name == n) return b;
        return null;
    }

    void WireGlow(Button btn, Image glow)
    {
        if (btn == null || glow == null) return;
        Color c = glow.color;
        c.a = glowIdleAlpha;
        glow.color = c;
        glow.raycastTarget = false;

        var h = btn.gameObject.GetComponent<ButtonGlowHandler>();
        if (h == null) h = btn.gameObject.AddComponent<ButtonGlowHandler>();
        h.glowImage  = glow;
        h.idleAlpha  = glowIdleAlpha;
        h.hoverAlpha = glowHoverAlpha;
        h.fadeSpeed  = glowFadeSpeed;
    }
}

// Added to the button GO at runtime by MainMenuUISetup — hidden from Add Component menu.
[AddComponentMenu("")]
public class ButtonGlowHandler : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler
{
    [HideInInspector] public Image glowImage;
    [HideInInspector] public float idleAlpha;
    [HideInInspector] public float hoverAlpha;
    [HideInInspector] public float fadeSpeed;

    float _target;

    void Start() => _target = idleAlpha;

    void Update()
    {
        if (glowImage == null) return;
        Color c = glowImage.color;
        c.a = Mathf.MoveTowards(c.a, _target, fadeSpeed * Time.deltaTime);
        glowImage.color = c;
    }

    public void OnPointerEnter(PointerEventData _) => _target = hoverAlpha;
    public void OnPointerExit(PointerEventData _)  => _target = idleAlpha;
    public void OnPointerDown(PointerEventData _)  => _target = Mathf.Min(hoverAlpha * 1.35f, 1f);
    public void OnPointerUp(PointerEventData _)    => _target = hoverAlpha;
}
