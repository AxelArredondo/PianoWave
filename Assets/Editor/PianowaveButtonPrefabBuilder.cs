#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public static class PianowaveButtonPrefabBuilder
{
    const string TexturePath      = "Assets/Art/Pianowave_buttons_images.png";
    const string OutputPath       = "Assets/Prefabs/UI/PianowaveButton.prefab";
    const string BackOutputPath   = "Assets/Prefabs/UI/PianowaveButtonBack.prefab";

    [MenuItem("PianoWave/Build Button Prefab")]
    static void Build() => BuildPrefab(withDeco: true,  outputPath: OutputPath,     rootName: "PianowaveButton",     fontSize: 58f, preferredHeight: 145f);

    [MenuItem("PianoWave/Build Back Button Prefab")]
    static void BuildBack() => BuildPrefab(withDeco: false, outputPath: BackOutputPath, rootName: "PianowaveButtonBack", fontSize: 42f, preferredHeight: 100f);

    static void BuildPrefab(bool withDeco, string outputPath, string rootName, float fontSize, float preferredHeight)
    {
        var allAssets = AssetDatabase.LoadAllAssetsAtPath(TexturePath);

        Sprite normalSprite   = null;
        Sprite selectedSprite = null;
        Sprite pressedSprite  = null;
        Sprite decoSprite     = null;

        foreach (var obj in allAssets)
        {
            if (obj is not Sprite s) continue;
            if      (s.name.EndsWith("_0")) normalSprite   = s;
            else if (s.name.EndsWith("_1")) selectedSprite = s;
            else if (s.name.EndsWith("_2")) pressedSprite  = s;
            else if (s.name.EndsWith("_3")) decoSprite     = s;
        }

        if (normalSprite == null)
        {
            Debug.LogError("[PianoWave] Could not find button sprites in " + TexturePath);
            return;
        }

        // ── Root: Button ──────────────────────────────────────────────────────────
        var rootGO = new GameObject(rootName, typeof(RectTransform));
        var rootRT = rootGO.GetComponent<RectTransform>();
        rootRT.sizeDelta = new Vector2(1250f, 200f);

        var bgImg    = rootGO.AddComponent<Image>();
        bgImg.sprite = normalSprite;
        bgImg.color  = Color.white;
        bgImg.type   = Image.Type.Simple;

        var btn = rootGO.AddComponent<Button>();
        btn.targetGraphic = bgImg;
        btn.transition    = Selectable.Transition.SpriteSwap;
        btn.spriteState   = new SpriteState
        {
            highlightedSprite = selectedSprite,
            pressedSprite     = pressedSprite,
            selectedSprite    = selectedSprite,
        };

        var le = rootGO.AddComponent<LayoutElement>();
        le.preferredHeight = preferredHeight;
        le.flexibleWidth   = 1f;

        // ── Side decoration (left edge, main button only) ─────────────────────────
        bool hasDeco = withDeco && decoSprite != null;
        if (hasDeco)
        {
            var decoGO  = Child("SideDecoration", rootGO);
            var decoImg = decoGO.AddComponent<Image>();
            decoImg.sprite         = decoSprite;
            decoImg.preserveAspect = true;
            decoImg.raycastTarget  = false;

            var decoRT              = decoGO.GetComponent<RectTransform>();
            decoRT.anchorMin        = new Vector2(0f, 0f);
            decoRT.anchorMax        = new Vector2(0f, 1f);
            decoRT.pivot            = new Vector2(0f, 0.5f);
            decoRT.sizeDelta        = new Vector2(120f, 0f);
            decoRT.anchoredPosition = new Vector2(8f, 0f);
        }

        // ── Label ─────────────────────────────────────────────────────────────────
        var labelGO      = Child("Label", rootGO);
        var tmp          = labelGO.AddComponent<TextMeshProUGUI>();
        tmp.text         = "BUTTON";
        tmp.fontSize     = fontSize;
        tmp.fontStyle    = FontStyles.Bold;
        tmp.alignment    = TextAlignmentOptions.Center;
        tmp.color        = Color.white;

        var labelRT       = labelGO.GetComponent<RectTransform>();
        labelRT.anchorMin = Vector2.zero;
        labelRT.anchorMax = Vector2.one;
        labelRT.offsetMin = new Vector2(hasDeco ? 136f : 0f, 0f);
        labelRT.offsetMax = Vector2.zero;

        // ── Save prefab ───────────────────────────────────────────────────────────
        PrefabUtility.SaveAsPrefabAsset(rootGO, outputPath);
        Object.DestroyImmediate(rootGO);
        AssetDatabase.Refresh();

        Debug.Log("[PianoWave] Prefab saved to " + outputPath);
    }

    static GameObject Child(string name, GameObject parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent.transform, false);
        return go;
    }
}
#endif
