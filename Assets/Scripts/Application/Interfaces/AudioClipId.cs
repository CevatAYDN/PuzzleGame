namespace PuzzleGame.Application.Interfaces
{
    /// <summary>
    /// Enum-mapped audio clips. Add new IDs here, map clips in AudioService.
    /// </summary>
    public enum AudioClipId
    {
        None,
        PourLoop,
        PourEnd,
        Error,
        LevelComplete,
        LevelStart,
        UiClick,
        CorkPop
    }
}
