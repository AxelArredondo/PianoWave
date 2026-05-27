using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    [Header("Scene")]
    public string gameSceneName = "PianoWave_Main";

    [Header("Panels")]
    [Tooltip("Drag MainMenuPanel here.")]
    public GameObject mainMenuPanel;
    [Tooltip("Drag LevelSelectPanel here.")]
    public GameObject levelSelectPanel;

    [Header("Main Menu Buttons")]
    [Tooltip("Opens the Level Select panel.")]
    public Button levelsButton;
    [Tooltip("Starts Endless/Random mode.")]
    public Button endlessButton;

    [Header("Level Select Buttons")]
    [Tooltip("Unlocked — starts Level 1.")]
    public Button level1Button;
    [Tooltip("Locked — disabled until unlocked.")]
    public Button level2Button;
    [Tooltip("Locked — disabled until unlocked.")]
    public Button level3Button;
    [Tooltip("Locked — disabled until unlocked.")]
    public Button level4Button;
    [Tooltip("Returns to Main Menu.")]
    public Button backButton;

    void Start()
    {
        if (GameSettings.Instance == null)
        {
            var go = new GameObject("GameSettings");
            go.AddComponent<GameSettings>();
        }

        AutoWireByName();
        WireListeners();
        LockFutureLevels();
        ShowPanel(mainMenuPanel);
        HidePanel(levelSelectPanel);
        LogWarnings();
    }

    // ── Panel Navigation ────────────────────────────────────────────────────────

    public void OpenLevelSelect()
    {
        HidePanel(mainMenuPanel);
        ShowPanel(levelSelectPanel);
    }

    public void BackToMainMenu()
    {
        HidePanel(levelSelectPanel);
        ShowPanel(mainMenuPanel);
    }

    // ── Game Launch ─────────────────────────────────────────────────────────────

    public void PlayLevel(string chartResourcePath)
    {
        GameSettings.Instance.Mode = GameMode.LevelMode;
        GameSettings.Instance.ChartResourcePath = chartResourcePath;
        SceneManager.LoadScene(gameSceneName);
    }

    public void PlayRandomMode()
    {
        GameSettings.Instance.Mode = GameMode.RandomMode;
        SceneManager.LoadScene(gameSceneName);
    }

    // ── Locking ─────────────────────────────────────────────────────────────────

    // Call this to unlock a level at runtime (e.g. from a save system).
    public void UnlockLevel(Button btn)
    {
        if (btn == null) return;
        btn.interactable = true;
        var cg = btn.GetComponent<CanvasGroup>();
        if (cg != null) cg.alpha = 1f;
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────

    void WireListeners()
    {
        levelsButton?.onClick.AddListener(OpenLevelSelect);
        endlessButton?.onClick.AddListener(PlayRandomMode);

        level1Button?.onClick.AddListener(() => PlayLevel("Charts/Level1"));
        level2Button?.onClick.AddListener(() => PlayLevel("Charts/Level2"));
        level3Button?.onClick.AddListener(() => PlayLevel("Charts/Level3"));
        level4Button?.onClick.AddListener(() => PlayLevel("Charts/Level4"));

        backButton?.onClick.AddListener(BackToMainMenu);
    }

    void LockFutureLevels()
    {
        SetLocked(level2Button, true);
        SetLocked(level3Button, true);
        SetLocked(level4Button, true);
    }

    void SetLocked(Button btn, bool locked)
    {
        if (btn == null) return;
        btn.interactable = !locked;
        var cg = btn.GetComponent<CanvasGroup>();
        if (cg == null) cg = btn.gameObject.AddComponent<CanvasGroup>();
        cg.alpha = locked ? 0.35f : 1f;
    }

    void ShowPanel(GameObject panel) { if (panel != null) panel.SetActive(true); }
    void HidePanel(GameObject panel) { if (panel != null) panel.SetActive(false); }

    void AutoWireByName()
    {
        if (levelsButton  == null) levelsButton  = FindButtonByName("LevelsButton");
        if (endlessButton == null) endlessButton = FindButtonByName("EndlessButton") ?? FindButtonByName("RandomButton");
        if (level1Button  == null) level1Button  = FindButtonByName("Level1Button");
        if (level2Button  == null) level2Button  = FindButtonByName("Level2Button");
        if (level3Button  == null) level3Button  = FindButtonByName("Level3Button");
        if (level4Button  == null) level4Button  = FindButtonByName("Level4Button");
        if (backButton    == null) backButton    = FindButtonByName("BackButton");
    }

    void LogWarnings()
    {
        if (mainMenuPanel    == null) Debug.LogWarning("[MainMenuManager] mainMenuPanel not assigned.");
        if (levelSelectPanel == null) Debug.LogWarning("[MainMenuManager] levelSelectPanel not assigned.");
        if (levelsButton     == null) Debug.LogWarning("[MainMenuManager] LevelsButton not found.");
        if (endlessButton    == null) Debug.LogWarning("[MainMenuManager] EndlessButton not found.");
        if (level1Button     == null) Debug.LogWarning("[MainMenuManager] Level1Button not found.");
        if (backButton       == null) Debug.LogWarning("[MainMenuManager] BackButton not found.");
    }

    Button FindButtonByName(string n)
    {
        foreach (var b in FindObjectsByType<Button>(FindObjectsSortMode.None))
            if (b.name == n) return b;
        return null;
    }
}
