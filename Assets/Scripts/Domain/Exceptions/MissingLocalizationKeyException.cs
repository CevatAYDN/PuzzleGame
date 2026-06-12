using System;

namespace PuzzleGame.Domain.Exceptions
{
    /// <summary>
    /// Thrown when a localization key cannot be found in the current translation data.
    /// Enforces the "Zero Hardcoded Strings" and "Fail Fast" rules.
    /// </summary>
    public class MissingLocalizationKeyException : Exception
    {
        public MissingLocalizationKeyException(string message) : base(message)
        {
        }

        public MissingLocalizationKeyException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
