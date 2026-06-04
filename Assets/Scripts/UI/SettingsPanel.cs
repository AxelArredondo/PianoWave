using UnityEngine;
using UnityEngine.UI;

/// Attach to the SettingsPanel GameObject.
/// Wire the three sliders and the back button in the Inspector,
/// then drag Pianowave_slider_panel onto sliderBackgroundSprite
/// and Pianowave_buttons_images_3 onto sliderHandleSprite.
public class SettingsPanel : MonoBehaviour
{
    [Header("Sliders")]
    [Tooltip("DRAG → MasterSlider")]
    public Slider masterSlider;
    [Tooltip("DRAG → MusicSlider")]
    public Slider musicSlider;
    [Tooltip("DRAG → SFXSlider")]
    public Slider sfxSlider;

    [Header("Back Button")]
    [Tooltip("DRAG → BackButton inside SettingsPanel")]
    public Button backButton;

    [Header("Slider Sprites")]
    [Tooltip("DRAG → Pianowave_slider_panel — applied as the Background image on every slider.")]
    public Sprite sliderBackgroundSprite;
    [Tooltip("DRAG → Pianowave_buttons_images_3 — applied as the Handle image on every slider.")]
    public Sprite sliderHandleSprite;

    [Header("Handle Size")]
    [Tooltip("Width of the slider handle in canvas units. Increase to make it wider.")]
    public float handleWidth = 30f;
    [Tooltip("Height of the slider handle in canvas units. 0 = leave at its current height.")]
    public float handleHeight = 0f;

    Image _masterHandle;
    Image _musicHandle;
    Image _sfxHandle;

    void Start()
    {
        _masterHandle = GetHandle(masterSlider);
        _musicHandle  = GetHandle(musicSlider);
        _sfxHandle    = GetHandle(sfxSlider);

        ApplySprites(masterSlider, _masterHandle);
        ApplySprites(musicSlider,  _musicHandle);
        ApplySprites(sfxSlider,    _sfxHandle);

        masterSlider?.onValueChanged.AddListener(OnMasterChanged);
        musicSlider?.onValueChanged.AddListener(OnMusicChanged);
        sfxSlider?.onValueChanged.AddListener(OnSFXChanged);

        backButton?.onClick.AddListener(OnBack);
    }

    void Update()
    {
        ApplyHandleSize(_masterHandle);
        ApplyHandleSize(_musicHandle);
        ApplyHandleSize(_sfxHandle);
    }

    void OnEnable()
    {
        if (GameSettings.Instance == null) return;
        masterSlider?.SetValueWithoutNotify(GameSettings.Instance.MasterVolume);
        musicSlider?.SetValueWithoutNotify(GameSettings.Instance.MusicVolume);
        sfxSlider?.SetValueWithoutNotify(GameSettings.Instance.SFXVolume);
    }

    void OnMasterChanged(float v)
    {
        AudioListener.volume = v;
        if (GameSettings.Instance != null) GameSettings.Instance.MasterVolume = v;
    }

    void OnMusicChanged(float v)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.SetMusicVolume(v);
        if (GameSettings.Instance != null) GameSettings.Instance.MusicVolume = v;
    }

    void OnSFXChanged(float v)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.SetSFXVolume(v);
        if (GameSettings.Instance != null) GameSettings.Instance.SFXVolume = v;
    }

    void OnBack()
    {
        if (GameSettings.Instance != null) GameSettings.Instance.SaveVolumes();
        var mgr = FindFirstObjectByType<MainMenuManager>();
        mgr?.BackFromSettings();
    }

    Image GetHandle(Slider slider)
    {
        return slider?.transform.Find("Handle Slide Area/Handle")?.GetComponent<Image>();
    }

    void ApplySprites(Slider slider, Image handle)
    {
        if (slider == null) return;

        if (sliderBackgroundSprite != null)
        {
            var bg = slider.transform.Find("Background")?.GetComponent<Image>();
            if (bg != null) bg.sprite = sliderBackgroundSprite;
        }

        if (handle != null && sliderHandleSprite != null)
            handle.sprite = sliderHandleSprite;

        ApplyHandleSize(handle);
    }

    void ApplyHandleSize(Image handle)
    {
        if (handle == null) return;
        var size = handle.rectTransform.sizeDelta;
        size.x = handleWidth;
        if (handleHeight > 0f) size.y = handleHeight;
        handle.rectTransform.sizeDelta = size;
    }
}
