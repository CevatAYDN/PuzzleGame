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

        // Non-spatial 2D SFX — UI clicks, level transitions, cast state changes, etc.
        void PlaySfx(AudioClipId id);
        // Spatial 3D SFX — emitted at a world position (e.g. cork pop, world-anchored effects).
        void PlaySfxAt(AudioClipId id, Vector3 worldPos);
        void PlayMusic(AudioClipId id, bool loop = true);
        void StopMusic(float fadeOut = 0.5f);
        void MuteAll();
        void UnmuteAll();
        void ReleaseAll();
    }
}
