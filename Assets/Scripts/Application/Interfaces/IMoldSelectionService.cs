using PuzzleGame.Domain.Models;
using System;

namespace PuzzleGame.Application.Interfaces
{
    /// <summary>
    /// Tracks which Mold the player has currently selected.
    /// Fires events so any listener (UI, audio, etc.) can react without polling.
    /// </summary>
    public interface IMoldSelectionService
    {
        /// <summary>Fired when a Mold becomes selected. Argument is the newly selected state.</summary>
        event Action<MoldState> OnMoldSelected;

        /// <summary>Fired when the current selection is cleared. Argument is the state that was deselected.</summary>
        event Action<MoldState> OnMoldDeselected;

        /// <summary>The currently selected Mold state, or null when nothing is selected.</summary>
        MoldState SelectedMold { get; }

        void Select(MoldState Mold);
        void Deselect();
    }
}
