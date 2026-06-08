namespace PuzzleGame.Domain.Models
{
    /// <summary>
    /// Types of power-ups the player can activate during gameplay.
    /// Each power-up consumes one charge from the player's inventory.
    /// </summary>
    public enum PowerUpType
    {
        /// <summary>Shuffles all layers across all molds randomly. Does not change mold count.</summary>
        Shuffle = 0,

        /// <summary>Adds one empty mold to the scene (up to a configurable max).</summary>
        ExtraMold = 1,

        /// <summary>Removes the top layer from the selected mold.</summary>
        UndoLayer = 2,

        /// <summary>Reveals the correct next move (highlights source + target).</summary>
        Hint = 3,

        /// <summary>Merges all layers of the same color in the selected mold into one layer.</summary>
        ColorBomb = 4
    }

    /// <summary>
    /// Immutable descriptor for a power-up type. Used by UI to display
    /// name, icon, and cost without coupling to the service layer.
    /// </summary>
    public readonly struct PowerUpDescriptor
    {
        public PowerUpType Type { get; }
        public string DisplayKey { get; }
        public int CoinCost { get; }
        public int MaxCharges { get; }

        public PowerUpDescriptor(PowerUpType type, string displayKey, int coinCost, int maxCharges)
        {
            Type = type;
            DisplayKey = displayKey;
            CoinCost = coinCost;
            MaxCharges = maxCharges;
        }
    }
}
