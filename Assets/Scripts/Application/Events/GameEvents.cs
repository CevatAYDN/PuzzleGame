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

    public readonly struct PourCompletedEvent
    {
        public BottleState Source { get; }
        public BottleState Target { get; }

        public PourCompletedEvent(BottleState source, BottleState target)
        {
            Source = source;
            Target = target;
        }
    }

    /// <summary>
    /// Published when a pour attempt is rejected by validator or pour logic.
    /// Reason is a stable string code (e.g. "validator_rejected", "no_matching_layers").
    /// </summary>
    public readonly struct PourRejectedEvent
    {
        public int SourceBottleIndex { get; }
        public int TargetBottleIndex { get; }
        public string Reason { get; }

        public PourRejectedEvent(int sourceIndex, int targetIndex, string reason)
        {
            SourceBottleIndex = sourceIndex;
            TargetBottleIndex = targetIndex;
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

    // S15 FIX: UndoPrePourEvent kaldırıldı.
    // Döküm öncesi undo snapshot kaydı artık InputHandlerService callback üzerinden
    // doğrudan GameHistoryManagementService'e yapılıyor (decoupled, type-safe).
}