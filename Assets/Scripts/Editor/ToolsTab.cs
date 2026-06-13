using UnityEditor;
using UnityEngine;

namespace PuzzleGame.Editor
{
    public class ToolsTab : IEditorTab
    {
        public string TabName => "Tools";
        public string Category => "Systems & QA";
        private ForgeEditorWindow _window;

        private Vector2 _scrollPos;

        public void OnEnable(ForgeEditorWindow window)
        {
            _window = window;
        }

        public void OnDisable()
        {
        }

        public void Refresh()
        {
        }

        public void OnGUI()
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            
            EditorGUILayout.LabelField("Development Utilities", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("UI Prefab Generation", EditorStyles.miniBoldLabel);
                EditorGUILayout.HelpBox("Generates all UI prefabs and sets up Canvas in the UITest scene.", MessageType.Info);
                
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Generate UI Prefabs", GUILayout.Height(28)))
                    {
                        EditorApplication.delayCall += UIPrefabGenerator.GenerateAll;
                    }
                    if (GUILayout.Button("Generate UI (ColorBlind)", GUILayout.Height(28)))
                    {
                        EditorApplication.delayCall += UIPrefabGenerator.GenerateAllColorBlind;
                    }
                }
            }

            EditorGUILayout.Space(8);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Scene & Asset Tools", EditorStyles.miniBoldLabel);
                
                if (GUILayout.Button("Fix URP Pipeline Settings", GUILayout.Height(28)))
                {
                    EditorApplication.delayCall += SceneBuilder.FixURPPipeline;
                }
                
                if (GUILayout.Button("Generate Missing Particles", GUILayout.Height(28)))
                {
                    EditorApplication.delayCall += ParticlePrefabGenerator.GenerateParticles;
                }
                
                if (GUILayout.Button("Setup Addressables", GUILayout.Height(28)))
                {
                    EditorApplication.delayCall += AddressablesInstaller.SetupAddressables;
                }
            }

            EditorGUILayout.EndScrollView();
        }

        public void OnSceneGUI(SceneView sceneView)
        {
        }
    }
}
