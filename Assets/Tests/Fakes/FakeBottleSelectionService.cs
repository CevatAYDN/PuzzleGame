using System;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Domain.Models;

namespace PuzzleGame.Tests.Fakes
{
    /// <summary>
    /// Controllable fake for IBottleSelectionService.
    /// </summary>
    public class FakeBottleSelectionService : IBottleSelectionService
    {
        public BottleState SelectedBottle { get; private set; }

        public event Action<BottleState> OnBottleSelected;
        public event Action<BottleState> OnBottleDeselected;

        public int SelectCallCount { get; private set; }
        public int DeselectCallCount { get; private set; }

        public void Select(BottleState bottle)
        {
            SelectCallCount++;
            SelectedBottle = bottle;
            OnBottleSelected?.Invoke(bottle);
        }

        public void Deselect()
        {
            DeselectCallCount++;
            var previous = SelectedBottle;
            SelectedBottle = null;
            if (previous != null)
                OnBottleDeselected?.Invoke(previous);
        }
    }
}
