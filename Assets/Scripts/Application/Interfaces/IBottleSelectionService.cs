using PuzzleGame.Domain.Models;
using System;

namespace PuzzleGame.Application.Interfaces
{
    /// <summary>
    /// Tracks which bottle the player has currently selected.
    /// Fires events so any listener (UI, audio, etc.) can react without polling.
    /// </summary>
    public interface IBottleSelectionService
    {
        /// <summary>Fired when a bottle becomes selected. Argument is the newly selected state.</summary>
        event Action<BottleState> OnBottleSelected;

        /// <summary>Fired when the current selection is cleared. Argument is the state that was deselected.</summary>
        event Action<BottleState> OnBottleDeselected;

        /// <summary>The currently selected bottle state, or null when nothing is selected.</summary>
        BottleState SelectedBottle { get; }

        void Select(BottleState bottle);
        void Deselect();
    }
}
