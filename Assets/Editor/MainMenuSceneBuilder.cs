#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using TMPro;

public static class MainMenuSceneBuilder
{
    const string ScenePath = "Assets/Scenes/MainMenu.unity";

    // ── Option A: fresh scene ────────────────────────────────────────────────────
    [MenuItem("PianoWave/Build Main Menu Scene (Synthwave)")]
    static void Build()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        AddCamera();
        PopulateUI();
        EditorSceneManager.SaveScene(scene, ScenePath);
        AddToBuildSettings(ScenePath);
        Debug.Log("[PianoWave] Synthwave MainMenu scene saved to " + ScenePath);
        AssetDatabase.Refresh();
    }

    // ── Option B: fill the currently open scene (replaces old UI) ───────────────
    [MenuItem("PianoWave/Populate Open Scene as Main Menu (Synthwave)")]
    static void Populate()
    {
        CleanupOldUI();
        PopulateUI();
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        EditorSceneManager.SaveScene(scene);
        AddToBuildSettings(scene.path);
        Debug.Log("[PianoWave] Synthwave UI populated in '" + scene.name + "' and scene saved.");
    }

    // ── Cleanup ──────────────────────────────────────────────────────────────────

    static void CleanupOldUI()
    {
        foreach (var c in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            Object.DestroyImmediate(c.gameObject);
        foreach (var m in Object.FindObjectsByType<MainMenuManager>(FindObjectsSortMode.None))
            Object.DestroyImmediate(m.gameObject);
        foreach (var a in Object.FindObjectsByType<AudioManager>(FindObjectsSortMode.None))
            Object.DestroyImmediate(a.gameObject);
        Debug.Log("[PianoWave] Old Canvas/MainMenuManager/AudioManager objects removed.");
    }

    // ── Camera ───────────────────────────────────────────────────────────────────

    static void AddCamera()
    {
        var camGO = new GameObject("Main Camera");
        var cam   = camGO.AddComponent<Camera>();
        cam.clearFlags      = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.04f, 0.02f, 0.12f, 1f);
        cam.orthographic    = true;
        cam.tag             = "MainCamera";
    }

    // ── UI Builder ───────────────────────────────────────────────────────────────

    static void PopulateUI()
    {
        // ── Canvas ───────────────────────────────────────────────────────────────
        var canvasGO = new GameObject("Canvas", typeof(RectTransform));
        var canvas   = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight  = 0.5f;
        scaler.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;

        canvasGO.AddComponent<GraphicRaycaster>();

        // ── SafeAreaPanel ─────────────────────────────────────────────────────────
        // SafeAreaFitter adjusts this at runtime to respect phone notch/home bar.
        var safeGO = Child("SafeAreaPanel", canvasGO);
        safeGO.AddComponent<SafeAreaFitter>();
        Stretch(safeGO);

        // ── BackgroundLayer ───────────────────────────────────────────────────────
        var bgLayerGO = Child("BackgroundLayer", safeGO);
        Stretch(bgLayerGO);

        // MainBackgroundImage: full screen, EnvelopeParent so it covers without letterbox.
        var bgImgGO  = Child("MainBackgroundImage", bgLayerGO);
        var bgImg    = bgImgGO.AddComponent<Image>();
        bgImg.color  = new Color(0.04f, 0.02f, 0.12f, 1f); // fallback until sprite assigned
        var bgARF    = bgImgGO.AddComponent<AspectRatioFitter>();
        bgARF.aspectMode  = AspectRatioFitter.AspectMode.EnvelopeParent;
        bgARF.aspectRatio = 9f / 16f; // portrait default — Unity updates this when a sprite is set
        Stretch(bgImgGO);

        // ForegroundFXImage: optional particle/star/scanline overlay above the BG.
        var fxImgGO   = Child("ForegroundFXImage", bgLayerGO);
        var fxImg     = fxImgGO.AddComponent<Image>();
        fxImg.color   = new Color(1f, 1f, 1f, 0f); // fully transparent until sprite is assigned
        Stretch(fxImgGO);

        // ── TVLayer ───────────────────────────────────────────────────────────────
        // Centered on screen. Roughly 67% of reference width (1080).
        // RESIZE THIS in the Inspector to match your CRT frame image's aspect ratio.
        var tvGO = Child("TVLayer", safeGO);
        var tvRT = tvGO.GetComponent<RectTransform>();
        tvRT.anchorMin        = new Vector2(0.5f, 0.5f);
        tvRT.anchorMax        = new Vector2(0.5f, 0.5f);
        tvRT.pivot            = new Vector2(0.5f, 0.5f);
        tvRT.sizeDelta        = new Vector2(720f, 960f); // ~67% wide — adjust height to fit your frame
        tvRT.anchoredPosition = Vector2.zero;

        // CRTFrameImage: the outer TV border, stretched to fill TVLayer.
        var crtFrameGO  = Child("CRTFrameImage", tvGO);
        var crtFrame    = crtFrameGO.AddComponent<Image>();
        crtFrame.color  = Color.white;
        Stretch(crtFrameGO);

        // ── TVScreenContentPanel ──────────────────────────────────────────────────
        // This panel represents the screen area INSIDE the TV frame.
        // Adjust these anchors so the content sits within the visible screen of your frame image.
        var screenGO = Child("TVScreenContentPanel", tvGO);
        var screenRT = screenGO.GetComponent<RectTransform>();
        screenRT.anchorMin = new Vector2(0.10f, 0.14f);
        screenRT.anchorMax = new Vector2(0.90f, 0.84f);
        screenRT.offsetMin = Vector2.zero;
        screenRT.offsetMax = Vector2.zero;

        // PianoWaveLogoImage: top ~38% of the screen content panel.
        var logoGO           = Child("PianoWaveLogoImage", screenGO);
        var logoImg          = logoGO.AddComponent<Image>();
        logoImg.color        = Color.white;
        logoImg.preserveAspect = true;
        var logoRT           = logoGO.GetComponent<RectTransform>();
        logoRT.anchorMin     = new Vector2(0.00f, 0.62f);
        logoRT.anchorMax     = new Vector2(1.00f, 1.00f);
        logoRT.offsetMin     = Vector2.zero;
        logoRT.offsetMax     = Vector2.zero;

        // ButtonContainer: holds the two buttons in a vertical layout.
        var btnContGO = Child("ButtonContainer", screenGO);
        var btnContRT = btnContGO.GetComponent<RectTransform>();
        btnContRT.anchorMin = new Vector2(0.04f, 0.02f);
        btnContRT.anchorMax = new Vector2(0.96f, 0.57f);
        btnContRT.offsetMin = Vector2.zero;
        btnContRT.offsetMax = Vector2.zero;

        var vlg = btnContGO.AddComponent<VerticalLayoutGroup>();
        vlg.spacing               = 36f;
        vlg.childAlignment        = TextAnchor.MiddleCenter;
        vlg.childControlWidth     = true;
        vlg.childControlHeight    = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.padding = new RectOffset(0, 0, 8, 8);

        // LevelsButton → loads LevelMode
        var levelsBtn = MakeButton(btnContGO, "LevelsButton", "LEVELS",
            normalCol:    new Color(0.06f, 0.06f, 0.22f, 0.90f),
            highlightCol: new Color(0.08f, 0.45f, 0.95f, 1.00f),
            pressedCol:   new Color(0.04f, 0.24f, 0.62f, 1.00f));

        // EndlessButton → loads RandomMode (visually renamed from RandomButton)
        var endlessBtn = MakeButton(btnContGO, "EndlessButton", "ENDLESS",
            normalCol:    new Color(0.17f, 0.05f, 0.22f, 0.90f),
            highlightCol: new Color(0.88f, 0.10f, 0.66f, 1.00f),
            pressedCol:   new Color(0.56f, 0.04f, 0.42f, 1.00f));

        // CRTScreenOverlayImage: scanlines/glare on top. Raycast OFF so buttons still work.
        var crtOverGO         = Child("CRTScreenOverlayImage", tvGO);
        var crtOver           = crtOverGO.AddComponent<Image>();
        crtOver.color         = new Color(1f, 1f, 1f, 0.55f);
        crtOver.raycastTarget = false;
        Stretch(crtOverGO);

        // ── EventSystem ───────────────────────────────────────────────────────────
        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            var esGO = new GameObject("EventSystem");
            esGO.AddComponent<EventSystem>();
            esGO.AddComponent<InputSystemUIInputModule>();
        }

        // ── GameSettings ──────────────────────────────────────────────────────────
        if (Object.FindFirstObjectByType<GameSettings>() == null)
        {
            var gsGO = new GameObject("GameSettings");
            gsGO.AddComponent<GameSettings>();
        }

        // ── MainMenuManager ───────────────────────────────────────────────────────
        var managerGO = new GameObject("MainMenuManager");
        var manager   = managerGO.AddComponent<MainMenuManager>();
        manager.levelsButton  = levelsBtn;
        manager.endlessButton = endlessBtn;
        EditorUtility.SetDirty(managerGO);

        // ── AudioManager ──────────────────────────────────────────────────────────
        if (Object.FindFirstObjectByType<AudioManager>() == null)
        {
            var audioGO  = new GameObject("AudioManager");
            var audioMgr = audioGO.AddComponent<AudioManager>();
            var audioSrc = audioGO.AddComponent<AudioSource>();
            audioSrc.loop        = true;
            audioSrc.playOnAwake = false;
            audioMgr.musicSource = audioSrc;

            var mainMenuClip = AssetDatabase.LoadAssetAtPath<AudioClip>(
                "Assets/Audio/Music/Pianowave_Main_Menu_song.mp3");
            if (mainMenuClip != null)
                audioMgr.musicClip = mainMenuClip;
            else
                Debug.LogWarning("[PianoWave] Pianowave_Main_Menu_song.mp3 not found — assign AudioManager.musicClip manually.");

            EditorUtility.SetDirty(audioGO);
        }

        // ── MainMenuUISetup (on Canvas) ───────────────────────────────────────────
        // All image references are pre-wired. Drag your sprites into these fields.
        var setup = canvasGO.AddComponent<MainMenuUISetup>();
        setup.mainBackgroundImage   = bgImg;
        setup.foregroundFXImage     = fxImg;
        setup.crtFrameImage         = crtFrame;
        setup.crtScreenOverlayImage = crtOver;
        setup.logoImage             = logoImg;
        setup.levelsButton          = levelsBtn;
        setup.endlessButton         = endlessBtn;
        EditorUtility.SetDirty(canvasGO);

        Debug.Log(
            "[PianoWave] Hierarchy built. Drag your sprites here:\n\n" +
            "  Canvas > SafeAreaPanel > BackgroundLayer\n" +
            "    MainBackgroundImage.sprite  ← main synthwave background\n" +
            "    ForegroundFXImage.sprite    ← optional FX layer (leave empty to hide)\n\n" +
            "  Canvas > SafeAreaPanel > TVLayer\n" +
            "    CRTFrameImage.sprite        ← CRT TV frame\n" +
            "    TVScreenContentPanel > PianoWaveLogoImage.sprite ← your logo\n" +
            "    CRTScreenOverlayImage.sprite ← scanlines/glare (raycast is already OFF)\n\n" +
            "  Also drag each sprite into the matching field on MainMenuUISetup (on the Canvas).\n" +
            "  MainMenuManager.levelsButton and .endlessButton are already wired.\n\n" +
            "  After assigning sprites:\n" +
            "    • Resize TVLayer to match your CRT frame image's aspect ratio.\n" +
            "    • Adjust TVScreenContentPanel anchors so content fits inside the frame.\n" +
            "    • Play → Levels button → Console should print GameMode.LevelMode.\n" +
            "    • Play → Endless button → Console should print GameMode.RandomMode."
        );
    }

    // ── Helpers ──────────────────────────────────────────────────────────────────

    static GameObject Child(string name, GameObject parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent.transform, false);
        return go;
    }

    static void Stretch(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static Button MakeButton(GameObject container, string objName, string label,
        Color normalCol, Color highlightCol, Color pressedCol)
    {
        var btnGO = new GameObject(objName, typeof(RectTransform));
        btnGO.transform.SetParent(container.transform, false);

        var img  = btnGO.AddComponent<Image>();
        img.color = Color.white; // ColorBlock drives the actual tint

        var btn = btnGO.AddComponent<Button>();
        btn.colors = new ColorBlock
        {
            normalColor      = normalCol,
            highlightedColor = highlightCol,
            pressedColor     = pressedCol,
            selectedColor    = highlightCol,
            disabledColor    = new Color(0.30f, 0.30f, 0.30f, 0.50f),
            colorMultiplier  = 1f,
            fadeDuration     = 0.12f,
        };

        var le = btnGO.AddComponent<LayoutElement>();
        le.preferredHeight = 145f;
        le.flexibleWidth   = 1f;

        var labelGO = new GameObject("Label", typeof(RectTransform));
        labelGO.transform.SetParent(btnGO.transform, false);

        var tmp       = labelGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = 58f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = Color.white;

        var labelRT      = labelGO.GetComponent<RectTransform>();
        labelRT.anchorMin = Vector2.zero;
        labelRT.anchorMax = Vector2.one;
        labelRT.offsetMin = Vector2.zero;
        labelRT.offsetMax = Vector2.zero;

        return btn;
    }

    static void AddToBuildSettings(string path)
    {
        if (string.IsNullOrEmpty(path)) return;
        var existing = EditorBuildSettings.scenes;
        foreach (var s in existing)
            if (s.path == path) return;

        var updated = new EditorBuildSettingsScene[existing.Length + 1];
        updated[0] = new EditorBuildSettingsScene(path, true);
        existing.CopyTo(updated, 1);
        EditorBuildSettings.scenes = updated;
    }
}
#endif
