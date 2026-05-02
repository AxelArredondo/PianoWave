using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Music")]
    public AudioSource musicSource;
    public AudioClip musicClip;

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

        PlayFromStart();
    }

    public void PlayFromStart()
    {
        if (musicSource == null || musicSource.clip == null) return;
        musicSource.Stop();
        musicSource.time = 0f;
        musicSource.Play();
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

        // One-shot so it doesn't interrupt other SFX
        sfxSource.PlayOneShot(gameOverClip);
    }
}