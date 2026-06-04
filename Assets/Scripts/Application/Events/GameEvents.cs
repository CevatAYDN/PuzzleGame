using PuzzleGame.Domain.Models;

namespace PuzzleGame.Application.Events
{
    public readonly struct GameStateChangedEvent
    {
        public GameState Previous { get; }
        public GameState Current { get; }

        public GameStateChangedEvent(GameState previous, GameState current)
        {
            Previous = previous;
            Current = current;
        }
    }

    public readonly struct CastCompletedEvent
    {
        public MoldState Source { get; }
        public MoldState Target { get; }

        public CastCompletedEvent(MoldState source, MoldState target)
        {
            Source = source;
            Target = target;
        }
    }

    /// <summary>
    /// Published when a Cast attempt is rejected by validator or Cast logic.
    /// Reason is a stable string code (e.g. "validator_rejected", "no_matching_layers").
    /// </summary>
    public readonly struct CastRejectedEvent
    {
        public int SourceMoldIndex { get; }
        public int TargetMoldIndex { get; }
        public string Reason { get; }

        public CastRejectedEvent(int sourceIndex, int targetIndex, string reason)
        {
            SourceMoldIndex = sourceIndex;
            TargetMoldIndex = targetIndex;
            Reason = reason;
        }
    }

    public readonly struct LevelCompletedEvent
    {
        public int MoveCount { get; }

        public LevelCompletedEvent(int moveCount)
        {
            MoveCount = moveCount;
        }
    }

    // S15 FIX: UndoPreCastEvent kaldırıldı.
    // Döküm öncesi undo snapshot kaydı artık InputHandlerService callback üzerinden
    // doğrudan GameHistoryManagementService'e yapılıyor (decoupled, type-safe).
}