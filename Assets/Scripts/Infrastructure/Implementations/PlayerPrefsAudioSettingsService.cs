using System;
using PuzzleGame.Application.Events;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Logging;
using UnityEngine;

namespace PuzzleGame.Infrastructure.Implementations
{
    /// <summary>
    /// PlayerPrefs-backed audio settings. Single source of truth that survives
    /// app restarts. Each setter mutates, persists, and raises the changed event.
    /// Subscribe to <see cref="OnSettingsChanged"/> to apply to IAudioService.
    /// </summary>
    public sealed class PlayerPrefsAudioSettingsService : IAudioSettingsService
    {
        private const string LogTag = "[AudioSettings]";

        // Pref keys — namespaced under PuzzleGame.Audio.* to avoid collisions
        private const string KeyMusicEnabled = "PuzzleGame.Audio.MusicEnabled";
        private const string KeySfxEnabled   = "PuzzleGame.Audio.SfxEnabled";
        private const string KeyMusicVolume  = "PuzzleGame.Audio.MusicVolume";
        private const string KeySfxVolume    = "PuzzleGame.Audio.SfxVolume";

        private AudioPreferences _current;

        public AudioPreferences Current => _current;

        public event Action<AudioPreferences> OnSettingsChanged;

        public PlayerPrefsAudioSettingsService(IEventAggregator eventAggregator = null)
        {
            _current = LoadFromPrefs();

            if (eventAggregator != null)
                OnSettingsChanged += s => eventAggregator.Publish(new AudioSettingsChangedEvent(s));
        }

        public void SetMusicEnabled(bool enabled)
        {
            var next = _current.WithMusicEnabled(enabled);
            if (next.Equals(_current)) return;
            ApplyAndPersist(next);
        }

        public void SetSfxEnabled(bool enabled)
        {
            var next = _current.WithSfxEnabled(enabled);
            if (next.Equals(_current)) return;
            ApplyAndPersist(next);
        }

        public void SetMusicVolume(float volume)
        {
            var next = _current.WithMusicVolume(volume);
            if (next.Equals(_current)) return;
            ApplyAndPersist(next);
        }

        public void SetSfxVolume(float volume)
        {
            var next = _current.WithSfxVolume(volume);
            if (next.Equals(_current)) return;
            ApplyAndPersist(next);
        }

        public void Save() => PlayerPrefs.Save();

        public void ResetToDefaults()
        {
            ApplyAndPersist(AudioPreferences.Default);
        }

        private void ApplyAndPersist(AudioPreferences next)
        {
            _current = next;
            WriteToPrefs(_current);
            MoldLogger.LogInfo($"{LogTag} Settings changed: music={next.MusicEnabled}/{next.MusicVolume:F2}, sfx={next.SfxEnabled}/{next.SfxVolume:F2}.");
            OnSettingsChanged?.Invoke(_current);
        }

        private static AudioPreferences LoadFromPrefs()
        {
            return new AudioPreferences(
                musicEnabled: PlayerPrefs.GetInt(KeyMusicEnabled, 1) == 1,
                sfxEnabled:   PlayerPrefs.GetInt(KeySfxEnabled,   1) == 1,
                musicVolume:  PlayerPrefs.GetFloat(KeyMusicVolume, AudioPreferences.Default.MusicVolume),
                sfxVolume:    PlayerPrefs.GetFloat(KeySfxVolume,   AudioPreferences.Default.SfxVolume));
        }

        private static void WriteToPrefs(AudioPreferences s)
        {
            PlayerPrefs.SetInt(KeyMusicEnabled, s.MusicEnabled ? 1 : 0);
            PlayerPrefs.SetInt(KeySfxEnabled,   s.SfxEnabled   ? 1 : 0);
            PlayerPrefs.SetFloat(KeyMusicVolume, s.MusicVolume);
            PlayerPrefs.SetFloat(KeySfxVolume,   s.SfxVolume);
            PlayerPrefs.Save();
        }
    }
}
