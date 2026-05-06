using UnityEngine;
using System.Collections;

[DefaultExecutionOrder(200)]
public class WideBackgroundFiller : MonoBehaviour
{
    public enum FitMode
    {
        Cover,      // uniform scale so both axes >= camera size (may crop edges)
        Contain,    // uniform scale so both axes <= camera size (shows full image — may leave gaps)
        FillWidth,  // uniform scale matched to screen width  (maintains aspect)
        FillHeight, // uniform scale matched to screen height (maintains aspect)
        Stretch,    // independent X/Y scale (fills exactly — use for gradients or grid)
    }

    [Tooltip("Assign the WideBackgroundCamera (kept for scene wiring, not used for sizing).")]
    public Camera backgroundCamera;

    public FitMode fitMode = FitMode.Cover;

    [Header("Partial Height (e.g. ground grid that stops at horizon)")]
    [Tooltip("1 = full camera height. 0.4 = bottom 40% only. Only meaningful with Stretch or FillWidth.")]
    [Range(0.05f, 1f)]
    public float heightFraction = 1f;

    [Tooltip("Pin the sprite's bottom edge to the camera's bottom edge. " +
             "Use with heightFraction < 1 for ground / horizon elements.")]
    public bool anchorToBottom = false;

    [Tooltip("Pin the sprite's bottom edge to BackgroundHorizonLine.WorldY. " +
             "Use for city, sun, and any art that should sit on the horizon. " +
             "Mutually exclusive with Anchor To Bottom.")]
    public bool anchorBottomToHorizon = false;

    [Header("Manual Adjustments")]
    [Tooltip("Multiplies the computed scale uniformly. 1 = fills screen as calculated. 1.2 = 20% larger.")]
    public float scaleMultiplier = 1f;

    [Tooltip("World-unit offset added to the computed position when any anchor mode is active. " +
             "For non-anchored sprites, move the object directly in the Scene view instead.")]
    public Vector2 positionOffset = Vector2.zero;

    [Tooltip("Enable to print calculated values to the Console for debugging.")]
    public bool debugLog = false;

    private SpriteRenderer sr;
    private int lastW, lastH;
    private float lastOrtho;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr == null)
            sr = GetComponentInChildren<SpriteRenderer>();

        if (sr == null)
            Debug.LogWarning($"WideBackgroundFiller on '{name}': no SpriteRenderer found on " +
                             "this object or its children.");
    }

    IEnumerator Start()
    {
        yield return new WaitForEndOfFrame();
        yield return null;
        lastW = -1;
    }

    void LateUpdate()
    {
        if (sr == null || sr.sprite == null) return;

        Camera mainCam = Camera.main;
        if (mainCam == null) return;

        float ortho = mainCam.orthographicSize;

        if (Screen.width == lastW && Screen.height == lastH && Mathf.Approximately(ortho, lastOrtho))
            return;

        lastW     = Screen.width;
        lastH     = Screen.height;
        lastOrtho = ortho;

        Fit(mainCam, ortho);
    }

    void Fit(Camera mainCam, float ortho)
    {
        float screenAspect = (float)Screen.width / Screen.height;
        float camW = ortho * screenAspect * 2f;
        float camH = ortho * 2f;
        float targetH = camH * heightFraction;

        Vector2 native = sr.sprite.bounds.size;
        if (native.x < 0.0001f || native.y < 0.0001f) return;

        // Divide by parent world scale so the sprite's world size equals camW / targetH
        // regardless of what scale the parent hierarchy has.
        Vector3 parentWS = sr.transform.parent != null
            ? sr.transform.parent.lossyScale
            : Vector3.one;
        float px = Mathf.Abs(parentWS.x) > 0.0001f ? Mathf.Abs(parentWS.x) : 1f;
        float py = Mathf.Abs(parentWS.y) > 0.0001f ? Mathf.Abs(parentWS.y) : 1f;

        float scaleX = camW    / (native.x * px);
        float scaleY = targetH / (native.y * py);

        float finalSx, finalSy;

        switch (fitMode)
        {
            case FitMode.Cover:
                float s = Mathf.Max(scaleX, scaleY);
                finalSx = finalSy = s;
                break;
            case FitMode.Contain:
                float sc = Mathf.Min(scaleX, scaleY);
                finalSx = finalSy = sc;
                break;
            case FitMode.FillWidth:
                finalSx = finalSy = scaleX;
                break;
            case FitMode.FillHeight:
                finalSx = finalSy = scaleY;
                break;
            default: // Stretch
                finalSx = scaleX;
                finalSy = scaleY;
                break;
        }

        finalSx *= scaleMultiplier;
        finalSy *= scaleMultiplier;

        sr.transform.localScale = new Vector3(finalSx, finalSy, 1f);

        if (anchorToBottom)
        {
            // Rendered half-height in world units = parent_scale * localScale * native_h / 2
            float renderedHalfH = finalSy * py * native.y * 0.5f;
            float camBottomY    = mainCam.transform.position.y - ortho;
            Vector3 pos         = sr.transform.position;
            pos.y               = camBottomY + renderedHalfH + positionOffset.y;
            pos.x               = positionOffset.x;
            sr.transform.position = pos;
        }
        else if (anchorBottomToHorizon)
        {
            if (BackgroundHorizonLine.Instance == null)
            {
                Debug.LogWarning($"[WideBackgroundFiller] '{name}': anchorBottomToHorizon is set " +
                                 "but no BackgroundHorizonLine found in the scene.");
            }
            else
            {
                // Place sprite so its bottom edge sits exactly on the horizon Y.
                // center Y = horizonY + half the sprite's rendered height.
                float renderedHalfH   = finalSy * py * native.y * 0.5f;
                Vector3 pos           = sr.transform.position;
                pos.y                 = BackgroundHorizonLine.WorldY + renderedHalfH + positionOffset.y;
                pos.x                 = positionOffset.x;
                sr.transform.position = pos;
            }
        }

        if (debugLog)
            Debug.Log($"[WideBackgroundFiller] {name} | ortho={ortho:F2}  " +
                      $"screenAspect={screenAspect:F3}  camW={camW:F2}  camH={camH:F2}  " +
                      $"targetH={targetH:F2}  native=({native.x:F3},{native.y:F3})  " +
                      $"parentScale=({px:F3},{py:F3})  " +
                      $"finalScale=({finalSx:F3},{finalSy:F3})");
    }
}
