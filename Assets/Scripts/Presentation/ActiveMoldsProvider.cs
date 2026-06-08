using System;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Logging;

namespace PuzzleGame.Presentation
{
    /// <summary>
    /// Provides runtime access to the active mold list for components that need it
    /// (e.g., MoldInputRouter for hit detection). Separated from MoldPoolInitializer
    /// to break the circular dependency: MoldPoolInitializer → InputHandlerService →
    /// MoldInputRouter → IActiveMoldsProvider.
    /// </summary>
    public sealed class ActiveMoldsProvider : IActiveMoldsProvider
    {
        private IMoldView[] _molds;

        public IMoldView[] Molds
        {
            get => _molds ?? Array.Empty<IMoldView>();
            set
            {
                if (value == null)
                {
                    MoldLogger.LogWarning("[ActiveMoldsProvider] Attempted to set null molds.");
                }
                _molds = value;
            }
        }
    }
}
