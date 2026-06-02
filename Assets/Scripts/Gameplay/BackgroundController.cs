using UnityEngine;

// Receives theme change requests from ChartSpawner (backgroundEvents) and tints
// whatever SpriteRenderers are assigned in the Inspector.
//
// To add a theme: add a public Color field and a matching case in SetTheme().
// To target specific background layers, drag their SpriteRenderers into tintTargets[].
public class BackgroundController : MonoBehaviour
{
    public static BackgroundController Instance { get; private set; }

    [Header("Position")]
    [Tooltip("Shifts every background element up (positive) or down (negative) by this many world units.")]
    public float heightOffset = 40f;

    [Header("Sprites to tint on theme change")]
    [Tooltip("Drag background SpriteRenderers here. All will be tinted when SetTheme() is called.")]
    public SpriteRenderer[] tintTargets;

    [Header("Theme Colors")]
    public Color themePurple = new Color(0.44f, 0.18f, 0.82f);
    public Color themePink   = new Color(0.90f, 0.18f, 0.52f);
    public Color themeBlue   = new Color(0.10f, 0.38f, 0.95f);
    public Color themeDefault = Color.white;

    void Awake()
    {
        Instance = this;
    }

    public void SetTheme(string theme)
    {
        Color c;
        switch (theme.ToLower())
        {
            case "purple": c = themePurple; break;
            case "pink":   c = themePink;   break;
            case "blue":   c = themeBlue;   break;
            default:       c = themeDefault; break;
        }

        if (tintTargets == null) return;
        foreach (var sr in tintTargets)
        {
            if (sr != null) sr.color = c;
        }
    }
}
