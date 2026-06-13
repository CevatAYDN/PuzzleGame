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
            // 11 sekme iki satıra bölündüğü için genişletildi
            window.minSize = new Vector2(520, 400);
            window.RefreshData();
        }

        private void OnEnable()
        {
            // Initialize modular tabs dynamically using reflection to adhere to Open-Closed Principle
            _tabs = new List<IEditorTab>();
            var tabTypes = UnityEditor.TypeCache.GetTypesDerivedFrom<IEditorTab>();
            foreach (var type in tabTypes)
            {
                if (!type.IsAbstract && !type.IsInterface)
                {
                    var tab = Activator.CreateInstance(type) as IEditorTab;
                    if (tab != null)
                    {
                        _tabs.Add(tab);
                        tab.OnEnable(this);
                    }
                }
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

            HandleKeyboardShortcuts();

            DrawTabs();
            EditorGUILayout.Space(6);

            if (_activeTabIndex >= 0 && _activeTabIndex < _tabs.Count)
            {
                _tabs[_activeTabIndex].OnGUI();
            }

            DrawStatusBar();
        }

        private void HandleKeyboardShortcuts()
        {
            var e = Event.current;
            if (e == null || e.type != EventType.KeyDown) return;

            bool ctrl = e.control || e.command;

            if (ctrl && e.keyCode == KeyCode.Tab)
            {
                e.Use();
                int dir = e.shift ? -1 : 1;
                _activeTabIndex = (_activeTabIndex + dir + _tabs.Count) % _tabs.Count;
                Repaint();
                return;
            }

            if (e.keyCode == KeyCode.F5)
            {
                e.Use();
                RefreshData();
                SetStatus("Refreshed (F5).", MessageType.Info);
                return;
            }

            if (ctrl && e.keyCode == KeyCode.R)
            {
                e.Use();
                RefreshData();
                SetStatus("Refreshed (Ctrl+R).", MessageType.Info);
            }
        }

        private void OnSceneGUIInternal(SceneView sceneView)
        {
            if (_tabs != null && _activeTabIndex >= 0 && _activeTabIndex < _tabs.Count)
            {
                _tabs[_activeTabIndex].OnSceneGUI(sceneView);
            }
        }

        /// <summary>
        /// Tab bar çizimi.
        /// 6'dan fazla sekme varsa iki satıra bölünür — overflow önlenir.
        /// </summary>
        private void DrawTabs()
        {
            const int MaxPerRow = 6;
            int total = _tabs.Count;
            int rows  = (total + MaxPerRow - 1) / MaxPerRow;

            for (int row = 0; row < rows; row++)
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

                int start = row * MaxPerRow;
                int end   = Mathf.Min(start + MaxPerRow, total);

                for (int i = start; i < end; i++)
                {
                    bool active = _activeTabIndex == i;
                    if (GUILayout.Toggle(active, _tabs[i].TabName, EditorStyles.toolbarButton))
                        _activeTabIndex = i;
                }

                GUILayout.FlexibleSpace();

                // Refresh butonu sadece son satırda görünsün
                if (row == rows - 1)
                {
                    if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70)))
                    {
                        EditorApplication.delayCall += () =>
                        {
                            RefreshData();
                            SetStatus("Refreshed.", MessageType.Info);
                        };
                    }
                }

                EditorGUILayout.EndHorizontal();
            }
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
            // Tüm tab'ların kendi Refresh() hook'unu çağır — tip bağımlılığı yok.
            foreach (var tab in _tabs)
            {
                try { tab.Refresh(); }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[ForgeEditorWindow] Tab {tab.GetType().Name}.Refresh() failed: {ex.Message}");
                }
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
