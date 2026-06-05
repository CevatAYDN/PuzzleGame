namespace PuzzleGame.Application.Interfaces
{
    /// <summary>
    /// Visual error feedback for failed pour operations.
    /// Shows red rim highlight on the offending mold(s) and a brief X indicator.
    /// </summary>
    public interface IErrorIndicatorService
    {
        /// <summary>
        /// Initializes the indicator service with active level parameters.
        /// </summary>
        void Initialize(Configuration.AnimationConfig animConfig, IMoldView[] moldViews);

        /// <summary>
        /// Flashes a red error indicator on the specified mold.
        /// </summary>
        /// <param name="moldIndex">Pool index of the mold to highlight.</param>
        /// <param name="reason">Stable reason code for debug display.</param>
        void ShowErrorOnMold(int moldIndex, string reason);

        /// <summary>Clears all active error indicators immediately.</summary>
        void ClearAllIndicators();
    }
}
