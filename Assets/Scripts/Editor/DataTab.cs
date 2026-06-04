using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace PuzzleGame.Editor
{
    public class DataTab : IEditorTab
    {
        public string TabName => "Data";
        private ForgeEditorWindow _window;

        private bool _overrideExisting = false;
        private Dictionary<string, bool> _dataPresence = new Dictionary<string, bool>();
        private Vector2 _dataScroll;

        public void OnEnable(ForgeEditorWindow window)
        {
            _window = window;
        }

        public void OnDisable()
        {
        }

        public void OnGUI()
        {
            EditorGUILayout.LabelField("ScriptableObject Asset Management", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            EditorGUILayout.HelpBox(
                "Assets are created under " + DataAssetCreator.DataPath + ".\n" +
                "Overwrite active values?",
                MessageType.None);

            EditorGUI.BeginChangeCheck();
            _overrideExisting = EditorGUILayout.ToggleLeft("Reset existing assets on update", _overrideExisting);
            if (EditorGUI.EndChangeCheck()) _window.Repaint();

            _dataScroll = EditorGUILayout.BeginScrollView(_dataScroll);
            EditorGUILayout.LabelField("Asset Status", EditorStyles.miniBoldLabel);
            foreach (var kvp in _dataPresence.OrderBy(x => x.Key))
            {
                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    var badgeColor = kvp.Value ? new Color(0.12f, 0.75f, 0.12f, 1f) : new Color(0.6f, 0.6f, 0.6f, 1f);
                    var badgeText = kvp.Value ? "● FOUND" : "● MISSING";

                    var oldColor = GUI.contentColor;
                    GUI.contentColor = badgeColor;
                    GUILayout.Label(badgeText, EditorStyles.boldLabel, GUILayout.Width(75));
                    GUI.contentColor = oldColor;

                    EditorGUILayout.LabelField(kvp.Key + ".asset", EditorStyles.miniLabel, GUILayout.MinWidth(160));
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Ping", GUILayout.Width(50)))
                    {
                        string key = kvp.Key;
                        EditorApplication.delayCall += () => PingAsset(key);
                    }
                    if (GUILayout.Button("Reset", GUILayout.Width(50)))
                    {
                        string key = kvp.Key;
                        EditorApplication.delayCall += () => ResetSingle(key);
                    }
                }
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(8);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Create / Update All", GUILayout.Height(28)))
                    EditorApplication.delayCall += CreateAllData;
                if (GUILayout.Button("Select Folder", GUILayout.Height(28), GUILayout.MaxWidth(140)))
                    EditorApplication.delayCall += SelectDataFolder;
            }

            EditorGUILayout.Space(12);
            DrawPlayerSaveSection();
        }

        public void OnSceneGUI(SceneView sceneView)
        {
        }

        private void DrawPlayerSaveSection()
        {
            EditorGUILayout.LabelField("Player Save Data", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            var exists = Application.Services.GameSaveManager.EditorInstance.HasSaveData;
            var size = Application.Services.GameSaveManager.EditorInstance.FileSizeBytes;
            var integ = exists ? Application.Services.GameSaveManager.EditorInstance.VerifyIntegrity() : false;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Status", EditorStyles.miniBoldLabel);
                EditorGUILayout.LabelField("File", Application.Services.GameSaveManager.EditorInstance.SaveFilePath);

                using (new EditorGUILayout.HorizontalScope())
                {
                    var oldColor = GUI.contentColor;

                    GUILayout.Label("File Status:", GUILayout.Width(80));
                    GUI.contentColor = exists ? new Color(0.12f, 0.75f, 0.12f, 1f) : new Color(0.6f, 0.6f, 0.6f, 1f);
                    GUILayout.Label(exists ? "● DETECTED" : "● EMPTY", EditorStyles.boldLabel, GUILayout.Width(100));
                    GUI.contentColor = oldColor;

                    GUILayout.FlexibleSpace();

                    if (exists)
                    {
                        GUILayout.Label("Security Integrity:", GUILayout.Width(110));
                        GUI.contentColor = integ ? new Color(0.12f, 0.75f, 0.12f, 1f) : new Color(0.85f, 0.2f, 0.2f, 1f);
                        GUILayout.Label(integ ? "● OK" : "● COMPROMISED", EditorStyles.boldLabel, GUILayout.Width(110));
                        GUI.contentColor = oldColor;

                        GUILayout.FlexibleSpace();
                        GUILayout.Label((size / 1024.0).ToString("F1") + " KB", EditorStyles.miniLabel, GUILayout.Width(60));
                    }
                }

                if (exists && integ)
                {
                    var data = Application.Services.GameSaveManager.EditorInstance.PeekVerified();
                    EditorGUILayout.LabelField("Last Played Level", data.lastPlayedLevel.ToString());
                    EditorGUILayout.LabelField("Completed Levels",
                        data.levels.FindAll(l => l.isCompleted).Count + "/" + data.levels.Count);
                }

                EditorGUILayout.Space(4);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Reveal in Explorer", GUILayout.Height(22)))
                        EditorApplication.delayCall += RevealSaveFile;
                    if (GUILayout.Button("Verify Integrity", GUILayout.Height(22)))
                        EditorApplication.delayCall += VerifySaveFile;
                    GUI.backgroundColor = new Color(0.9f, 0.5f, 0.5f);
                    if (GUILayout.Button("Delete Save", GUILayout.Height(22)))
                        EditorApplication.delayCall += DeleteSaveFile;
                    GUI.backgroundColor = Color.white;
                }
            }
        }

        private void RevealSaveFile()
        {
            if (!Application.Services.GameSaveManager.EditorInstance.HasSaveData)
            {
                _window.SetStatus("No save file to reveal.", MessageType.Warning);
                return;
            }
            EditorUtility.RevealInFinder(Application.Services.GameSaveManager.EditorInstance.SaveFilePath);
            _window.SetStatus("Save file revealed.", MessageType.Info);
        }

        private void VerifySaveFile()
        {
            if (!Application.Services.GameSaveManager.EditorInstance.HasSaveData)
            {
                _window.SetStatus("No save file to verify.", MessageType.Warning);
                return;
            }

            bool ok = Application.Services.GameSaveManager.EditorInstance.VerifyIntegrity();
            _window.SetStatus(ok
                ? "Save integrity: OK (HMAC-SHA256 matches)."
                : "Save integrity: FAILED — file tampered or corrupted!",
                ok ? MessageType.Info : MessageType.Error);
        }

        private void DeleteSaveFile()
        {
            if (!Application.Services.GameSaveManager.EditorInstance.HasSaveData)
            {
                _window.SetStatus("No save data to delete.", MessageType.Info);
                return;
            }

            if (!EditorUtility.DisplayDialog(
                "Delete all save data?",
                "This action cannot be undone. All player level progress will be deleted.",
                "Delete", "Cancel"))
            {
                return;
            }

            Application.Services.GameSaveManager.EditorInstance.DeleteAll();
            _window.SetStatus("Save data deleted.", MessageType.Info);
        }

        private void PingAsset(string fileName)
        {
            var path = $"{DataAssetCreator.DataPath}/{fileName}.asset";
            var obj = AssetDatabase.LoadMainAssetAtPath(path);
            if (obj != null)
            {
                EditorGUIUtility.PingObject(obj);
                Selection.activeObject = obj;
            }
            else
            {
                _window.SetStatus($"{fileName}.asset not found.", MessageType.Warning);
            }
        }

        private void ResetSingle(string fileName)
        {
            if (!EditorUtility.DisplayDialog(
                "Reset asset",
                $"{fileName}.asset will be overwritten with default values. Continue?",
                "Reset", "Cancel"))
            {
                return;
            }
            var results = DataAssetCreator.CreateAllDefaults(_ => true);
            _window.SetStatus($"Reset {fileName}.asset to defaults.", MessageType.Info);
            RefreshDataPresence();
        }

        private void CreateAllData()
        {
            int toCreate = 0, toOverwrite = 0;
            foreach (var kvp in _dataPresence)
            {
                if (kvp.Value) toOverwrite++;
                else toCreate++;
            }

            string summary = $"{toCreate} new, {toOverwrite} to overwrite.";
            if (_overrideExisting && toOverwrite > 0)
            {
                if (!EditorUtility.DisplayDialog(
                    "Confirm overwrite",
                    "Existing assets will be overwritten with default values.\n\n" + summary + "\n\nProceed?",
                    "Create / Update", "Cancel"))
                {
                    return;
                }
            }

            try
            {
                EditorUtility.DisplayProgressBar("PuzzleGame Data", "Creating assets...", 0.5f);
                var results = DataAssetCreator.CreateAllDefaults(name =>
                    _overrideExisting || !_dataPresence.TryGetValue(name, out var exists) || !exists);

                int created = results.Count(r => r.created);
                int updated = results.Count(r => r.overwritten);
                int skipped = results.Count(r => !r.created && !r.overwritten);

                _window.SetStatus($"Created: {created}, Updated: {updated}, Skipped: {skipped}", MessageType.Info);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                RefreshDataPresence();
            }
        }

        private void SelectDataFolder()
        {
            if (!System.IO.Directory.Exists(DataAssetCreator.DataPath))
            {
                System.IO.Directory.CreateDirectory(DataAssetCreator.DataPath);
                AssetDatabase.Refresh();
            }
            var obj = AssetDatabase.LoadAssetAtPath(DataAssetCreator.DataPath, typeof(UnityEngine.Object));
            if (obj != null) EditorGUIUtility.PingObject(obj);
        }

        public void RefreshDataPresence()
        {
            _dataPresence = DataAssetCreator.CheckAllExist();
            _window.RefreshLevelList();
        }
    }
}
