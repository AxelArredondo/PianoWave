using UnityEngine;

// Persists between scenes so the main menu can pass the selected mode to the game scene.
public class GameSettings : MonoBehaviour
{
    public static GameSettings Instance { get; private set; }

    public GameMode Mode = GameMode.RandomMode;
    public string ChartResourcePath = "Charts/Level1";

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
