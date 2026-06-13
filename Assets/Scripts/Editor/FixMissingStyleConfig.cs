using UnityEditor;
using UnityEngine;
using PuzzleGame.Presentation.UI;

namespace PuzzleGame.Editor
{
    /// <summary>
    /// Automatically creates the missing UIStyleConfig asset to resolve the strict dependency exception.
    /// </summary>
    [InitializeOnLoad]
    public static class FixMissingStyleConfig
    {
        static FixMissingStyleConfig()
        {
            EditorApplication.delayCall += () =>
            {
                string[] guids = AssetDatabase.FindAssets("t:UIStyleConfig");
                if (guids.Length == 0)
                {
                    var config = ScriptableObject.CreateInstance<UIStyleConfig>();
                    
                    if (!System.IO.Directory.Exists("Assets/Resources"))
                    {
                        System.IO.Directory.CreateDirectory("Assets/Resources");
                        AssetDatabase.Refresh();
                    }
                        
                    AssetDatabase.CreateAsset(config, "Assets/Resources/UIStyleConfig.asset");
                    AssetDatabase.SaveAssets();
                    Debug.Log("[FixMissingStyleConfig] Auto-created default UIStyleConfig at Assets/Resources/UIStyleConfig.asset to satisfy strict dependency.");
                }
            };
        }
    }
}
