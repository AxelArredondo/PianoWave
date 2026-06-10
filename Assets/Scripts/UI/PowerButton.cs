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

    [Header("TV On Light")]
    [Tooltip("DRAG → Image using Pianowave_TV_on_light.png — glows when on, dark when off")]
    public Image tvOnLightImage;
    [Range(0.6f, 1f)]    public float lightFlickerMin      = 0.82f;
    [Range(0.05f, 0.4f)] public float lightFlickerInterval = 0.10f;

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
    Coroutine _lightFlicker;

    static readonly Color LightOff = new Color(0.12f, 0.12f, 0.12f, 1f);

    void Start()
    {
        if (powerButtonInside != null)
        {
            powerButtonInside.SetActive(false);
            foreach (var img in powerButtonInside.GetComponentsInChildren<Image>(true))
                img.raycastTarget = false;
        }

        if (tvOnLightImage != null)
            _lightFlicker = StartCoroutine(LightFlicker());

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

        if (_lightFlicker != null) StopCoroutine(_lightFlicker);
        if (!_isOn)
            _lightFlicker = StartCoroutine(LightFlicker());
        else
            SetLightOff();

        float vol = GameSettings.Instance != null ? GameSettings.Instance.SFXVolume : 1f;
        if (pressClip != null) _audio.PlayOneShot(pressClip, vol);

        if (_isOn && turnOnClip != null)
            _audio.PlayOneShot(turnOnClip, vol);
        else if (!_isOn && turnOffClip != null)
            StartCoroutine(PlayDelayed(turnOffClip, vol, pressClip != null ? pressClip.length : 0f));
    }

    void SetLightOff()
    {
        if (tvOnLightImage != null)
            tvOnLightImage.color = LightOff;
        _lightFlicker = null;
    }

    IEnumerator LightFlicker()
    {
        while (true)
        {
            float b = Random.Range(lightFlickerMin, 1f);
            tvOnLightImage.color = new Color(b, b, b, 1f);
            yield return new WaitForSeconds(lightFlickerInterval * Random.Range(0.5f, 1.5f));
        }
    }

    IEnumerator PlayDelayed(AudioClip clip, float vol, float delay)
    {
        yield return new WaitForSeconds(delay);
        _audio.PlayOneShot(clip, vol);
    }
}
