using UnityEngine;

// Draws a rectangle outline + vertical dividers over the hit receptors.
// Automatically tracks size and position from HitReceptorController / TileSizing
// so it stays correct on any screen size.
[DefaultExecutionOrder(210)] // after HitReceptorController (200)
public class ReceptorGridOverlay : MonoBehaviour
{
    [Header("Refs (auto-found if left empty)")]
    public HitReceptorController receptorController;
    public LaneLayout laneLayout;

    [Header("Appearance")]
    public float lineThickness = 0.05f;
    public Color lineColor     = Color.black;
    public int   sortingOrder  = 5;
    [Tooltip("Leave empty to use the default sorting layer.")]
    public string sortingLayerName = "";

    // 0=Top  1=Bottom  2=Left  3=Right  4=Div1  5=Div2  6=Div3
    SpriteRenderer[] lines;

    void Start()
    {
        if (receptorController == null)
            receptorController = FindFirstObjectByType<HitReceptorController>();
        if (laneLayout == null)
            laneLayout = FindFirstObjectByType<LaneLayout>();
        Build();
    }

    void LateUpdate()   => UpdateLayout();

    void OnValidate()
    {
        if (lines == null) return;
        ApplyAppearance();
        UpdateLayout();
    }

    void Build()
    {
        string[] names = { "Top", "Bottom", "Left", "Right", "Divider1", "Divider2", "Divider3" };
        lines = new SpriteRenderer[names.Length];

        for (int i = 0; i < names.Length; i++)
        {
            var go    = new GameObject(names[i]);
            go.transform.SetParent(transform, worldPositionStays: false);

            var sr    = go.AddComponent<SpriteRenderer>();
            var tex   = Texture2D.whiteTexture;
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                                      new Vector2(0.5f, 0.5f), 1f);
            lines[i] = sr;
        }

        ApplyAppearance();
        UpdateLayout();
    }

    void ApplyAppearance()
    {
        foreach (var sr in lines)
        {
            if (sr == null) continue;
            sr.color = lineColor;
            sr.sortingOrder = sortingOrder;
            if (!string.IsNullOrEmpty(sortingLayerName))
                sr.sortingLayerName = sortingLayerName;
        }
    }

    void UpdateLayout()
    {
        if (lines == null) return;

        float w, h;

        if (receptorController != null && laneLayout != null
            && laneLayout.lanes != null && laneLayout.lanes.Length > 0)
        {
            int   n     = laneLayout.lanes.Length;
            float leftX  = laneLayout.lanes[0].position.x;
            float rightX = laneLayout.lanes[n - 1].position.x;
            float tileW  = receptorController.FillWidth;
            h = receptorController.FillHeight;
            // Span from left edge of lane 0 to right edge of lane n-1.
            w = (rightX - leftX) + tileW;

            // Centre the outline over the receptor group and sit it on the hitline.
            float cx = (leftX + rightX) * 0.5f;
            float cy = receptorController.hitLine != null ? receptorController.hitLine.position.y : transform.position.y;
            transform.position = new Vector3(cx, cy, transform.position.z);

            // Dividers between every pair of adjacent lanes (local X relative to centre).
            float laneStep = (n > 1) ? (rightX - leftX) / (n - 1) : tileW;
            float divStart = leftX + laneStep * 0.5f - cx;
            float innerH2  = Mathf.Max(0f, h - 2f * lineThickness);
            for (int i = 0; i < 3 && (i + 4) < lines.Length; i++)
                SetLine(lines[4 + i], lineThickness, innerH2, divStart + laneStep * i, 0f);
        }
        else
        {
            // Fallback: keep whatever the transform already has; no dividers.
            w = 1f;
            h = 1f;
        }

        float t      = lineThickness;
        float innerH = Mathf.Max(0f, h - 2f * t);

        SetLine(lines[0],  w,       t,  0f,  h * 0.5f - t * 0.5f);   // top
        SetLine(lines[1],  w,       t,  0f, -(h * 0.5f - t * 0.5f)); // bottom
        SetLine(lines[2],  t, innerH, -(w * 0.5f - t * 0.5f), 0f);   // left
        SetLine(lines[3],  t, innerH,  (w * 0.5f - t * 0.5f), 0f);   // right
    }

    static void SetLine(SpriteRenderer sr, float w, float h, float x, float y)
    {
        if (sr == null) return;
        sr.transform.localPosition = new Vector3(x, y, 0f);
        sr.transform.localScale    = new Vector3(w, h, 1f);
    }
}
