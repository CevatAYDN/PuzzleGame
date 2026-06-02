using PuzzleGame.Domain.Models;

using System.Collections.Generic;

namespace PuzzleGame.Domain.Interfaces
{
    public interface IGameHistoryService
    {
        bool CanUndo { get; }
        List<LiquidLayer>[] LastSnapshot { get; }
        void RecordSnapshot(BottleState[] bottles);
        void Undo();
        void Clear();
    }
}
