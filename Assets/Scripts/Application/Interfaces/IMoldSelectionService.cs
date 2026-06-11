using PuzzleGame.Domain.Models;
using System;
using System.Collections.Generic;

namespace PuzzleGame.Application.Interfaces
{
    /// <summary>
    /// Tracks which Mold(s) the player has currently selected.
    /// Supports both single-select (default) and multi-select (for multi-pour).
    /// Fires events so any listener (UI, audio, etc.) can react without polling.
    /// </summary>
    public interface IMoldSelectionService
    {
        /// <summary>Fired when a Mold becomes selected. Argument is the newly selected state.</summary>
        event Action<MoldState> OnMoldSelected;

        /// <summary>Fired when the current selection is cleared. Argument is the state that was deselected.</summary>
        event Action<MoldState> OnMoldDeselected;

        /// <summary>The currently selected Mold state (primary), or null when nothing is selected.</summary>
        MoldState SelectedMold { get; }

        /// <summary>All currently selected molds (includes SelectedMold). Empty list when nothing selected.</summary>
        IReadOnlyList<MoldState> SelectedMolds { get; }

        /// <summary>Whether multi-select mode is active (for multi-pour).</summary>
        bool IsMultiSelect { get; }

        /// <summary>Enable or disable multi-select mode.</summary>
        void SetMultiSelect(bool enabled);

        /// <summary>Select a single Mold (replaces current selection in single mode, adds in multi mode).</summary>
        void Select(MoldState Mold);

        /// <summary>Toggle a Mold in/out of the selection (multi-select mode only).</summary>
        void ToggleSelection(MoldState Mold);

        /// <summary>Clear all selections.</summary>
        void Deselect();

        /// <summary>Deselect a specific Mold from the selection (multi-select mode only).</summary>
        void Deselect(MoldState Mold);
    }
}
