using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Music")]
    public AudioSource musicSource;
    public AudioClip musicClip;

    [Header("Music Library — add clips here to reference by name in chart JSON")]
    public AudioClip[] musicLibrary;

    [Header("SFX")]
    public AudioSource sfxSource;
    public AudioClip gameOverClip;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    void Start()
    {
        if (musicSource == null)
            musicSource = GetComponent<AudioSource>();

        if (musicSource != null && musicClip != null)
            musicSource.clip = musicClip;

        // In LevelMode the ChartSpawner sets the clip and controls when playback starts.
        bool levelMode = GameSettings.Instance != null && GameSettings.Instance.Mode == GameMode.LevelMode;
        if (!levelMode)
            PlayFromStart();
    }

    // Sets musicSource.clip by matching name against musicLibrary.
    // Assign Pianowave-themesong in the musicLibrary array in the Inspector.
    public void SetMusicByName(string clipName)
    {
        if (musicLibrary == null) return;
        foreach (var clip in musicLibrary)
        {
            if (clip != null && clip.name == clipName)
            {
                musicSource.clip = clip;
                return;
            }
        }
        Debug.LogWarning($"AudioManager: clip '{clipName}' not found in musicLibrary.");
    }

    public void PlayFromStart()
    {
        if (musicSource == null || musicSource.clip == null) return;
        musicSource.Stop();
        musicSource.time = 0f;
        musicSource.Play();
    }

    public void Pause()
    {
        musicSource?.Pause();
    }

    public void Resume()
    {
        musicSource?.UnPause();
    }

    public void StopAndReset()
    {
        if (musicSource == null) return;
        musicSource.Stop();
        musicSource.time = 0f;
    }

    public void PlayGameOverSFX()
    {
        if (sfxSource == null || gameOverClip == null) return;
        sfxSource.PlayOneShot(gameOverClip);
    }
}
