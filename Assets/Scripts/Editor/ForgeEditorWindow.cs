using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using PuzzleGame.Application.Configuration;

namespace PuzzleGame.Editor
{
    /// <summary>
    /// PuzzleGame Editor Tools — Modular, Plug-in based Editor Window.
    /// Manages dynamically loaded IEditorTab instances.
    /// </summary>
    public partial class ForgeEditorWindow : EditorWindow
    {
        private List<IEditorTab> _tabs;
        private int _activeTabIndex = 0;

        private string _statusMessage = "Ready.";
        private MessageType _statusType = MessageType.Info;

        [MenuItem("Tools/PuzzleGame/Open Editor")]
        public static void Open()
        {
            var window = GetWindow<ForgeEditorWindow>("PuzzleGame Editor");
            window.minSize = new Vector2(420, 360);
            window.RefreshData();
        }

        private void OnEnable()
        {
            // Initialize modular tabs
            _tabs = new List<IEditorTab>
            {
                new DataTab(),
                new LevelsTab(),
                new SceneTab(),
                new ValidateTab(),
                new PaletteTab(),
                new FeaturesTab(),
                new LevelUITab(),
                new TestTab(),
                new LocalizationTab(),
                new PouringLabTab()
            };

            foreach (var tab in _tabs)
            {
                tab.OnEnable(this);
            }

            SceneView.duringSceneGui += OnSceneGUIInternal;
            RefreshData();
        }

        private void OnDisable()
        {
            // SceneView delegate'i en başta kaldır — tab exception'larından etkilenmez
            SceneView.duringSceneGui -= OnSceneGUIInternal;

            if (_tabs != null)
            {
                foreach (var tab in _tabs)
                {
                    try
                    {
                        tab.OnDisable();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[ForgeEditorWindow] Tab {tab.GetType().Name}.OnDisable() failed: {ex.Message}");
                    }
                }
            }
        }

        private void OnGUI()
        {
            if (_tabs == null || _tabs.Count == 0) return;

            DrawTabs();
            EditorGUILayout.Space(6);

            if (_activeTabIndex >= 0 && _activeTabIndex < _tabs.Count)
            {
                _tabs[_activeTabIndex].OnGUI();
            }

            DrawStatusBar();
        }

        private void OnSceneGUIInternal(SceneView sceneView)
        {
            if (_tabs != null && _activeTabIndex >= 0 && _activeTabIndex < _tabs.Count)
            {
                _tabs[_activeTabIndex].OnSceneGUI(sceneView);
            }
        }

        private void DrawTabs()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            for (int i = 0; i < _tabs.Count; i++)
            {
                bool active = _activeTabIndex == i;
                if (GUILayout.Toggle(active, _tabs[i].TabName, EditorStyles.toolbarButton))
                {
                    _activeTabIndex = i;
                }
            }
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70)))
            {
                EditorApplication.delayCall += () =>
                {
                    RefreshData();
                    SetStatus("Refreshed.", MessageType.Info);
                };
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawStatusBar()
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.HelpBox(_statusMessage, _statusType);
        }

        public void SetStatus(string message, MessageType type)
        {
            _statusMessage = message;
            _statusType = type;
            Repaint();
        }

        public void RefreshData()
        {
            if (_tabs == null) return;
            var dataTab = _tabs.OfType<DataTab>().FirstOrDefault();
            if (dataTab != null)
            {
                dataTab.RefreshDataPresence();
            }
        }

        public void RefreshLevelList()
        {
            if (_tabs == null) return;
            var levelsTab = _tabs.OfType<LevelsTab>().FirstOrDefault();
            if (levelsTab != null)
            {
                levelsTab.RefreshLevelList();
            }
        }

        public void LoadLevelIntoScene(LevelData level)
        {
            if (_tabs == null) return;
            var levelsTab = _tabs.OfType<LevelsTab>().FirstOrDefault();
            if (levelsTab != null)
            {
                levelsTab.LoadLevelIntoScene(level);
            }
        }
    }
}
