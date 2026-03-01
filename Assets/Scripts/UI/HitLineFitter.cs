using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class HitLineFitter : MonoBehaviour
{
    public Camera cam;

    [Header("Width source")]
    public bool limitWidthToTileArea = true;
    public LaneGuides laneGuides;

    [Header("Fallback (if not using tile area)")]
    [Range(0.1f, 1.0f)]
    public float widthPercentOfScreen = 0.95f;

    [Header("Position")]
    public float bottomPaddingWorld = 1.2f;

    [Header("Thickness")]
    public float thicknessPixels = 6f;

    SpriteRenderer sr;
    int lastW, lastH;
    float lastOrtho;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (cam == null) cam = Camera.main;
    }

    void LateUpdate()
    {
        if (cam == null || !cam.orthographic || sr == null || sr.sprite == null) return;

        if (Screen.width != lastW || Screen.height != lastH || !Mathf.Approximately(cam.orthographicSize, lastOrtho))
        {
            Fit();
            lastW = Screen.width;
            lastH = Screen.height;
            lastOrtho = cam.orthographicSize;
        }
    }

    void Fit()
    {
        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;
        float screenW = halfW * 2f;

        // Anchor in view
        Vector3 p = transform.position;
        p.y = cam.transform.position.y - halfH + bottomPaddingWorld;
        transform.position = p;

        // Width
        float targetWidthWorld;

        if (limitWidthToTileArea && laneGuides != null)
        {
            targetWidthWorld = laneGuides.RightEdgeWorld - laneGuides.LeftEdgeWorld;
        }
        else
        {
            targetWidthWorld = screenW * widthPercentOfScreen;
        }

        // Thickness in pixels
        float worldPerPixel = (halfH * 2f) / Screen.height;
        float targetThicknessWorld = worldPerPixel * thicknessPixels;

        Vector2 native = sr.sprite.bounds.size;
        float sx = targetWidthWorld / native.x;
        float sy = targetThicknessWorld / native.y;

        transform.localScale = new Vector3(sx, sy, 1f);
    }
}