using System;
using System.Collections.Generic;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Domain.Models;

namespace PuzzleGame.Application.Services
{
    /// <summary>
    /// Plain C# service — no MonoBehaviour, fully testable.
    /// Selecting an already-selected Mold deselects it (toggle behaviour).
    /// In multi-select mode, selecting additional molds adds them to the selection.
    /// </summary>
    public class MoldSelectionService : IMoldSelectionService
    {
        private readonly List<MoldState> _selectedMolds = new List<MoldState>();
        private bool _isMultiSelect;

        public event Action<MoldState> OnMoldSelected;
        public event Action<MoldState> OnMoldDeselected;

        public MoldState SelectedMold => _selectedMolds.Count > 0 ? _selectedMolds[0] : null;
        public IReadOnlyList<MoldState> SelectedMolds => _selectedMolds.AsReadOnly();
        public bool IsMultiSelect => _isMultiSelect;

        public void SetMultiSelect(bool enabled)
        {
            _isMultiSelect = enabled;
            if (!enabled && _selectedMolds.Count > 1)
            {
                // Collapse multi-select to single when disabling
                for (int i = _selectedMolds.Count - 1; i > 0; i--)
                {
                    var state = _selectedMolds[i];
                    _selectedMolds.RemoveAt(i);
                    FireDeselect(state);
                }
            }
        }

        /// <exception cref="ArgumentNullException">If Mold is null.</exception>
        public void Select(MoldState Mold)
        {
            if (Mold == null) return;

            if (_isMultiSelect)
            {
                // In multi-select mode: add to selection if not already present
                if (_selectedMolds.Contains(Mold))
                {
                    // Toggle off
                    _selectedMolds.Remove(Mold);
                    FireDeselect(Mold);
                }
                else
                {
                    _selectedMolds.Add(Mold);
                    OnMoldSelected?.Invoke(Mold);
                }
                return;
            }

            // Single-select mode: original behaviour
            if (_selectedMolds.Count == 1 && _selectedMolds[0] == Mold)
            {
                Deselect();
                return;
            }

            if (_selectedMolds.Count > 0)
            {
                var previous = _selectedMolds[0];
                _selectedMolds.Clear();
                FireDeselect(previous);
            }

            _selectedMolds.Add(Mold);
            OnMoldSelected?.Invoke(Mold);
        }

        public void ToggleSelection(MoldState Mold)
        {
            if (Mold == null) return;
            if (!_isMultiSelect)
            {
                Select(Mold);
                return;
            }

            if (_selectedMolds.Contains(Mold))
            {
                _selectedMolds.Remove(Mold);
                FireDeselect(Mold);
            }
            else
            {
                _selectedMolds.Add(Mold);
                OnMoldSelected?.Invoke(Mold);
            }
        }

        public void Deselect()
        {
            for (int i = _selectedMolds.Count - 1; i >= 0; i--)
            {
                var state = _selectedMolds[i];
                _selectedMolds.RemoveAt(i);
                FireDeselect(state);
            }
        }

        public void Deselect(MoldState Mold)
        {
            if (Mold == null) return;
            if (_selectedMolds.Remove(Mold))
            {
                FireDeselect(Mold);
            }
        }

        private void FireDeselect(MoldState Mold) =>
            OnMoldDeselected?.Invoke(Mold);
    }
}
