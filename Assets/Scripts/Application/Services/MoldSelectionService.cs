using System;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Domain.Models;

namespace PuzzleGame.Application.Services
{
    /// <summary>
    /// Plain C# service — no MonoBehaviour, fully testable.
    /// Selecting an already-selected Mold deselects it (toggle behaviour).
    /// </summary>
    public class MoldSelectionService : IMoldSelectionService
    {
        public event Action<MoldState> OnMoldSelected;
        public event Action<MoldState> OnMoldDeselected;

        public MoldState SelectedMold { get; private set; }

        /// <exception cref="ArgumentNullException">If Mold is null.</exception>
        public void Select(MoldState Mold)
        {
            if (Mold == null) return;

            if (SelectedMold == Mold)
            {
                Deselect();
                return;
            }

            if (SelectedMold != null)
                FireDeselect(SelectedMold);

            SelectedMold = Mold;
            OnMoldSelected?.Invoke(Mold);
        }

        public void Deselect()
        {
            if (SelectedMold == null) return;

            var previous = SelectedMold;
            SelectedMold = null;
            FireDeselect(previous);
        }

        private void FireDeselect(MoldState Mold) =>
            OnMoldDeselected?.Invoke(Mold);
    }
}
