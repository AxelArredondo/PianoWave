using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// Creates four invisible, full-height lane panels in a Screen Space Overlay canvas.
// Add this component to any GameObject in the gameplay scene.
//
// IMPORTANT — Sort Order:
//   This canvas must have a LOWER Sort Order than your gameplay UI canvas
//   (pause menu, settings button, etc.) so those buttons receive pointer events first.
//   Default is 0; set your gameplay UI canvas to 1 or higher.
public class LanePanelSetup : MonoBehaviour
{
    [Tooltip("Sort Order for the lane panel canvas. Must be LOWER than your gameplay UI canvas.")]
    [SerializeField] private int canvasSortOrder = 0;

    [Tooltip("Must match the number of lanes in LaneLayout.")]
    [SerializeField] private int laneCount = 4;

    // Queried by InputManager to know whether to bypass raw Touchscreen input on Android.
    public static bool PanelsCreated { get; private set; }

    void Awake()
    {
        PanelsCreated = false;
        Build();
    }

    void OnDestroy()
    {
        PanelsCreated = false;
    }

    void Build()
    {
        // Ensure an EventSystem exists — required for UI pointer events.
        if (FindFirstObjectByType<EventSystem>() == null)
        {
            var esGO = new GameObject("EventSystem");
            esGO.AddComponent<EventSystem>();
            esGO.AddComponent<StandaloneInputModule>();
            Debug.Log("[LanePanelSetup] Created EventSystem (none was present).");
        }

        // Dedicated canvas at a low Sort Order so existing gameplay UI sits above it.
        var canvasGO = new GameObject("LanePanelCanvas");
        canvasGO.transform.SetParent(transform, false);

        var canvas            = canvasGO.AddComponent<Canvas>();
        canvas.renderMode     = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder   = canvasSortOrder;

        // CanvasScaler with anchor-based layout is independent of reference resolution,
        // but it is standard practice to include one.
        var scaler                  = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode          = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution  = new Vector2(1080, 1920);
        scaler.screenMatchMode      = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight   = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // Each panel spans 1/laneCount of the screen width, full height.
        float w = 1f / laneCount;
        for (int i = 0; i < laneCount; i++)
        {
            var panelGO = new GameObject($"LanePanel_{i}");
            panelGO.transform.SetParent(canvasGO.transform, false);

            var rt         = panelGO.AddComponent<RectTransform>();
            rt.anchorMin   = new Vector2(i * w,       0f);
            rt.anchorMax   = new Vector2((i + 1) * w, 1f);
            rt.offsetMin   = Vector2.zero;
            rt.offsetMax   = Vector2.zero;

            // Transparent image — invisible but raycastable.
            var img           = panelGO.AddComponent<Image>();
            img.color         = new Color(0f, 0f, 0f, 0f);
            img.raycastTarget = true;

            var panel       = panelGO.AddComponent<LanePanelInput>();
            panel.laneIndex = i;
        }

        PanelsCreated = true;
        Debug.Log($"[LanePanelSetup] {laneCount} lane panels created (sortOrder={canvasSortOrder}).");
    }
}
