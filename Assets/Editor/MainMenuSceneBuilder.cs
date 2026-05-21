#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using TMPro;

// PianoWave → Build Main Menu Scene   : creates a brand-new MainMenu.unity
// PianoWave → Populate Open Scene as Main Menu : fills the CURRENTLY OPEN scene with
//             the same UI (use this if you already made the scene yourself)
public static class MainMenuSceneBuilder
{
    const string ScenePath = "Assets/Scenes/MainMenu.unity";

    // ── option A: create a fresh scene ────────────────────────────────────────
    [MenuItem("PianoWave/Build Main Menu Scene")]
    static void Build()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        AddCamera();
        PopulateUI();
        EditorSceneManager.SaveScene(scene, ScenePath);
        AddToBuildSettings(ScenePath);
        Debug.Log($"[PianoWave] MainMenu scene saved to {ScenePath} and added to Build Settings.");
        AssetDatabase.Refresh();
    }

    // ── option B: populate whatever scene is already open ─────────────────────
    [MenuItem("PianoWave/Populate Open Scene as Main Menu")]
    static void Populate()
    {
        PopulateUI();

        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        EditorSceneManager.SaveScene(scene);
        AddToBuildSettings(scene.path);
        Debug.Log($"[PianoWave] Populated '{scene.name}' and added to Build Settings.");
    }

    // ── shared UI builder ─────────────────────────────────────────────────────

    static void AddCamera()
    {
        var camGO = new GameObject("Main Camera");
        var cam   = camGO.AddComponent<Camera>();
        cam.clearFlags       = CameraClearFlags.SolidColor;
        cam.backgroundColor  = new Color(0.05f, 0.05f, 0.18f, 1f);
        cam.orthographic     = true;
        cam.orthographicSize = 5f;
        cam.tag              = "MainCamera";
    }

    static void PopulateUI()
    {
        // ── Canvas ────────────────────────────────────────────────────────────
        var canvasGO = new GameObject("Canvas");
        var canvas   = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 0;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight  = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // ── Background panel (dark gradient feel) ─────────────────────────────
        var bgGO = new GameObject("Background");
        bgGO.transform.SetParent(canvasGO.transform, false);
        var bgImg  = bgGO.AddComponent<Image>();
        bgImg.color = new Color(0.04f, 0.04f, 0.14f, 1f);
        SetAnchors(bgGO, 0f, 0f, 1f, 1f);

        // ── Title ─────────────────────────────────────────────────────────────
        var titleGO   = new GameObject("Title");
        titleGO.transform.SetParent(canvasGO.transform, false);
        var titleText = titleGO.AddComponent<TextMeshProUGUI>();
        titleText.text      = "PIANOWAVE";
        titleText.fontSize  = 96;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color     = new Color(0.21f, 0.88f, 1f);
        SetAnchors(titleGO, 0.05f, 0.68f, 0.95f, 0.92f);

        // ── Subtitle ──────────────────────────────────────────────────────────
        var subGO   = new GameObject("Subtitle");
        subGO.transform.SetParent(canvasGO.transform, false);
        var subText = subGO.AddComponent<TextMeshProUGUI>();
        subText.text      = "Select a mode to play";
        subText.fontSize  = 38;
        subText.alignment = TextAlignmentOptions.Center;
        subText.color     = new Color(0.75f, 0.80f, 1f);
        SetAnchors(subGO, 0.05f, 0.58f, 0.95f, 0.68f);

        // ── Level 1 button ────────────────────────────────────────────────────
        CreateButton(canvasGO, "Level1Button", "LEVEL  1",
            new Color(0.10f, 0.50f, 1f), 0.15f, 0.40f, 0.85f, 0.54f);

        // ── Random Mode button ────────────────────────────────────────────────
        CreateButton(canvasGO, "RandomButton", "RANDOM MODE",
            new Color(1f, 0.22f, 0.65f), 0.15f, 0.22f, 0.85f, 0.36f);

        // ── EventSystem ───────────────────────────────────────────────────────
        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            var esGO = new GameObject("EventSystem");
            esGO.AddComponent<EventSystem>();
            esGO.AddComponent<InputSystemUIInputModule>();
        }

        // ── GameSettings ──────────────────────────────────────────────────────
        var settingsGO = new GameObject("GameSettings");
        settingsGO.AddComponent<GameSettings>();

        // ── MainMenuManager ───────────────────────────────────────────────────
        var managerGO = new GameObject("MainMenuManager");
        managerGO.AddComponent<MainMenuManager>();
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    static void CreateButton(GameObject canvas, string name, string label,
        Color color, float xMin, float yMin, float xMax, float yMax)
    {
        var btnGO = new GameObject(name);
        btnGO.transform.SetParent(canvas.transform, false);

        var img = btnGO.AddComponent<Image>();
        img.color = color;

        btnGO.AddComponent<Button>();
        SetAnchors(btnGO, xMin, yMin, xMax, yMax);

        var textGO = new GameObject("Label");
        textGO.transform.SetParent(btnGO.transform, false);
        var text = textGO.AddComponent<TextMeshProUGUI>();
        text.text      = label;
        text.fontSize  = 52;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color     = Color.white;
        SetAnchors(textGO, 0f, 0f, 1f, 1f);
    }

    static void SetAnchors(GameObject go, float xMin, float yMin, float xMax, float yMax)
    {
        var rt = go.GetComponent<RectTransform>();
        if (rt == null) rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(xMin, yMin);
        rt.anchorMax = new Vector2(xMax, yMax);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
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
