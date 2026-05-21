using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Attach to a GameObject in the MainMenu scene.
// Buttons named "Level1Button" and "RandomButton" are auto-wired by name,
// or you can assign them manually in the Inspector.
public class MainMenuManager : MonoBehaviour
{
    [Header("Scene to load")]
    public string gameSceneName = "PianoWave_Main";

    [Header("Buttons (auto-found by name if left empty)")]
    public Button level1Button;
    public Button randomButton;

    void Start()
    {
        // Ensure GameSettings singleton exists (created here if not already present).
        if (GameSettings.Instance == null)
        {
            GameObject go = new GameObject("GameSettings");
            go.AddComponent<GameSettings>();
        }

        // Auto-wire buttons if not assigned in Inspector.
        if (level1Button == null)
            level1Button = FindButtonByName("Level1Button");
        if (randomButton == null)
            randomButton = FindButtonByName("RandomButton");

        level1Button?.onClick.AddListener(PlayLevel1);
        randomButton?.onClick.AddListener(PlayRandomMode);
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

    Button FindButtonByName(string buttonName)
    {
        Button[] all = FindObjectsByType<Button>(FindObjectsSortMode.None);
        foreach (var b in all)
            if (b.name == buttonName) return b;
        return null;
    }
}
