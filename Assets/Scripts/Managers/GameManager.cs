using UnityEngine;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public bool IsGameOver { get; private set; }
    public bool IsPaused { get; private set; }

    [Header("Game Stats")]
    public int score = 0;
    public int attempts = 5;
    public float timeAlive = 0f;

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

        IsGameOver = false;
        IsPaused = false;
        Time.timeScale = 1f;
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            TogglePause();
        }

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

    void UpdateMultiplier()
    {
        multiplier = 1 + (combo / comboStep);
        uiManager.UpdateCombo(combo, multiplier);
    }

    public void MissTile()
    {
        if (IsGameOver) return;

        attempts--;
        uiManager.UpdateAttempts(attempts);

        SpawnPopup("MISS", missMaterial, 34f);

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

        AudioManager.Instance?.StopAndReset();     // stop music
        AudioManager.Instance?.PlayGameOverSFX();  // play lose sound

        BeatManager.Instance?.ResetBeatTimer();
        uiManager.ShowGameOver(score, timeAlive);
    }

    public void TogglePause()
    {
        if (IsGameOver) return;

        IsPaused = !IsPaused;

        if (IsPaused)
            PauseGame();
        else
            ResumeGame();
    }

    void PauseGame()
    {
        Time.timeScale = 0f;

        // ✅ Stop and reset music on pause
        AudioManager.Instance?.StopAndReset();

        uiManager.ShowPauseMenu(true);
    }

    void ResumeGame()
    {
        Time.timeScale = 1f;

        // ✅ Restart music from beginning on resume
        AudioManager.Instance?.PlayFromStart();

        uiManager.ShowPauseMenu(false);
    }
}