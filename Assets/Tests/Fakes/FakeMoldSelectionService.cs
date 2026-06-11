using System;
using System.Collections.Generic;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Domain.Models;

namespace PuzzleGame.Tests.Fakes
{
    /// <summary>
    /// Controllable fake for IMoldSelectionService.
    /// </summary>
    public class FakeMoldSelectionService : IMoldSelectionService
    {
        public MoldState SelectedMold { get; private set; }
        public IReadOnlyList<MoldState> SelectedMolds => _selectedMolds.AsReadOnly();
        public bool IsMultiSelect { get; private set; }

        private readonly List<MoldState> _selectedMolds = new List<MoldState>();

        public event Action<MoldState> OnMoldSelected;
        public event Action<MoldState> OnMoldDeselected;

        public int SelectCallCount { get; private set; }
        public int DeselectCallCount { get; private set; }

        public void SetMultiSelect(bool enabled)
        {
            IsMultiSelect = enabled;
            if (!enabled && _selectedMolds.Count > 1)
            {
                for (int i = _selectedMolds.Count - 1; i > 0; i--)
                {
                    var state = _selectedMolds[i];
                    _selectedMolds.RemoveAt(i);
                    OnMoldDeselected?.Invoke(state);
                }
            }
        }

        public void Select(MoldState Mold)
        {
            SelectCallCount++;
            SelectedMold = Mold;
            if (!IsMultiSelect)
                _selectedMolds.Clear();
            _selectedMolds.Add(Mold);
            OnMoldSelected?.Invoke(Mold);
        }

        public void ToggleSelection(MoldState Mold)
        {
            if (_selectedMolds.Contains(Mold))
            {
                _selectedMolds.Remove(Mold);
                SelectedMold = _selectedMolds.Count > 0 ? _selectedMolds[0] : null;
                OnMoldDeselected?.Invoke(Mold);
            }
            else
            {
                Select(Mold);
            }
        }

        public void Deselect()
        {
            DeselectCallCount++;
            var toDeselect = new List<MoldState>(_selectedMolds);
            _selectedMolds.Clear();
            SelectedMold = null;
            foreach (var state in toDeselect)
                OnMoldDeselected?.Invoke(state);
        }

        public void Deselect(MoldState Mold)
        {
            if (_selectedMolds.Remove(Mold))
            {
                SelectedMold = _selectedMolds.Count > 0 ? _selectedMolds[0] : null;
                OnMoldDeselected?.Invoke(Mold);
            }
        }

        public void RaiseSelected(MoldState mold) => OnMoldSelected?.Invoke(mold);
        public void RaiseDeselected(MoldState mold) => OnMoldDeselected?.Invoke(mold);
    }
}
