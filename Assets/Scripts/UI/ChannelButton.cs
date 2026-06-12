using UnityEngine;
using UnityEngine.UI;

public enum ChannelTarget { Main, Levels, Settings, Stats }

/// Attach to ChannelButtonMain, ChannelButtonLevels, ChannelButtonSettings.
/// Drag the inner "pressed/dark" child GameObject onto buttonInside in the Inspector.
[RequireComponent(typeof(Button))]
public class ChannelButton : MonoBehaviour
{
    [Header("Channel")]
    public ChannelTarget target;

    [Header("Visuals")]
    [Tooltip("DRAG → the dark overlay child inside this button (same idea as PowerButtonInside)")]
    public GameObject buttonInside;

    [Header("Audio")]
    [Tooltip("DRAG → the button press SFX clip")]
    public AudioClip pressClip;

    static ChannelTarget _activeChannel = ChannelTarget.Main;

    AudioSource _audio;
    MainMenuManager _menuManager;

    void Start()
    {
        _audio = gameObject.AddComponent<AudioSource>();
        _audio.playOnAwake = false;

        _menuManager = FindFirstObjectByType<MainMenuManager>();

        GetComponent<Button>().onClick.AddListener(OnPress);
        RefreshVisual();
    }

    void OnPress()
    {
        if (_activeChannel == target) return;

        if (pressClip != null)
        {
            float vol = GameSettings.Instance != null ? GameSettings.Instance.SFXVolume : 1f;
            _audio.PlayOneShot(pressClip, vol);
        }

        switch (target)
        {
            case ChannelTarget.Main:    _menuManager?.GoToMain();        break;
            case ChannelTarget.Levels:  _menuManager?.OpenLevelSelect(); break;
            case ChannelTarget.Settings: _menuManager?.OpenSettings();   break;
            case ChannelTarget.Stats:  _menuManager?.OpenStats();        break;
        }

        NotifyChannelChanged(target);
    }

    public static void NotifyChannelChanged(ChannelTarget channel)
    {
        _activeChannel = channel;
        foreach (var btn in FindObjectsByType<ChannelButton>(FindObjectsSortMode.None))
            btn.RefreshVisual();
    }

    void RefreshVisual()
    {
        if (buttonInside != null)
            buttonInside.SetActive(_activeChannel == target);
    }
}
