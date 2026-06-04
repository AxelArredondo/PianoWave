using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// Attach to the PowerButton GameObject inside TVLayer.
/// Drag the PowerButtonInside GameObject onto powerButtonInside in the Inspector.
[RequireComponent(typeof(Button))]
public class PowerButton : MonoBehaviour
{
    [Header("TV Layer")]
    [Tooltip("DRAG → PowerButtonInside inside TVLayer")]
    public GameObject powerButtonInside;

    [Header("Audio")]
    [Tooltip("DRAG → soundreality-button-202966")]
    public AudioClip pressClip;
    [Tooltip("DRAG → dragon-studio-tv-shutdown-386167 — plays when turning ON")]
    public AudioClip turnOnClip;
    [Tooltip("DRAG → freesound_community-japantvturnson-106431 — plays when turning OFF")]
    public AudioClip turnOffClip;

    AudioSource _audio;
    MainMenuUISetup _uiSetup;
    bool _isOn = false;

    void Start()
    {
        if (powerButtonInside != null)
        {
            powerButtonInside.SetActive(false);
            foreach (var img in powerButtonInside.GetComponentsInChildren<Image>(true))
                img.raycastTarget = false;
        }

        _audio = gameObject.AddComponent<AudioSource>();
        _audio.playOnAwake = false;

        _uiSetup = FindFirstObjectByType<MainMenuUISetup>();

        GetComponent<Button>().onClick.AddListener(OnPress);
    }

    void OnPress()
    {
        _isOn = !_isOn;
        if (powerButtonInside != null)
            powerButtonInside.SetActive(_isOn);

        _uiSetup?.SetCRTPoweredOn(_isOn);

        float vol = GameSettings.Instance != null ? GameSettings.Instance.SFXVolume : 1f;
        if (pressClip != null) _audio.PlayOneShot(pressClip, vol);

        if (_isOn && turnOnClip != null)
            _audio.PlayOneShot(turnOnClip, vol);
        else if (!_isOn && turnOffClip != null)
            StartCoroutine(PlayDelayed(turnOffClip, vol, pressClip != null ? pressClip.length : 0f));
    }

    IEnumerator PlayDelayed(AudioClip clip, float vol, float delay)
    {
        yield return new WaitForSeconds(delay);
        _audio.PlayOneShot(clip, vol);
    }
}
