using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace PuzzleGame.Editor
{
    /// <summary>
    /// CI/CD build entry points. Called by GitHub Actions via
    /// game-ci/unity-builder with -executeMethod.
    /// </summary>
    public static class BuildScript
    {
        private const string AndroidDefine = "HAS_GOOGLE_MOBILE_ADS";

        public static void BuildAndroidRelease()
        {
            Debug.Log("[BuildScript] Starting Android IL2CPP Release build...");
            AddressablesInstaller.Setup();

            // Ensure AdMob define is set for release builds
            var androidTarget = UnityEditor.Build.NamedBuildTarget.Android;
            string currentDefines = PlayerSettings.GetScriptingDefineSymbols(androidTarget);
            if (!currentDefines.Contains(AndroidDefine))
            {
                PlayerSettings.SetScriptingDefineSymbols(androidTarget,
                    string.IsNullOrEmpty(currentDefines)
                        ? AndroidDefine
                        : currentDefines + ";" + AndroidDefine);
                Debug.Log($"[BuildScript] Added {AndroidDefine} to Android scripting defines.");
            }

            // IL2CPP is the default scripting backend for Android in Unity 6000+;
            // BuildOptions.Il2Cpp was removed. Use StrictMode for CI enforcement.
            var options = new BuildPlayerOptions
            {
                scenes = GetEnabledScenes(),
                locationPathName = "build/Android/PuzzleGame.apk",
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.StrictMode
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[BuildScript] Android build SUCCESS — {summary.totalSize / (1024 * 1024)} MB, " +
                          $"time={summary.totalTime.TotalSeconds:F1}s");
            }
            else
            {
                string errorMsg = $"[BuildScript] Android build FAILED — {summary.result}, errors={summary.totalErrors}, warnings={summary.totalWarnings}";
                Debug.LogError(errorMsg);
                throw new UnityEditor.Build.BuildFailedException(errorMsg);
            }
        }

        public static void BuildWindowsRelease()
        {
            Debug.Log("[BuildScript] Starting Windows Standalone build...");
            AddressablesInstaller.Setup();

            var options = new BuildPlayerOptions
            {
                scenes = GetEnabledScenes(),
                locationPathName = "build/Windows/PuzzleGame.exe",
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.StrictMode
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[BuildScript] Windows build SUCCESS — {summary.totalSize / (1024 * 1024)} MB, " +
                          $"time={summary.totalTime.TotalSeconds:F1}s");
            }
            else
            {
                string errorMsg = $"[BuildScript] Windows build FAILED — {summary.result}, errors={summary.totalErrors}, warnings={summary.totalWarnings}";
                Debug.LogError(errorMsg);
                throw new UnityEditor.Build.BuildFailedException(errorMsg);
            }
        }

        public static void BuildiOSRelease()
        {
            Debug.Log("[BuildScript] Starting iOS build...");
            AddressablesInstaller.Setup();

            var options = new BuildPlayerOptions
            {
                scenes = GetEnabledScenes(),
                locationPathName = "build/iOS",
                target = BuildTarget.iOS,
                targetGroup = BuildTargetGroup.iOS,
                options = BuildOptions.StrictMode
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[BuildScript] iOS build SUCCESS — time={summary.totalTime.TotalSeconds:F1}s");
            }
            else
            {
                string errorMsg = $"[BuildScript] iOS build FAILED — {summary.result}, errors={summary.totalErrors}, warnings={summary.totalWarnings}";
                Debug.LogError(errorMsg);
                throw new UnityEditor.Build.BuildFailedException(errorMsg);
            }
        }

        private static string[] GetEnabledScenes()
        {
            var scenes = new System.Collections.Generic.List<string>();
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (scene.enabled)
                    scenes.Add(scene.path);
            }
            return scenes.ToArray();
        }
    }
}
