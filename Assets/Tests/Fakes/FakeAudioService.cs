using UnityEngine;
using PuzzleGame.Application.Interfaces;

namespace PuzzleGame.Tests.Fakes
{
    /// <summary>
    /// Controllable fake for IAudioService. Records all calls, no-op audio.
    /// </summary>
    public class FakeAudioService : IAudioService
    {
        public float MasterVolume { get; set; } = 1f;
        public float MusicVolume { get; set; } = 1f;
        public float SfxVolume { get; set; } = 1f;

        public int PlaySfxCallCount { get; private set; }
        public int PlayMusicCallCount { get; private set; }
        public int StopMusicCallCount { get; private set; }
        public int MuteAllCallCount { get; private set; }
        public int UnmuteAllCallCount { get; private set; }
        public int ReleaseAllCallCount { get; private set; }

        public AudioClipId LastSfxId { get; private set; }
        public AudioClipId LastMusicId { get; private set; }
        public Vector3? LastSfxWorldPos { get; private set; }

        public void PlaySfx(AudioClipId id)
        {
            PlaySfxCallCount++;
            LastSfxId = id;
            LastSfxWorldPos = null;
        }

        public void PlaySfxAt(AudioClipId id, Vector3 worldPos)
        {
            PlaySfxCallCount++;
            LastSfxId = id;
            LastSfxWorldPos = worldPos;
        }

        public void PlayMusic(AudioClipId id, bool loop = true)
        {
            PlayMusicCallCount++;
            LastMusicId = id;
        }

        public void StopMusic(float fadeOut = 0.5f)
        {
            StopMusicCallCount++;
        }

        public void MuteAll()
        {
            MuteAllCallCount++;
        }

        public void UnmuteAll()
        {
            UnmuteAllCallCount++;
        }

        public void ReleaseAll()
        {
            ReleaseAllCallCount++;
        }
    }
}
