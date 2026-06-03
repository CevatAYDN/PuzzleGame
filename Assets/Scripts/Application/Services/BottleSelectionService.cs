using System;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Domain.Models;

namespace PuzzleGame.Application.Services
{
    /// <summary>
    /// Plain C# service — no MonoBehaviour, fully testable.
    /// Selecting an already-selected bottle deselects it (toggle behaviour).
    /// </summary>
    public class BottleSelectionService : IBottleSelectionService
    {
        public event Action<BottleState> OnBottleSelected;
        public event Action<BottleState> OnBottleDeselected;

        public BottleState SelectedBottle { get; private set; }

        /// <exception cref="ArgumentNullException">If bottle is null.</exception>
        public void Select(BottleState bottle)
        {
            if (bottle == null) throw new ArgumentNullException(nameof(bottle));

            if (SelectedBottle == bottle)
            {
                Deselect();
                return;
            }

            if (SelectedBottle != null)
                FireDeselect(SelectedBottle);

            SelectedBottle = bottle;
            OnBottleSelected?.Invoke(bottle);
        }

        public void Deselect()
        {
            if (SelectedBottle == null) return;

            var previous = SelectedBottle;
            SelectedBottle = null;
            FireDeselect(previous);
        }

        private void FireDeselect(BottleState bottle) =>
            OnBottleDeselected?.Invoke(bottle);
    }
}
