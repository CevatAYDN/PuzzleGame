using System.Collections.Generic;
using PuzzleGame.Domain.Interfaces;
using PuzzleGame.Domain.Models;

namespace PuzzleGame.Application.Services
{
    /// <summary>
    /// Her hamle öncesinde tüm şişelerin state'ini (Layers listesi)
    /// kaydeder ve Undo ile bir önceki state'e dönmeyi sağlar.
    /// Immutable snapshot: orijinal liste değiştirilmez.
    /// </summary>
    public class GameHistoryService : IGameHistoryService
    {
        private readonly Stack<List<LiquidLayer>[]> _history = new Stack<List<LiquidLayer>[]>();

        public bool CanUndo => _history.Count > 0;

        public void RecordSnapshot(BottleState[] bottles)
        {
            if (bottles == null) return;

            var snapshot = new List<LiquidLayer>[bottles.Length];
            for (int i = 0; i < bottles.Length; i++)
            {
                var state = bottles[i];
                if (state == null) continue; // null bottle = boş layer
                snapshot[i] = new List<LiquidLayer>(state.Layers);
            }
            _history.Push(snapshot);
        }

        public void Undo()
        {
            if (_history.Count == 0) return;
            var snapshot = _history.Pop();
            // BottleState'e yazmak için external setter gerek —
            // arayüz tasarımı gereği BottleState.Layers read-only.
            // Bu nedenle snapshot'ları tüketen taraf (GameManager)
            // her şişeye Clear() + AddLayer() ile yükler.
            LastSnapshot = snapshot;
        }

        /// <summary>
        /// Undo() çağrıldığında doldurulur.
        /// Tüketen taraf (GameManager) buradaki listeleri BottleState'lere yükler.
        /// </summary>
        public List<LiquidLayer>[] LastSnapshot { get; private set; }
    }
}
