using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public AudioSource musicSource;
    public AudioClip musicClip;

    void Start()
    {
        musicSource.clip = musicClip;
        musicSource.Play();
    }

    void Update()
    {
        // tempo scaling later
    }
}
