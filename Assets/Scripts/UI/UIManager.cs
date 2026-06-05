using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("Gameplay UI")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI attemptsText;

    [Header("Game Over UI")]
    public GameObject gameOverPanel;
    public TextMeshProUGUI finalScoreText;
    public TextMeshProUGUI timeText;

    [Header("Combo UI")]
    public TextMeshProUGUI comboText;

    [Header("Pause UI")]
    public GameObject pausePanel;

    [Header("Pause/Retry Background")]
    public Image pauseBackground;

    [Header("Settings Button")]
    [Tooltip("Drag the in-game settings/cog button here — opens the pause panel.")]
    public Button settingsButton;

    [Header("Button SFX")]
    [Tooltip("Drag epic_stock_media-ui-button-heavy-button-press-metallic-333826 here.")]
    public AudioClip buttonClickClip;

    private AudioSource _sfxSource;

    void Start()
    {
        _sfxSource = gameObject.AddComponent<AudioSource>();
        _sfxSource.playOnAwake = false;

        gameOverPanel.SetActive(false);
        pausePanel.SetActive(false);
        if (pauseBackground != null) { pauseBackground.enabled = false; pauseBackground.raycastTarget = false; }

        settingsButton?.onClick.AddListener(OnSettingsButtonPressed);
    }

    void PlayButtonClick()
    {
        if (_sfxSource != null && buttonClickClip != null)
            _sfxSource.PlayOneShot(buttonClickClip);
    }

    public void UpdateScore(int score)
    {
        scoreText.text = "Score: " + score;
    }

    public void UpdateCombo(int combo, int multiplier)
    {
        comboText.text = $"Combo: {combo}  x{multiplier}";
    }

    public void UpdateAttempts(int attempts)
    {
        attemptsText.text = "Attempts: " + attempts;
    }

    public void ShowGameOver(int score, float timeAlive)
    {
        if (pauseBackground != null) pauseBackground.enabled = true;
        gameOverPanel.SetActive(true);
        finalScoreText.text = "Score: " + score;
        timeText.text = "Time: " + timeAlive.ToString("F1") + "s";
    }

    public void Retry()
    {
        PlayButtonClick();
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void ReturnToMenu()
    {
        PlayButtonClick();
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    public void Resume()
    {
        PlayButtonClick();
        GameManager.Instance.TogglePause();
    }

    void OnSettingsButtonPressed()
    {
        PlayButtonClick();
        GameManager.Instance.TogglePause();
    }

    public void ShowPauseMenu(bool show)
    {
        if (pauseBackground != null) pauseBackground.enabled = show;
        pausePanel.SetActive(show);
    }


}
