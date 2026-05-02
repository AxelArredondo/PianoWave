using UnityEngine;

/// <summary>
/// Scales a background sprite to fill the WideBackgroundCamera's full view.
///
/// Attach to BGSky, BGGrid, BGSun, or any sprite that should cover the
/// widescreen background. Choose a FitMode appropriate to the art:
///   Cover   — fills entire screen, may crop sprite edges (good for sky/solid fills)
///   FillWidth  — fits width, keeps aspect (may leave top/bottom uncovered)
///   FillHeight — fits height, keeps aspect (may leave sides uncovered)
///   Stretch — ignores aspect entirely (only safe for solid colours or gradients)
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class WideBackgroundFiller : MonoBehaviour
{
    public enum FitMode
    {
        Cover,      // scale so both dimensions are >= camera size (safe default, may crop)
        FillWidth,  // uniform scale from width match (maintains aspect)
        FillHeight, // uniform scale from height match (maintains aspect)
        Stretch,    // independent X/Y scale (use only for flat colour or gradient sprites)
    }

    [Tooltip("The WideBackgroundCamera this sprite should fill.")]
    public Camera backgroundCamera;

    public FitMode fitMode = FitMode.Cover;

    private SpriteRenderer sr;
    private int lastW, lastH;
    private float lastOrtho;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    void LateUpdate()
    {
        if (backgroundCamera == null || sr == null || sr.sprite == null) return;

        float ortho = backgroundCamera.orthographicSize;
        if (Screen.width == lastW && Screen.height == lastH && Mathf.Approximately(ortho, lastOrtho))
            return;

        lastW = Screen.width;
        lastH = Screen.height;
        lastOrtho = ortho;

        Fit(ortho);
    }

    void Fit(float ortho)
    {
        // Camera's full visible world size (background camera is always full-screen).
        float camW = ortho * backgroundCamera.aspect * 2f;
        float camH = ortho * 2f;

        Vector2 native = sr.sprite.bounds.size;
        if (native.x < 0.0001f || native.y < 0.0001f) return;

        float scaleX = camW / native.x;
        float scaleY = camH / native.y;

        switch (fitMode)
        {
            case FitMode.Cover:
                float cover = Mathf.Max(scaleX, scaleY);
                transform.localScale = new Vector3(cover, cover, 1f);
                break;
            case FitMode.FillWidth:
                transform.localScale = new Vector3(scaleX, scaleX, 1f);
                break;
            case FitMode.FillHeight:
                transform.localScale = new Vector3(scaleY, scaleY, 1f);
                break;
            case FitMode.Stretch:
                transform.localScale = new Vector3(scaleX, scaleY, 1f);
                break;
        }
    }
}
