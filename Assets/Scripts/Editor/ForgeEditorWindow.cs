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

        private Vector2 _sidebarScroll;

        private void OnGUI()
        {
            if (_tabs == null || _tabs.Count == 0) return;

            HandleKeyboardShortcuts();

            EditorGUILayout.BeginHorizontal();
            
            // Sidebar
            DrawSidebar();

            // Separator
            GUILayout.Box("", GUILayout.Width(1), GUILayout.ExpandHeight(true));

            // Content Area
            using (new EditorGUILayout.VerticalScope())
            {
                EditorGUILayout.Space(4);
                if (_activeTabIndex >= 0 && _activeTabIndex < _tabs.Count)
                {
                    _tabs[_activeTabIndex].OnGUI();
                }
            }

            EditorGUILayout.EndHorizontal();

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

        private void DrawSidebar()
        {
            float sidebarWidth = 180f;
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Width(sidebarWidth)))
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("Forge Dashboard", EditorStyles.boldLabel);
                EditorGUILayout.Space(8);

                _sidebarScroll = EditorGUILayout.BeginScrollView(_sidebarScroll);
                
                var groupedTabs = _tabs.GroupBy(t => t.Category).OrderBy(g => g.Key);
                
                foreach (var group in groupedTabs)
                {
                    EditorGUILayout.LabelField(group.Key.ToUpper(), EditorStyles.miniBoldLabel);
                    EditorGUILayout.Space(2);
                    
                    foreach (var tab in group)
                    {
                        int tabIndex = _tabs.IndexOf(tab);
                        bool isActive = _activeTabIndex == tabIndex;
                        
                        GUIStyle btnStyle = new GUIStyle(EditorStyles.toolbarButton);
                        btnStyle.alignment = TextAnchor.MiddleLeft;
                        btnStyle.fixedHeight = 24;
                        btnStyle.padding = new RectOffset(10, 0, 0, 0);

                        var oldColor = GUI.backgroundColor;
                        if (isActive)
                            GUI.backgroundColor = new Color(0.2f, 0.6f, 1f, 1f);
                        
                        if (GUILayout.Toggle(isActive, tab.TabName, btnStyle))
                        {
                            if (_activeTabIndex != tabIndex)
                            {
                                _activeTabIndex = tabIndex;
                                GUI.FocusControl(null);
                            }
                        }
                        
                        GUI.backgroundColor = oldColor;
                    }
                    EditorGUILayout.Space(10);
                }
                
                EditorGUILayout.EndScrollView();

                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Refresh Data", GUILayout.Height(30)))
                {
                    EditorApplication.delayCall += () =>
                    {
                        RefreshData();
                        SetStatus("Refreshed.", MessageType.Info);
                    };
                }
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
