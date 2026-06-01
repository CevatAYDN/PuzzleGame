using UnityEngine;

namespace PuzzleGame.Application.Interfaces
{
    /// <summary>
    /// Central audio controller. SFX pool, music track, volume control.
    /// Plain interface — testable, swappable.
    /// </summary>
    public interface IAudioService
    {
        float MasterVolume { get; set; }
        float MusicVolume { get; set; }
        float SfxVolume { get; set; }

        void PlaySfx(AudioClipId id, Vector3? worldPos = null);
        void PlayMusic(AudioClipId id, bool loop = true);
        void StopMusic(float fadeOut = 0.5f);
        void MuteAll();
        void UnmuteAll();
        void ReleaseAll();
    }
}
