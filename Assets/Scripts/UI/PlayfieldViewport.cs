using UnityEngine;
using System.Collections;

/// <summary>
/// Constrains the gameplay camera to a centred column on widescreen / PC.
/// On mobile / portrait the camera uses the full screen as normal.
///
/// Column width and widescreen threshold are driven by PlayfieldLayout when
/// it is present in the scene; the fallback fields below are used otherwise.
/// </summary>
[DefaultExecutionOrder(-50)]
[RequireComponent(typeof(Camera))]
public class PlayfieldViewport : MonoBehaviour
{
    [Header("Fallback — only used when PlayfieldLayout is absent")]
    [Tooltip("Portrait reference aspect (w/h) used to derive column width.")]
    public float portraitReferenceAspect = 9f / 16f;

    [Tooltip("Screen aspects above this switch to widescreen/PC mode.")]
    public float widescreenThreshold = 0.75f;

    [Tooltip("Assign the background camera — enabled in widescreen mode, disabled on mobile.")]
    public Camera backgroundCamera;

    [Tooltip("GameObjects to show only on PC (widescreen). Drag BGSkyWide here.")]
    public GameObject[] pcOnlyObjects;

    [Tooltip("GameObjects to show only on mobile (portrait). Drag BGSky here.")]
    public GameObject[] mobileOnlyObjects;

    [Tooltip("Must match the layer name in Edit > Project Settings > Tags and Layers.")]
    public string backgroundLayerName = "Background";

    private Camera cam;
    private int lastW, lastH;

    void Awake()
    {
        cam = GetComponent<Camera>();

        if (LayerMask.NameToLayer(backgroundLayerName) < 0)
            Debug.LogWarning($"PlayfieldViewport: Layer '{backgroundLayerName}' not found. " +
                             "Create it in Edit > Project Settings > Tags and Layers.");
    }

    IEnumerator Start()
    {
        yield return new WaitForEndOfFrame();
        Apply();
        Cache();
    }

    void Update()
    {
        if (Screen.width != lastW || Screen.height != lastH)
        {
            Apply();
            Cache();
        }
    }

    void Cache() { lastW = Screen.width; lastH = Screen.height; }

    void Apply()
    {
        float screenAspect = (float)Screen.width / Screen.height;

        bool isWidescreen = PlayfieldLayout.Instance != null
            ? PlayfieldLayout.IsWidescreen
            : screenAspect > widescreenThreshold;

        // Single-camera approach: one camera renders background + gameplay for all platforms.
        // Background sprites (sorting order -30 to -5) sit behind gameplay sprites (order 0+)
        // within the same pass, eliminating all two-camera clear/depth artifacts.
        cam.rect = new Rect(0f, 0f, 1f, 1f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.ResetAspect();

        int bgLayer = LayerMask.NameToLayer(backgroundLayerName);
        if (bgLayer >= 0)
            cam.cullingMask |= (1 << bgLayer);

        // Background Camera not needed — keep it off.
        if (backgroundCamera != null)
            backgroundCamera.gameObject.SetActive(false);

        if (pcOnlyObjects != null)
            foreach (var obj in pcOnlyObjects)
                if (obj != null) obj.SetActive(isWidescreen);

        if (mobileOnlyObjects != null)
            foreach (var obj in mobileOnlyObjects)
                if (obj != null) obj.SetActive(!isWidescreen);
    }
}
