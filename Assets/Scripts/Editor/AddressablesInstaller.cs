#if ENABLE_ADDRESSABLES
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace PuzzleGame.Editor
{
    /// <summary>
    /// One-click Addressables setup for the project.
    /// Creates groups, labels, and assigns addresses for all LevelData SOs.
    /// Run via: Window → PuzzleGame → Setup Addressables
    /// </summary>
    public static class AddressablesInstaller
    {
        private const string LevelDataLabel = "LevelData";
        private const string LevelAddressFormat = "Level_{0:D2}";

        [MenuItem("Tools/PuzzleGame/Setup Addressables")]
        public static void Setup()
        {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            if (settings == null)
            {
                Debug.LogError("[AddressablesInstaller] Could not get Addressable settings.");
                return;
            }

            CreateGroups(settings);
            AssignLevelDataAddresses(settings);
            AssetDatabase.SaveAssets();
            Debug.Log("[AddressablesInstaller] Addressables setup complete.");
        }

        private static void CreateGroups(AddressableAssetSettings settings)
        {
            // Level Data group — local, bundled
            EnsureGroup(settings, "LevelData", BundledAssetGroupSchema.SchemaTypeId,
                new Dictionary<string, object> { { "m_BuildPath", "Built In Data" }, { "m_LoadPath", "{UnityEngine.AddressableAssets.Addressables.RuntimePath}/" } });

            // Prefabs group — local, bundled
            EnsureGroup(settings, "Prefabs", BundledAssetGroupSchema.SchemaTypeId,
                new Dictionary<string, object> { { "m_BuildPath", "Built In Data" }, { "m_LoadPath", "{UnityEngine.AddressableAssets.Addressables.RuntimePath}/" } });

            // Remote group (for future use) — remote path
            var remoteGroup = EnsureGroup(settings, "RemoteContent", BundledAssetGroupSchema.SchemaTypeId,
                new Dictionary<string, object> { { "m_BuildPath", "RemoteBuildPath" }, { "m_LoadPath", "RemoteLoadPath" } });

            // Add content update restriction schema for remote
            var updateSchema = remoteGroup.AddSchema<ContentUpdateGroupSchema>();
            updateSchema.StaticContent = true;

            // Add player data schema to LevelData group (for PlayerPrefs)
            var playerDataGroup = settings.groups.Find(g => g.Name == "LevelData");
            if (playerDataGroup != null)
            {
                var playerSchema = playerDataGroup.AddSchema<PlayerDataGroupSchema>();
                playerSchema.DisableCatalogUpdateOnStartup = false;
            }
        }

        private static AddressableAssetGroup EnsureGroup(AddressableAssetSettings settings,
            string name, int schemaTypeGuid, Dictionary<string, object> schemaSettings = null)
        {
            var existing = settings.groups.Find(g => g.Name == name);
            if (existing != null) return existing;

            var group = settings.CreateGroup(name, false, false, true, null, typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
            return group;
        }

        private static void AssignLevelDataAddresses(AddressableAssetSettings settings)
        {
            // Find all LevelData ScriptableObject assets
            string[] guids = AssetDatabase.FindAssets("t:LevelData");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var levelData = AssetDatabase.LoadAssetAtPath<UnityEngine.ScriptableObject>(path);
                if (levelData == null) continue;

                // Try to read levelNumber via reflection (avoids compile dependency on Domain)
                var levelNumberProp = levelData.GetType().GetProperty("levelNumber") ?? levelData.GetType().GetField("levelNumber");
                if (levelNumberProp == null)
                {
                    Debug.LogWarning($"[AddressablesInstaller] No levelNumber field/property on {levelData.name}");
                    continue;
                }

                int levelNum = levelNumberProp is System.Reflection.PropertyInfo pi
                    ? (int)pi.GetValue(levelData)
                    : (int)((System.Reflection.FieldInfo)levelNumberProp).GetValue(levelData);

                string address = string.Format(LevelAddressFormat, levelNum);

                // Find or create entry
                var entry = settings.FindAssetEntry(guid);
                if (entry == null)
                {
                    var group = settings.DefaultGroup;
                    entry = settings.CreateOrMoveEntry(guid, group);
                }

                entry.address = address;

                // Add label
                if (!entry.labels.Contains(LevelDataLabel))
                    entry.SetLabel(LevelDataLabel, true);
            }

            Debug.Log($"[AddressablesInstaller] Assigned addresses to {guids.Length} LevelData assets.");
        }
    }
}
#else
using UnityEditor;

namespace PuzzleGame.Editor
{
    public static class AddressablesInstaller
    {
        [MenuItem("Tools/PuzzleGame/Setup Addressables")]
        public static void Setup()
        {
            if (UnityEngine.Application.isBatchMode)
            {
                UnityEngine.Debug.LogError("[AddressablesInstaller] Addressables package is not installed or 'ENABLE_ADDRESSABLES' is not defined.");
                EditorApplication.Exit(1);
            }
            else
            {
                EditorUtility.DisplayDialog(
                    "Addressables Missing",
                    "Addressables package is not installed or 'ENABLE_ADDRESSABLES' is not defined in Scripting Define Symbols.\n\n" +
                    "To fix: Install Addressables package via Package Manager, and define 'ENABLE_ADDRESSABLES' in Project Settings > Player > Scripting Define Symbols.",
                    "OK");
            }
        }
    }
}
#endif
