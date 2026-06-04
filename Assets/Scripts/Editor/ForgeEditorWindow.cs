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
    /// <summary>
    /// PuzzleGame için tüm editor araçları — tek pencerede 8 sekme:
    ///   - Data: ScriptableObject asset yönetimi
    ///   - Levels: Batch level creation ve yönetimi
    ///   - Scene: Sahne oluşturma kontrolü
    ///   - Validate: Proje sağlık kontrolleri
    ///   - Palette: Color palette ve level property editor
    ///   - Features: Multi-layer Cast & Reaction system yönetimi (YENİ)
    ///   - Level UI: Pre-built level designer (YENİ)
    ///   - Test: Analytics & cheat tools (YENİ)
    /// Split into partial classes: one file per tab + solver helpers.
    /// </summary>
    public partial class ForgeEditorWindow : EditorWindow
    {
        private enum Tab { Data, Levels, Scene, Validate, Palette, Features, LevelUI, Test, Localization }
        private Tab _activeTab = Tab.Data;

        // ── Status bar ──────────────────────────────────────────────────────
        private string _statusMessage = "Ready.";
        private MessageType _statusType = MessageType.Info;

        [MenuItem("Tools/PuzzleGame/Open Editor")]
        public static void Open()
        {
            var window = GetWindow<ForgeEditorWindow>("PuzzleGame Editor");
            window.minSize = new Vector2(420, 360);
            window.RefreshDataPresence();
        }

        private void OnEnable()
        {
            RefreshDataPresence();
        }

        private void OnGUI()
        {
            DrawTabs();
            EditorGUILayout.Space(6);
            switch (_activeTab)
            {
                case Tab.Data:       DrawDataTab();         break;
                case Tab.Levels:     DrawLevelsTab();       break;
                case Tab.Scene:      DrawSceneTab();        break;
                case Tab.Validate:   DrawValidateTab();     break;
                case Tab.Palette:    DrawPaletteTab();      break;
                case Tab.Features:   DrawFeaturesTab();    break;
                case Tab.LevelUI:    DrawLevelUITab();      break;
                case Tab.Test:       DrawTestTab();        break;
                case Tab.Localization: DrawLocalizationTab(); break;
            }
            DrawStatusBar();
        }

        // ── Tab bar ─────────────────────────────────────────────────────────

        private void DrawTabs()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Toggle(_activeTab == Tab.Data, "Data", EditorStyles.toolbarButton)) _activeTab = Tab.Data;
            if (GUILayout.Toggle(_activeTab == Tab.Levels, "Levels", EditorStyles.toolbarButton)) _activeTab = Tab.Levels;
            if (GUILayout.Toggle(_activeTab == Tab.Scene, "Scene", EditorStyles.toolbarButton)) _activeTab = Tab.Scene;
            if (GUILayout.Toggle(_activeTab == Tab.Validate, "Validate", EditorStyles.toolbarButton)) _activeTab = Tab.Validate;
            if (GUILayout.Toggle(_activeTab == Tab.Palette, "Palette", EditorStyles.toolbarButton)) _activeTab = Tab.Palette;
            if (GUILayout.Toggle(_activeTab == Tab.Features, "Features", EditorStyles.toolbarButton)) _activeTab = Tab.Features;
            if (GUILayout.Toggle(_activeTab == Tab.LevelUI, "Level UI", EditorStyles.toolbarButton)) _activeTab = Tab.LevelUI;
            if (GUILayout.Toggle(_activeTab == Tab.Test, "Test", EditorStyles.toolbarButton)) _activeTab = Tab.Test;
            if (GUILayout.Toggle(_activeTab == Tab.Localization, "i18n", EditorStyles.toolbarButton)) _activeTab = Tab.Localization;
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70)))
            {
                EditorApplication.delayCall += () =>
                {
                    RefreshDataPresence();
                    SetStatus("Refreshed.", MessageType.Info);
                };
            }
            EditorGUILayout.EndHorizontal();
        }

        // ── Status bar ──────────────────────────────────────────────────────

        private void DrawStatusBar()
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.HelpBox(_statusMessage, _statusType);
        }

        private void SetStatus(string message, MessageType type)
        {
            _statusMessage = message;
            _statusType = type;
            Repaint();
        }
    }
}
