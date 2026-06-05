using PuzzleGame.Application.Configuration;

namespace PuzzleGame.Application.Interfaces
{
    /// <summary>
    /// Provides a transient default <see cref="LevelData"/> for play-test mode
    /// (no level set, no asset). Owns the <c>ScriptableObject</c> lifecycle —
    /// creates on first use, destroys via <see cref="Dispose"/> to prevent
    /// editor asset leaks.
    ///
    /// <para>
    /// Zero dependencies. Implementation lives in Infrastructure layer
    /// (UnityEngine.Object access).
    /// </para>
    /// </summary>
    public interface IInputHandlerDefaults
    {
        /// <summary>
        /// Returns <paramref name="currentLevelData"/> if non-null, otherwise
        /// creates (once) and returns a play-test default. Caller should NOT
        /// cache the result — every call returns the same default instance
        /// until <see cref="Dispose"/> is called.
        /// </summary>
        LevelData GetActiveLevelData(LevelData currentLevelData);

        /// <summary>Destroys the cached play-test default LevelData (if any). Idempotent.</summary>
        void Dispose();
    }
}
