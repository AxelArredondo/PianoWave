using UnityEngine;

// Draws a rectangle outline + 3 vertical dividers over the hit receptors.
// Add this to any empty GameObject, position it over the receptors, and adjust
// Width / Height in the Inspector. All lines update live while the game runs.
//
// Setup:
//   1. Create empty GameObject in PianoWave_Main, name it "ReceptorGrid"
//   2. Add this component
//   3. Set Sorting Order higher than both the fill and border of HitReceptorController
//      so the grid draws on top (e.g. sortingOrder = 5 if receptors use -1 / 0)
public class ReceptorGridOverlay : MonoBehaviour
{
    [Header("Grid Size")]
    public float width         = 4f;
    public float height        = 1f;

    [Header("Appearance")]
    public float lineThickness = 0.05f;
    public Color lineColor     = Color.black;
    public int   sortingOrder  = 5;
    [Tooltip("Leave empty to use the default sorting layer.")]
    public string sortingLayerName = "";

    // 0=Top  1=Bottom  2=Left  3=Right  4=Div1  5=Div2  6=Div3
    SpriteRenderer[] lines;

    void Start()        => Build();
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

        float t      = lineThickness;
        float w      = width;
        float h      = height;
        // Inner content height — the vertical space between the two horizontal borders.
        float innerH = Mathf.Max(0f, h - 2f * t);

        // Outer rectangle drawn INWARD so no line ever goes past the w×h boundary.
        // Top/bottom span the full width so they fill the corners; left/right fit between them.
        SetLine(lines[0],  w,       t,  0f,  h * 0.5f - t * 0.5f);   // top
        SetLine(lines[1],  w,       t,  0f, -(h * 0.5f - t * 0.5f)); // bottom
        SetLine(lines[2],  t, innerH, -(w * 0.5f - t * 0.5f), 0f);   // left
        SetLine(lines[3],  t, innerH,  (w * 0.5f - t * 0.5f), 0f);   // right

        // 3 vertical dividers — same innerH so they end exactly at the border edges.
        SetLine(lines[4],  t, innerH,  -w / 4f, 0f);
        SetLine(lines[5],  t, innerH,   0f,      0f);
        SetLine(lines[6],  t, innerH,   w / 4f, 0f);
    }

    static void SetLine(SpriteRenderer sr, float w, float h, float x, float y)
    {
        if (sr == null) return;
        sr.transform.localPosition = new Vector3(x, y, 0f);
        sr.transform.localScale    = new Vector3(w, h, 1f);
    }
}
