using System;

namespace PuzzleGame.Application.Interfaces
{
    /// <summary>
    /// Immutable snapshot of player audio preferences.
    /// Held by IAudioSettingsService and broadcast on change.
    /// Independent of Unity (testable in plain C#).
    /// Named "AudioPreferences" (not "AudioSettings") to avoid collision with UnityEngine.AudioSettings.
    /// </summary>
    public readonly struct AudioPreferences : IEquatable<AudioPreferences>
    {
        public bool MusicEnabled { get; }
        public bool SfxEnabled { get; }
        /// <summary>0..1, the user-chosen music level. Effective volume = MusicEnabled ? MusicVolume : 0.</summary>
        public float MusicVolume { get; }
        /// <summary>0..1, the user-chosen SFX level. Effective volume = SfxEnabled ? SfxVolume : 0.</summary>
        public float SfxVolume { get; }

        public AudioPreferences(bool musicEnabled, bool sfxEnabled, float musicVolume, float sfxVolume)
        {
            MusicEnabled = musicEnabled;
            SfxEnabled = sfxEnabled;
            MusicVolume = Clamp01(musicVolume);
            SfxVolume = Clamp01(sfxVolume);
        }

        public static AudioPreferences Default => new AudioPreferences(
            musicEnabled: true,
            sfxEnabled: true,
            musicVolume: 0.6f,
            sfxVolume: 0.8f);

        public float EffectiveMusicVolume => MusicEnabled ? MusicVolume : 0f;
        public float EffectiveSfxVolume => SfxEnabled ? SfxVolume : 0f;

        public AudioPreferences WithMusicEnabled(bool enabled) =>
            new AudioPreferences(enabled, SfxEnabled, MusicVolume, SfxVolume);
        public AudioPreferences WithSfxEnabled(bool enabled) =>
            new AudioPreferences(MusicEnabled, enabled, MusicVolume, SfxVolume);
        public AudioPreferences WithMusicVolume(float v) =>
            new AudioPreferences(MusicEnabled, SfxEnabled, v, SfxVolume);
        public AudioPreferences WithSfxVolume(float v) =>
            new AudioPreferences(MusicEnabled, SfxEnabled, MusicVolume, v);

        public bool Equals(AudioPreferences other) =>
            MusicEnabled == other.MusicEnabled &&
            SfxEnabled == other.SfxEnabled &&
            MusicVolume.Equals(other.MusicVolume) &&
            SfxVolume.Equals(other.SfxVolume);

        public override bool Equals(object obj) => obj is AudioPreferences s && Equals(s);
        public override int GetHashCode() =>
            (MusicEnabled ? 1 : 0) ^ (SfxEnabled ? 2 : 0) ^ MusicVolume.GetHashCode() ^ SfxVolume.GetHashCode();

        public static bool operator ==(AudioPreferences left, AudioPreferences right) => left.Equals(right);
        public static bool operator !=(AudioPreferences left, AudioPreferences right) => !left.Equals(right);

        private static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
    }

    /// <summary>
    /// Persistent audio settings service. Single source of truth for BGM/SFX
    /// enable flags and volume levels. Survives app restarts via PlayerPrefs.
    /// Consumers subscribe to <see cref="OnSettingsChanged"/> to react.
    /// </summary>
    public interface IAudioSettingsService
    {
        AudioPreferences Current { get; }
        event Action<AudioPreferences> OnSettingsChanged;

        void SetMusicEnabled(bool enabled);
        void SetSfxEnabled(bool enabled);
        void SetMusicVolume(float volume);
        void SetSfxVolume(float volume);

        /// <summary>Force a Save() — usually automatic after each setter.</summary>
        void Save();

        /// <summary>Reset to factory defaults and persist.</summary>
        void ResetToDefaults();
    }
}
