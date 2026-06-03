using UnityEngine;
using UnityEngine.Audio;

namespace PuzzleGame.Application.Configuration
{
    /// <summary>
    /// All audio-related data lives here. Inspector-driven, no code changes for tuning.
    /// </summary>
    [CreateAssetMenu(menuName = "PuzzleGame/Audio Config", fileName = "AudioConfig")]
    public class AudioConfig : ScriptableObject
    {
        [Header("Mixer Groups (optional, can be null for 2D fallback)")]
        public AudioMixerGroup musicGroup;
        public AudioMixerGroup sfxGroup;

        [Header("Volumes (0-1)")]
        [Range(0f, 1f)] public float masterVolume = 1.0f;
        [Range(0f, 1f)] public float musicVolume = 0.6f;
        [Range(0f, 1f)] public float sfxVolume = 0.8f;

        [Header("Pool")]
        [Min(1)] public int sfxPoolSize = 8;
        [Min(1)] public int musicPoolSize = 2;

        [Header("Clips")]
        public AudioClip pourLoopClip;
        public AudioClip pourEndClip;
        public AudioClip errorClip;
        public AudioClip levelCompleteClip;
        public AudioClip levelStartClip;
        public AudioClip uiClickClip;
        public AudioClip corkPopClip;

        [Header("Behavior")]
        public bool spatialBlend3D = false; // true → 3D positioned audio
        [Min(0f)] public float spatialMinDistance = 1f;
        [Min(0f)] public float spatialMaxDistance = 20f;
    }
}
