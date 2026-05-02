using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class LaneBackgroundFitter : MonoBehaviour
{
    [Header("Refs")]
    public LaneGuides laneGuides;
    public Camera cam;
    public Transform hitLine;

    [Header("Padding (world units)")]
    [Tooltip("Positive trims inward from BOTH left and right edges. Negative expands outward.")]
    public float horizontalInset = 0f;

    [Tooltip("Positive trims inward from the top. Negative expands upward.")]
    public float topInset = 0f;

    [Tooltip("Positive raises the bottom upward. Negative extends it downward.")]
    public float bottomInset = 0f;

    [Header("Bottom Control")]
    [Tooltip("If true, the background ends at the hitline instead of the bottom of the camera.")]
    public bool endAtHitLine = true;

    private SpriteRenderer sr;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (cam == null) cam = Camera.main;
    }

    void LateUpdate()
    {
        if (laneGuides == null) return;
        if (cam == null) cam = Camera.main;
        if (cam == null || !cam.orthographic) return;
        if (sr == null || sr.sprite == null) return;

        // Get exact left/right edges from guides
        float left = laneGuides.LeftEdgeWorld + horizontalInset;
        float right = laneGuides.RightEdgeWorld - horizontalInset;

        // Prevent inversion if inset is too large
        if (right <= left)
            right = left + 0.01f;

        float targetWidth = right - left;
        float centerX = (left + right) * 0.5f;

        // Top stays based on camera view
        float halfH = cam.orthographicSize;
        float top = cam.transform.position.y + halfH - topInset;

        // Bottom can end at hitline or camera bottom
        float bottom;
        if (endAtHitLine && hitLine != null)
            bottom = hitLine.position.y + bottomInset;
        else
            bottom = cam.transform.position.y - halfH + bottomInset;

        if (top <= bottom)
            top = bottom + 0.01f;

        float targetHeight = top - bottom;
        float centerY = (top + bottom) * 0.5f;

        // Native sprite size in world units at scale 1
        Vector2 nativeSize = sr.sprite.bounds.size;

        float scaleX = targetWidth / nativeSize.x;
        float scaleY = targetHeight / nativeSize.y;

        transform.position = new Vector3(centerX, centerY, transform.position.z);
        transform.localScale = new Vector3(scaleX, scaleY, 1f);
    }
}