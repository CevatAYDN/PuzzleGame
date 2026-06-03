using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using PuzzleGame.Domain.Models;
using PuzzleGame.Application.Configuration;
using PuzzleGame.Application.Configuration.FeatureSystem;
using PuzzleGame.Domain.Services;
using PuzzleGame.Infrastructure;
using PuzzleGame.Application.Services;

namespace PuzzleGame.Editor
{
    public partial class PuzzleGameEditorWindow
    {
        // ── Data tab ────────────────────────────────────────────────────────
        private bool _overrideExisting = false;
        private Dictionary<string, bool> _dataPresence = new Dictionary<string, bool>();
        private Vector2 _dataScroll;

        // ── DATA TAB ────────────────────────────────────────────────────────

        private void DrawDataTab()
        {
            EditorGUILayout.LabelField("ScriptableObject Asset Management", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            EditorGUILayout.HelpBox(
                "Asset'ler " + DataAssetCreator.DataPath + " altında oluşturulur.\n" +
                "Aktif değerlerin üzerine yazılsın mı?",
                MessageType.None);

            EditorGUI.BeginChangeCheck();
            _overrideExisting = EditorGUILayout.ToggleLeft("Reset existing assets on update", _overrideExisting);
            if (EditorGUI.EndChangeCheck()) Repaint();

            _dataScroll = EditorGUILayout.BeginScrollView(_dataScroll);
            EditorGUILayout.LabelField("Asset Durumu", EditorStyles.miniBoldLabel);
            foreach (var kvp in _dataPresence.OrderBy(x => x.Key))
            {
                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    var icon = kvp.Value ? "✓" : "✗";
                    var color = kvp.Value ? Color.green : Color.gray;
                    GUILayout.Label(icon, GUILayout.Width(20));
                    GUI.contentColor = color;
                    GUILayout.Label(kvp.Key + ".asset", GUILayout.MinWidth(160));
                    GUI.contentColor = Color.white;
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

        // ── PLAYER SAVE SECTION ───────────────────────────────────────────────

        private void DrawPlayerSaveSection()
        {
            EditorGUILayout.LabelField("Player Save Data", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            var exists = Application.Services.GameSaveManager.HasSaveData;
            var size = Application.Services.GameSaveManager.FileSizeBytes;
            var integ = exists ? Application.Services.GameSaveManager.VerifyIntegrity() : false;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Status", EditorStyles.miniBoldLabel);
                EditorGUILayout.LabelField("File", Application.Services.GameSaveManager.SaveFilePath);

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label("Exists:  ", GUILayout.Width(60));
                    GUI.contentColor = exists ? Color.green : Color.gray;
                    GUILayout.Label(exists ? "✓" : "✗", GUILayout.Width(20));
                    GUI.contentColor = Color.white;
                    GUILayout.FlexibleSpace();

                    if (exists)
                    {
                        GUILayout.Label("Integrity:", GUILayout.Width(65));
                        GUI.contentColor = integ ? Color.green : Color.red;
                        GUILayout.Label(integ ? "OK" : "TAMPERED", GUILayout.Width(80));
                        GUI.contentColor = Color.white;
                        GUILayout.FlexibleSpace();

                        GUILayout.Label((size / 1024.0).ToString("F1") + " KB", GUILayout.Width(60));
                    }
                }

                if (exists && integ)
                {
                    var data = Application.Services.GameSaveManager.PeekVerified();
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
            if (!Application.Services.GameSaveManager.HasSaveData)
            {
                SetStatus("No save file to reveal.", MessageType.Warning);
                return;
            }
            EditorUtility.RevealInFinder(Application.Services.GameSaveManager.SaveFilePath);
            SetStatus("Save file revealed.", MessageType.Info);
        }

        private void VerifySaveFile()
        {
            if (!Application.Services.GameSaveManager.HasSaveData)
            {
                SetStatus("No save file to verify.", MessageType.Warning);
                return;
            }

            bool ok = Application.Services.GameSaveManager.VerifyIntegrity();
            SetStatus(ok
                ? "Save integrity: OK (HMAC-SHA256 matches)."
                : "Save integrity: FAILED — file tampered or corrupted!",
                ok ? MessageType.Info : MessageType.Error);
        }

        private void DeleteSaveFile()
        {
            if (!Application.Services.GameSaveManager.HasSaveData)
            {
                SetStatus("No save data to delete.", MessageType.Info);
                return;
            }

            if (!EditorUtility.DisplayDialog(
                "Delete all save data?",
                "Bu işlem geri alınamaz. Oyuncunun tüm kayıtlı level ilerlemesi silinecek.",
                "Delete", "Cancel"))
            {
                return;
            }

            Application.Services.GameSaveManager.DeleteAll();
            SetStatus("Save data deleted.", MessageType.Info);
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
                SetStatus($"{fileName}.asset not found.", MessageType.Warning);
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
            SetStatus($"Reset {fileName}.asset to defaults.", MessageType.Info);
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

                SetStatus($"Created: {created}, Updated: {updated}, Skipped: {skipped}", MessageType.Info);
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

        private void RefreshDataPresence()
        {
            _dataPresence = DataAssetCreator.CheckAllExist();
            RefreshLevelList();
        }
    }
}
