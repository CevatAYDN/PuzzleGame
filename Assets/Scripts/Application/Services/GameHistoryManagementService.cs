using System;
using UnityEngine;
using PuzzleGame.Domain.Interfaces;
using PuzzleGame.Domain.Models;
using PuzzleGame.Infrastructure.Interfaces;
using PuzzleGame.Logging;
using PuzzleGame.Application.Interfaces;

namespace PuzzleGame.Application.Services
{
    /// <summary>
    /// Handles game history and undo functionality.
    /// Single source of truth for MoveCount.
    /// GameManager updates via callback.
    /// </summary>
    public class GameHistoryManagementService
    {
        private readonly IGameHistoryService _historyService;
        private readonly IBottleView[] _bottles;
        private int _moveCount;
        private Action<int> _onMoveCountChanged;

        public int CurrentMoveCount => _moveCount;
        public IGameHistoryService HistoryService => _historyService;

        public GameHistoryManagementService(IGameHistoryService historyService, IBottleView[] bottles)
        {
            _historyService = historyService;
            _bottles = bottles;
            _moveCount = 0;
        }

        public void SetMoveCountChangedCallback(Action<int> callback)
        {
            _onMoveCountChanged = callback;
        }

        public void ResetMoveCount()
        {
            _moveCount = 0;
            _onMoveCountChanged?.Invoke(_moveCount);
        }

        public void IncrementMoveCount()
        {
            _moveCount++;
            _onMoveCountChanged?.Invoke(_moveCount);
            BottleLogger.LogInfo($"Move incremented: {_moveCount}");
        }

        public void RecordUndoSnapshot()
        {
            if (_bottles == null || _historyService == null) return;
            var states = new BottleState[_bottles.Length];
            for (int i = 0; i < _bottles.Length; i++)
                states[i] = _bottles[i]?.State;
            _historyService.RecordSnapshot(states);
        }

        public bool CanUndo => _historyService != null && _historyService.CanUndo;

        public void Undo()
        {
            if (!CanUndo) return;

            _historyService.Undo();
            var snapshots = _historyService.LastSnapshot;
            if (snapshots == null || _bottles == null) return;

            for (int i = 0; i < snapshots.Length && i < _bottles.Length; i++)
            {
                if (_bottles[i] == null || _bottles[i].State == null) continue;
                _bottles[i].State.ReplaceLayers(snapshots[i]);
                _bottles[i].UpdateVisualsFromState();
            }

            // Decrement move count after successful undo
            _moveCount = Mathf.Max(0, _moveCount - 1);
            _onMoveCountChanged?.Invoke(_moveCount);
            BottleLogger.LogInfo($"Undo performed. Current moves: {_moveCount}");
        }
        
        /// <summary>
        /// Reset history and move count (for new level).
        /// </summary>
        public void ResetAll()
        {
            _historyService?.Clear();
            _moveCount = 0;
            _onMoveCountChanged?.Invoke(_moveCount);
            BottleLogger.LogInfo("History and move count reset.");
        }
    }
}
