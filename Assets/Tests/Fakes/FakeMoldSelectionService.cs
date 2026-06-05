using System;
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

        public event Action<MoldState> OnMoldSelected;
        public event Action<MoldState> OnMoldDeselected;

        public int SelectCallCount { get; private set; }
        public int DeselectCallCount { get; private set; }

        public void Select(MoldState Mold)
        {
            SelectCallCount++;
            SelectedMold = Mold;
            OnMoldSelected?.Invoke(Mold);
        }

        public void Deselect()
        {
            DeselectCallCount++;
            var previous = SelectedMold;
            SelectedMold = null;
            if (previous != null)
                OnMoldDeselected?.Invoke(previous);
        }

        public void RaiseSelected(MoldState mold) => OnMoldSelected?.Invoke(mold);
        public void RaiseDeselected(MoldState mold) => OnMoldDeselected?.Invoke(mold);
    }
}
