using System;

namespace PuzzleGame.Domain.Exceptions
{
    /// <summary>
    /// Thrown when an accessibility requirement is not met, such as missing a DomainPattern
    /// fallback for a specific color in Color-Blind Mode.
    /// Enforces the "Fail Fast" rule of the QA & Security Specialist.
    /// </summary>
    public class MissingAccessibilityFallbackException : Exception
    {
        public MissingAccessibilityFallbackException(string message) : base(message)
        {
        }

        public MissingAccessibilityFallbackException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
