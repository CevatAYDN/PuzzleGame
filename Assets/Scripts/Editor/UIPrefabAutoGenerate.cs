using UnityEditor;

namespace PuzzleGame.Editor
{
    /// <summary>
    /// Auto-generates UI prefabs on first Editor launch after code change.
    /// Uses a session-scoped EditorPref to run only once per session.
    /// </summary>
    [InitializeOnLoad]
    internal static class UIPrefabAutoGenerate
    {
        private const string PrefKey = "PuzzleGame_UIPrefabsGenerated_v2";

        static UIPrefabAutoGenerate()
        {
            bool alreadyGenerated = SessionState.GetBool(PrefKey, false);
            if (alreadyGenerated) return;

            EditorApplication.delayCall += () =>
            {
                bool generated = SessionState.GetBool(PrefKey, false);
                if (!generated)
                {
                    UIPrefabGenerator.GenerateAll();
                    SessionState.SetBool(PrefKey, true);
                }
            };
        }
    }
}

