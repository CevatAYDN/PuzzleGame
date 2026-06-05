using PuzzleGame.Application.Configuration;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Logging;
using UnityEngine;

namespace PuzzleGame.Application.Services
{
    /// <summary>
    /// Cross-platform haptic feedback.
    /// Mobile (Android/iOS) uses native APIs; editor / desktop uses no-op with structured logging.
    /// </summary>
    public sealed class HapticFeedbackService : IHapticFeedbackService
    {
        private const string LogTag = "[Haptics]";

        public bool IsEnabled { get; set; } = true;

        public void Trigger(HapticIntensity intensity)
        {
            if (!IsEnabled) return;

#if UNITY_ANDROID && !UNITY_EDITOR
            TriggerAndroid(intensity);
#elif UNITY_IOS && !UNITY_EDITOR
            TriggerIOS(intensity);
#else
            if (MoldLogger.IsDebugEnabled)
                MoldLogger.LogDebug($"{LogTag} Triggered {intensity} (no-op on this platform).");
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private static AndroidJavaClass GetVibrator()
        {
            using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            using var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            return new AndroidJavaClass("com.puzzlegame.haptics.HapticBridge").CallStatic<AndroidJavaClass>("getInstance", activity);
        }

        private static void TriggerAndroid(HapticIntensity intensity)
        {
            try
            {
                int ms = intensity switch
                {
                    HapticIntensity.Light => 10,
                    HapticIntensity.Medium => 20,
                    HapticIntensity.Heavy => 40,
                    HapticIntensity.Selection => 5,
                    HapticIntensity.Success => 30,
                    HapticIntensity.Warning => 35,
                    HapticIntensity.Error => 50,
                    _ => 15
                };
                Handheld.Vibrate();
            }
            catch (System.Exception e)
            {
                MoldLogger.LogWarning($"{LogTag} Android vibrate failed: {e.Message}");
            }
        }
#endif

#if UNITY_IOS && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern void _PGTriggerHaptic(int style);

        private static void TriggerIOS(HapticIntensity intensity)
        {
            int style = intensity switch
            {
                HapticIntensity.Light => 0,
                HapticIntensity.Medium => 1,
                HapticIntensity.Heavy => 2,
                HapticIntensity.Selection => 3,
                HapticIntensity.Success => 4,
                HapticIntensity.Warning => 5,
                HapticIntensity.Error => 6,
                _ => 1
            };
            _PGTriggerHaptic(style);
        }
#endif
    }
}
