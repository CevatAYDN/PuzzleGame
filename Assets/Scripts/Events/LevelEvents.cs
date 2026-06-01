namespace PuzzleGame.Events
{
    /// <summary>Published when player selects a level from the level select UI.</summary>
    public readonly struct LevelSelectedEvent
    {
        public int LevelNumber { get; }

        public LevelSelectedEvent(int levelNumber) => LevelNumber = levelNumber;
    }

    /// <summary>Published when a level's progress is recorded or updated.</summary>
    public readonly struct LevelProgressChangedEvent
    {
        public int LevelNumber { get; }
        public int Stars { get; }
        public int Moves { get; }

        public LevelProgressChangedEvent(int levelNumber, int stars, int moves)
        {
            LevelNumber = levelNumber;
            Stars = stars;
            Moves = moves;
        }
    }
}
