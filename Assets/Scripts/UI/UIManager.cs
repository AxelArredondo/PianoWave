using UnityEngine;
using UnityEngine.SceneManagement;
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

    void Start()
    {
        gameOverPanel.SetActive(false);
        gameOverPanel.SetActive(false);
        pausePanel.SetActive(false);
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
        gameOverPanel.SetActive(true);
        finalScoreText.text = "Score: " + score;
        timeText.text = "Time: " + timeAlive.ToString("F1") + "s";
    }

    public void Retry()
    {
        Time.timeScale = 1f; 
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void ShowPauseMenu(bool show)
    {
        pausePanel.SetActive(show);
    }


}
