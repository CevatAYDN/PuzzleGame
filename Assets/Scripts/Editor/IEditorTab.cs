using UnityEditor;

namespace PuzzleGame.Editor
{
    /// <summary>
    /// Interface for modular editor plug-in tabs in ForgeEditorWindow.
    /// </summary>
    public interface IEditorTab
    {
        string TabName { get; }
        string Category { get; }
        void OnEnable(ForgeEditorWindow window);
        void OnDisable();
        void OnGUI();
        void OnSceneGUI(SceneView sceneView);

        /// <summary>
        /// Called by ForgeEditorWindow when a global refresh is requested (F5 / Ctrl+R / Refresh button).
        /// Tab'lar kendi cache'lerini temizlemek için override edebilir.
        /// Default implementasyon: no-op (mevcut tab'lar break olmaz).
        /// </summary>
        void Refresh() { }
    }
}
