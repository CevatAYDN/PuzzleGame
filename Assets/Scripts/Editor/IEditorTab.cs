using UnityEditor;

namespace PuzzleGame.Editor
{
    /// <summary>
    /// Interface for modular editor plug-in tabs in ForgeEditorWindow.
    /// </summary>
    public interface IEditorTab
    {
        string TabName { get; }
        void OnEnable(ForgeEditorWindow window);
        void OnDisable();
        void OnGUI();
        void OnSceneGUI(SceneView sceneView);
    }
}
