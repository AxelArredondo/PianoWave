using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public bool IsGameOver     { get; private set; }
    public bool IsPaused       { get; private set; }
    public bool IsLevelComplete { get; private set; }

    [Header("Game Stats")]
    public int score = 0;
    public int attempts = 5;
    public float timeAlive = 0f;

    [HideInInspector]
    public bool unlimitedAttempts = false;

    [Header("UI")]
    public UIManager uiManager;

    [Header("Combo")]
    public int combo = 0;
    public int multiplier = 1;

    [SerializeField]
    private int comboStep = 5; // every 5 hits, multiplier increases

    [Header("Hit Popups")]
    public HitPopup hitPopupPrefab;
    public Transform hitLine;

    [Header("Hit Popup Materials")]
    public Material perfectMaterial;
    public Material goodMaterial;
    public Material missMaterial;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        IsGameOver      = false;
        IsPaused        = false;
        IsLevelComplete = false;
        Time.timeScale  = 1f;
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            TogglePause();
        }

#if UNITY_EDITOR
        if (Keyboard.current != null && Keyboard.current.uKey.wasPressedThisFrame)
            unlimitedAttempts = !unlimitedAttempts;
#endif

        if (IsGameOver || IsPaused) return;

        timeAlive += Time.deltaTime;
    }

    public void AddScore(int amount)
    {
        if (IsGameOver) return;
        score += amount;
        uiManager.UpdateScore(score);
    }

    public void RegisterPerfect()
    {
        if (IsGameOver) return;

        combo++;
        UpdateMultiplier();
        AddScore(300 * multiplier);

        SpawnPopup("PERFECT", perfectMaterial, 40f);
    }

    public void RegisterGood()
    {
        if (IsGameOver) return;

        combo++;
        UpdateMultiplier();
        AddScore(100 * multiplier);

        SpawnPopup("GOOD", goodMaterial, 32f);
    }

    public void RegisterMiss()
    {
        if (IsGameOver) return;

        combo = 0;
        multiplier = 1;
        uiManager.UpdateCombo(combo, multiplier);
    }

    // Called when a hold note is completed. Does not affect combo or multiplier —
    // that was already handled by the head hit (Perfect/Good).
    public void RegisterHoldBonus(int bonus)
    {
        if (IsGameOver) return;
        AddScore(bonus);
        SpawnPopup("HOLD!", perfectMaterial, 34f);
    }

    void UpdateMultiplier()
    {
        multiplier = 1 + (combo / comboStep);
        uiManager.UpdateCombo(combo, multiplier);
    }

    public void MissTile()
    {
        if (IsGameOver) return;

        if (TapDebugMode.Instance != null && TapDebugMode.Instance.debugMode)
        {
            TapDebugMode.Instance.TriggerDebugPause();
            return;
        }

        SpawnPopup("MISS", missMaterial, 34f);

        if (unlimitedAttempts) return;

        attempts--;
        uiManager.UpdateAttempts(attempts);

        if (attempts <= 0)
            EndGame();
    }

    void SpawnPopup(string text, Material material, float size)
    {
        if (IsGameOver) return;
        if (material == null) return;
        if (hitPopupPrefab == null) return;
        if (uiManager == null) return;

        HitPopup popup = Instantiate(hitPopupPrefab, uiManager.transform);
        popup.transform.SetAsLastSibling();

        RectTransform popupRect = popup.GetComponent<RectTransform>();
        if (popupRect == null) return;

        Vector2 screenPos = new Vector2(Screen.width * 0.5f, Screen.height * 0.65f);
        popupRect.position = screenPos;

        popup.Setup(text, material, size);
    }

    void EndGame()
    {
        IsGameOver = true;

        if (GameSettings.Instance?.Mode == GameMode.RandomMode)
        {
            int prevBest = PlayerPrefs.GetInt("EndlessBestScore", 0);
            if (score > prevBest)
            {
                PlayerPrefs.SetInt("EndlessBestScore", score);
                PlayerPrefs.Save();
            }
        }

        AudioManager.Instance?.StopAndReset();
        AudioManager.Instance?.PlayGameOverSFX();
        AudioManager.Instance?.PlayGameOverLoop();

        BeatManager.Instance?.ResetBeatTimer();
        uiManager.ShowGameOver(score, timeAlive);
    }

    public void TriggerLevelComplete()
    {
        if (IsGameOver || IsLevelComplete) return;

        IsLevelComplete = true;

        string chartPath = GameSettings.Instance?.ChartResourcePath ?? "Charts/Level1";
        string key = "HighScore_" + chartPath.Replace("Charts/", "");
        int prevHS   = PlayerPrefs.GetInt(key, 0);
        bool isNewHS = score > prevHS;
        if (isNewHS)
        {
            PlayerPrefs.SetInt(key, score);
            PlayerPrefs.Save();
        }

        Time.timeScale = 0f;
        AudioManager.Instance?.Pause();
        uiManager.ShowLevelComplete(score, isNewHS ? score : prevHS, isNewHS);
    }

    public void TogglePause()
    {
        if (IsGameOver || IsLevelComplete) return;

        IsPaused = !IsPaused;

        if (IsPaused)
            PauseGame();
        else
            ResumeGame();
    }

    void PauseGame()
    {
        Time.timeScale = 0f;
        AudioManager.Instance?.Pause();
        uiManager.ShowPauseMenu(true);
    }

    void ResumeGame()
    {
        Time.timeScale = 1f;
        AudioManager.Instance?.Resume();
        uiManager.ShowPauseMenu(false);
    }
}