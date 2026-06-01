using PuzzleGame.Domain.Models;

namespace PuzzleGame.Events
{
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
}