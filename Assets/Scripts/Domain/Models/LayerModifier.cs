namespace PuzzleGame.Domain.Models
{
    /// <summary>
    /// Optional modifier for an ore layer that changes its behavior during gameplay.
    /// </summary>
    public enum LayerModifier
    {
        /// <summary>Regular layer with no special behavior.</summary>
        None = 0,

        /// <summary>
        /// Frozen layers cannot be poured out (cast from).
        /// They thaw when a matching color is poured on top.
        /// </summary>
        Frozen = 1,
    }
}
