using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class AmbientSFXEntry
{
    public AudioClip clip;
    [Range(0f, 1f)] public float volume = 1f;
    public bool loop     = false;
    public bool autoPlay = true;
}

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
    public AudioClip hitSFXClip;
    public AudioClip gameOverLoopClip;
    [Range(0f, 1f)] public float gameOverLoopVolume = 1f;

    private AudioSource gameOverLoopSource;

    [Header("Ambient SFX — add clips here, toggle loop and autoPlay per entry")]
    public AmbientSFXEntry[] ambientSFX;

    // Runtime AudioSources created for each ambient entry (parallel array).
    private AudioSource[] ambientSources;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    void Start()
    {
        if (musicSource == null)
            musicSource = GetComponent<AudioSource>();

        // sfxSource must be a separate component from musicSource
        if (sfxSource == null || sfxSource == musicSource)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
        }

        if (musicSource != null && musicClip != null)
            musicSource.clip = musicClip;

        StartAmbientSFX();
        ApplySavedVolumes();

        // In LevelMode the ChartSpawner sets the clip and controls when playback starts.
        bool levelMode = GameSettings.Instance != null && GameSettings.Instance.Mode == GameMode.LevelMode;
        if (!levelMode)
            PlayFromStart();
    }

    void ApplySavedVolumes()
    {
        if (GameSettings.Instance == null) return;
        AudioListener.volume = GameSettings.Instance.MasterVolume;
        SetMusicVolume(GameSettings.Instance.MusicVolume);
        SetSFXVolume(GameSettings.Instance.SFXVolume);
    }

    public void SetMusicVolume(float v)
    {
        if (musicSource != null) musicSource.volume = v;
    }

    public void SetSFXVolume(float v)
    {
        if (sfxSource != null) sfxSource.volume = v;
        if (gameOverLoopSource != null) gameOverLoopSource.volume = v;
    }

    void StartAmbientSFX()
    {
        if (ambientSFX == null || ambientSFX.Length == 0) return;

        ambientSources = new AudioSource[ambientSFX.Length];
        for (int i = 0; i < ambientSFX.Length; i++)
        {
            var entry = ambientSFX[i];
            if (entry.clip == null) continue;

            var src = gameObject.AddComponent<AudioSource>();
            src.clip        = entry.clip;
            src.volume      = entry.volume;
            src.loop        = entry.loop;
            src.playOnAwake = false;
            ambientSources[i] = src;

            if (entry.autoPlay) src.Play();
        }
    }

    // Play an ambient entry by its clip name.
    public void PlayAmbient(string clipName)
    {
        int i = FindAmbientIndex(clipName);
        if (i < 0 || ambientSources == null || ambientSources[i] == null) return;
        ambientSources[i].Play();
    }

    // Stop an ambient entry by its clip name.
    public void StopAmbient(string clipName)
    {
        int i = FindAmbientIndex(clipName);
        if (i < 0 || ambientSources == null || ambientSources[i] == null) return;
        ambientSources[i].Stop();
    }

    int FindAmbientIndex(string clipName)
    {
        if (ambientSFX == null) return -1;
        for (int i = 0; i < ambientSFX.Length; i++)
            if (ambientSFX[i].clip != null && ambientSFX[i].clip.name == clipName) return i;
        return -1;
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

    public void PlayGameOverLoop()
    {
        if (gameOverLoopClip == null) return;

        if (gameOverLoopSource == null)
        {
            gameOverLoopSource = gameObject.AddComponent<AudioSource>();
            gameOverLoopSource.playOnAwake = false;
            gameOverLoopSource.loop = true;
        }

        gameOverLoopSource.clip   = gameOverLoopClip;
        gameOverLoopSource.volume = gameOverLoopVolume;
        gameOverLoopSource.Play();
    }

    public void StopGameOverLoop()
    {
        gameOverLoopSource?.Stop();
    }

    public void PlayHitSFX()
    {
        if (sfxSource == null || hitSFXClip == null) return;
        sfxSource.PlayOneShot(hitSFXClip);
    }
}
