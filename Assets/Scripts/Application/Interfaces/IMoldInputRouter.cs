using PuzzleGame.Application.Configuration;

namespace PuzzleGame.Application.Interfaces
{
    /// <summary>
    /// Routes raw player input into gameplay actions (select, cast, deselect).
    /// Owns the input → selection → cast flow. Does not own mold lookup
    /// (<see cref="IMoldLookupCache"/>) or play-test defaults
    /// (<see cref="IInputHandlerDefaults"/>) — those are injected dependencies.
    /// </summary>
    public interface IMoldInputRouter
    {
        /// <summary>
        /// Called once per frame by the host MonoBehaviour. Reads pointer state,
        /// raycasts against the Mold layer, and dispatches to select/cast/deselect.
        /// No-op outside <c>GameState.Playing</c> / <c>OptionalCasting</c>.
        /// </summary>
        void ProcessInput();

        /// <summary>
        /// Sets the level data used for cast validation. Pass null to fall back
        /// to play-test defaults (<see cref="IInputHandlerDefaults.GetActiveLevelData"/>).
        /// </summary>
        void SetLevelData(LevelData levelData);

        /// <summary>
        /// Disposes the play-test default LevelData (if any). Call from
        /// MonoBehaviour.OnDestroy to prevent asset leaks in editor play mode.
        /// </summary>
        void DisposeDefaults();
    }
}
