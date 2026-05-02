using UnityEngine;

/// <summary>
/// Attached to a dedicated background camera (depth -1, full-screen rect).
/// Renders only GameObjects on the "Background" layer so background art fills
/// the entire widescreen display, independent of the gameplay camera's portrait column.
///
/// Syncs its orthographic size and world position with the gameplay camera so
/// background sprites positioned in world space appear exactly where expected.
/// </summary>
[RequireComponent(typeof(Camera))]
public class WideBackgroundCamera : MonoBehaviour
{
    [Tooltip("The main gameplay camera (Camera.main). Used to sync ortho size and position.")]
    public Camera gameplayCamera;

    [Tooltip("Must match the layer name in PlayfieldViewport and your Tags & Layers settings.")]
    public string backgroundLayerName = "Background";

    [Tooltip("Colour shown in areas not covered by any background sprite.")]
    public Color clearColor = Color.black;

    private Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();

        cam.depth = -1;                              // renders before the gameplay camera
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = clearColor;
        cam.rect = new Rect(0f, 0f, 1f, 1f);        // always full screen
        cam.orthographic = true;

        // Only render objects on the Background layer.
        int bgLayer = LayerMask.NameToLayer(backgroundLayerName);
        if (bgLayer >= 0)
            cam.cullingMask = 1 << bgLayer;
        else
            Debug.LogWarning($"WideBackgroundCamera: Layer '{backgroundLayerName}' not found. " +
                             "Create it in Edit > Project Settings > Tags and Layers.");

        if (gameplayCamera == null)
            gameplayCamera = Camera.main;
    }

    void LateUpdate()
    {
        if (gameplayCamera == null) return;

        // Sync orthographic size so background world positions align with gameplay world positions.
        cam.orthographicSize = gameplayCamera.orthographicSize;

        // Sync XY position; keep our own Z so the camera doesn't move onto gameplay objects.
        Vector3 pos = cam.transform.position;
        pos.x = gameplayCamera.transform.position.x;
        pos.y = gameplayCamera.transform.position.y;
        cam.transform.position = pos;
    }
}
