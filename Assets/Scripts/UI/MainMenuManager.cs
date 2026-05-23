using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    [Header("Scene")]
    public string gameSceneName = "PianoWave_Main";

    [Header("Buttons — drag from the hierarchy")]
    [Tooltip("Drag LevelsButton here. Loads LevelMode (Charts/Level1).")]
    public Button levelsButton;

    [Tooltip("Drag EndlessButton here. Loads RandomMode (endless/random).")]
    public Button endlessButton;

    void Start()
    {
        if (GameSettings.Instance == null)
        {
            var go = new GameObject("GameSettings");
            go.AddComponent<GameSettings>();
        }

        // Fallback auto-wire by name (supports old names too)
        if (levelsButton == null)
            levelsButton = FindButtonByName("LevelsButton") ?? FindButtonByName("Level1Button");
        if (endlessButton == null)
            endlessButton = FindButtonByName("EndlessButton") ?? FindButtonByName("RandomButton");

        levelsButton?.onClick.AddListener(PlayLevel1);
        endlessButton?.onClick.AddListener(PlayRandomMode);

        if (levelsButton == null)
            Debug.LogWarning("[MainMenuManager] LevelsButton not found — assign it in the Inspector.");
        if (endlessButton == null)
            Debug.LogWarning("[MainMenuManager] EndlessButton not found — assign it in the Inspector.");
    }

    public void PlayLevel1()
    {
        GameSettings.Instance.Mode = GameMode.LevelMode;
        GameSettings.Instance.ChartResourcePath = "Charts/Level1";
        SceneManager.LoadScene(gameSceneName);
    }

    public void PlayRandomMode()
    {
        GameSettings.Instance.Mode = GameMode.RandomMode;
        SceneManager.LoadScene(gameSceneName);
    }

    Button FindButtonByName(string n)
    {
        foreach (var b in FindObjectsByType<Button>(FindObjectsSortMode.None))
            if (b.name == n) return b;
        return null;
    }
}
