using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Music")]
    public AudioSource musicSource;
    public AudioClip musicClip;

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
}