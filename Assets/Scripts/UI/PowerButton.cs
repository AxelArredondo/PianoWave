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

    AudioSource _audio;
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

        GetComponent<Button>().onClick.AddListener(OnPress);
    }

    void OnPress()
    {
        _isOn = !_isOn;
        if (powerButtonInside != null)
            powerButtonInside.SetActive(_isOn);

        if (pressClip != null)
        {
            float vol = GameSettings.Instance != null ? GameSettings.Instance.SFXVolume : 1f;
            _audio.PlayOneShot(pressClip, vol);
        }
    }
}
