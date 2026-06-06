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
        private static void TriggerAndroid(HapticIntensity intensity)
        {
            try
            {
                int ms = intensity switch
                {
                    HapticIntensity.Light => 15,
                    HapticIntensity.Medium => 30,
                    HapticIntensity.Heavy => 60,
                    HapticIntensity.Selection => 10,
                    HapticIntensity.Success => 45,
                    HapticIntensity.Warning => 50,
                    HapticIntensity.Error => 80,
                    _ => 25
                };

                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator"))
                {
                    if (vibrator != null && vibrator.Call<bool>("hasVibrator"))
                    {
                        using (var buildVersion = new AndroidJavaClass("android.os.Build$VERSION"))
                        {
                            int sdkInt = buildVersion.GetStatic<int>("SDK_INT");
                            if (sdkInt >= 26)
                            {
                                using (var vibrationEffectClass = new AndroidJavaClass("android.os.VibrationEffect"))
                                using (var effect = vibrationEffectClass.CallStatic<AndroidJavaObject>("createOneShot", (long)ms, -1))
                                {
                                    vibrator.Call("vibrate", effect);
                                }
                            }
                            else
                            {
                                vibrator.Call("vibrate", (long)ms);
                            }
                        }
                    }
                    else
                    {
                        Handheld.Vibrate();
                    }
                }
            }
            catch (System.Exception e)
            {
                MoldLogger.LogWarning($"{LogTag} Android vibrate failed: {e.Message}");
                Handheld.Vibrate();
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
