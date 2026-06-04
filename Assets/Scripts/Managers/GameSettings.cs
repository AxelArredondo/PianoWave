using UnityEngine;

// Persists between scenes so the main menu can pass the selected mode to the game scene.
public class GameSettings : MonoBehaviour
{
    public static GameSettings Instance { get; private set; }

    public GameMode Mode = GameMode.RandomMode;
    public string ChartResourcePath = "Charts/Level1";

    [Range(0f, 1f)] public float MasterVolume = 1f;
    [Range(0f, 1f)] public float MusicVolume  = 1f;
    [Range(0f, 1f)] public float SFXVolume    = 1f;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadVolumes();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void LoadVolumes()
    {
        MasterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        MusicVolume  = PlayerPrefs.GetFloat("MusicVolume",  1f);
        SFXVolume    = PlayerPrefs.GetFloat("SFXVolume",    1f);
    }

    public void SaveVolumes()
    {
        PlayerPrefs.SetFloat("MasterVolume", MasterVolume);
        PlayerPrefs.SetFloat("MusicVolume",  MusicVolume);
        PlayerPrefs.SetFloat("SFXVolume",    SFXVolume);
        PlayerPrefs.Save();
    }
}
