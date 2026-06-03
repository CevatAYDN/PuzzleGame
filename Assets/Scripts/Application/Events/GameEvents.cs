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
    /// Published immediately before any pour (single or multi) begins.
    /// Emitted once per user-initiated pour attempt, regardless of outcome.
    /// </summary>
    public readonly struct PourStartedEvent
    {
        public int SourceBottleIndex { get; }
        public int TargetBottleIndex { get; }
        public bool IsMultiLayer { get; }

        public PourStartedEvent(int sourceIndex, int targetIndex, bool isMultiLayer)
        {
            SourceBottleIndex = sourceIndex;
            TargetBottleIndex = targetIndex;
            IsMultiLayer = isMultiLayer;
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

    public readonly struct BottleSelectedEvent
    {
        public BottleState Bottle { get; }

        public BottleSelectedEvent(BottleState bottle)
        {
            Bottle = bottle;
        }
    }

    public readonly struct BottleDeselectedEvent
    {
        public BottleState Bottle { get; }

        public BottleDeselectedEvent(BottleState bottle)
        {
            Bottle = bottle;
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