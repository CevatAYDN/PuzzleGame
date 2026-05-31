using BottleShaders.Domain.Models;

namespace BottleShaders.Events
{
    // ── Pour ────────────────────────────────────────────────────────────────

    /// <summary>Published after a successful pour between two bottles.</summary>
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

    // ── Selection ────────────────────────────────────────────────────────────

    /// <summary>Published when the player selects a bottle.</summary>
    public readonly struct BottleSelectedEvent
    {
        public BottleState Bottle { get; }
        public BottleSelectedEvent(BottleState bottle) => Bottle = bottle;
    }

    /// <summary>Published when the current selection is cleared.</summary>
    public readonly struct BottleDeselectedEvent
    {
        public BottleState Bottle { get; }
        public BottleDeselectedEvent(BottleState bottle) => Bottle = bottle;
    }

    // ── Game state ───────────────────────────────────────────────────────────

    /// <summary>Published when the player completes the level.</summary>
    public readonly struct LevelCompletedEvent
    {
        public int MoveCount { get; }
        public LevelCompletedEvent(int moveCount) => MoveCount = moveCount;
    }
}
