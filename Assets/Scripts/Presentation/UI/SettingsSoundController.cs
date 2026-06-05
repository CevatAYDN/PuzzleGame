using PuzzleGame.Application.Events;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Logging;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace PuzzleGame.Presentation.UI
{
    /// <summary>
    /// Settings > Sound sub-panel. Lets the player toggle BGM/SFX and adjust volumes.
    /// Reads/writes via IAudioSettingsService (which persists + broadcasts changes).
    /// Back button publishes HideSoundPanelRequestEvent for MainMenu to react to.
    /// SRP: only owns sound-settings UI state and navigation. Persistence is delegated.
    /// </summary>
    public class SettingsSoundController : MonoBehaviour
    {
        private const string LogTag = "[SettingsSound]";

        [Header("Toggles")]
        [SerializeField] private Toggle _musicToggle;
        [SerializeField] private Toggle _sfxToggle;

        [Header("Volume Sliders (0..1)")]
        [SerializeField] private Slider _musicVolumeSlider;
        [SerializeField] private Slider _sfxVolumeSlider;

        [Header("Navigation")]
        [SerializeField] private Button _backButton;

        private IEventAggregator _events;
        private IAudioSettingsService _audioSettings;
        private IAudioService _audio;

        [Inject]
        public void Construct(
            IEventAggregator events,
            IAudioSettingsService audioSettings,
            IAudioService audio)
        {
            _events = events;
            _audioSettings = audioSettings;
            _audio = audio;
        }

        private void OnEnable()
        {
            if (_musicToggle != null) _musicToggle.onValueChanged.AddListener(OnMusicToggled);
            if (_sfxToggle != null) _sfxToggle.onValueChanged.AddListener(OnSfxToggled);
            if (_musicVolumeSlider != null) _musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            if (_sfxVolumeSlider != null) _sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
            if (_backButton != null) _backButton.onClick.AddListener(OnBackClicked);

            if (_events != null)
                _events.Subscribe<AudioSettingsChangedEvent>(OnAudioSettingsChanged);

            RefreshFromSettings();
        }

        private void OnDisable()
        {
            if (_musicToggle != null) _musicToggle.onValueChanged.RemoveListener(OnMusicToggled);
            if (_sfxToggle != null) _sfxToggle.onValueChanged.RemoveListener(OnSfxToggled);
            if (_musicVolumeSlider != null) _musicVolumeSlider.onValueChanged.RemoveListener(OnMusicVolumeChanged);
            if (_sfxVolumeSlider != null) _sfxVolumeSlider.onValueChanged.RemoveListener(OnSfxVolumeChanged);
            if (_backButton != null) _backButton.onClick.RemoveListener(OnBackClicked);

            if (_events != null)
                _events.Unsubscribe<AudioSettingsChangedEvent>(OnAudioSettingsChanged);
        }

        public void Show() => gameObject.SetActive(true);
        public void Hide() => gameObject.SetActive(false);

        private void RefreshFromSettings()
        {
            if (_audioSettings == null) return;
            var s = _audioSettings.Current;
            if (_musicToggle != null) _musicToggle.SetIsOnWithoutNotify(s.MusicEnabled);
            if (_sfxToggle != null) _sfxToggle.SetIsOnWithoutNotify(s.SfxEnabled);
            if (_musicVolumeSlider != null) _musicVolumeSlider.SetValueWithoutNotify(s.MusicVolume);
            if (_sfxVolumeSlider != null) _sfxVolumeSlider.SetValueWithoutNotify(s.SfxVolume);

            ApplyInteractability(s);
        }

        private void ApplyInteractability(AudioPreferences s)
        {
            if (_musicVolumeSlider != null) _musicVolumeSlider.interactable = s.MusicEnabled;
            if (_sfxVolumeSlider != null) _sfxVolumeSlider.interactable = s.SfxEnabled;
        }

        private void OnMusicToggled(bool enabled)
        {
            if (_audioSettings == null) return;
            _audioSettings.SetMusicEnabled(enabled);
            ApplyInteractability(_audioSettings.Current);
        }

        private void OnSfxToggled(bool enabled)
        {
            if (_audioSettings == null) return;
            _audioSettings.SetSfxEnabled(enabled);
            ApplyInteractability(_audioSettings.Current);
        }

        private void OnMusicVolumeChanged(float value)
        {
            _audioSettings?.SetMusicVolume(value);
        }

        private void OnSfxVolumeChanged(float value)
        {
            _audioSettings?.SetSfxVolume(value);
        }

        private void OnAudioSettingsChanged(AudioSettingsChangedEvent evt)
        {
            // External change (e.g. reset) — sync UI without echoing back to the service
            var s = evt.NewSettings;
            if (_musicToggle != null) _musicToggle.SetIsOnWithoutNotify(s.MusicEnabled);
            if (_sfxToggle != null) _sfxToggle.SetIsOnWithoutNotify(s.SfxEnabled);
            if (_musicVolumeSlider != null) _musicVolumeSlider.SetValueWithoutNotify(s.MusicVolume);
            if (_sfxVolumeSlider != null) _sfxVolumeSlider.SetValueWithoutNotify(s.SfxVolume);
            ApplyInteractability(s);

            // Apply effective volumes to the live audio engine
            if (_audio != null)
            {
                _audio.MusicVolume = s.EffectiveMusicVolume;
                _audio.SfxVolume = s.EffectiveSfxVolume;
            }
        }

        private void OnBackClicked()
        {
            MoldLogger.LogInfo($"{LogTag} Back clicked.");
            _events?.Publish(new HideSoundPanelRequestEvent());
        }
    }

    /// <summary>
    /// Published by SettingsSoundController when "Back" is clicked.
    /// MainMenuController subscribes to reactivate its root panel.
    /// </summary>
    public class HideSoundPanelRequestEvent { }
}
