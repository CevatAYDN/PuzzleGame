using System;
using UnityEngine;
using PuzzleGame.Domain.Interfaces;
using PuzzleGame.Domain.Models;
using PuzzleGame.Infrastructure.Interfaces;
using PuzzleGame.Events;
using PuzzleGame.Logging;

namespace PuzzleGame.Application.Services
{
    /// <summary>
    /// Handles game history and undo functionality
    /// </summary>
    public class GameHistoryManagementService
    {
        private readonly IGameHistoryService _historyService;
        private readonly BottleController[] _bottles;
        private int _moveCount;
        private Action<int> _updateHUDCallback;

        public GameHistoryManagementService(IGameHistoryService historyService, BottleController[] bottles)
        {
            _historyService = historyService;
            _bottles = bottles;
        }

        public void SetUpdateHUDCallback(Action<int> callback)
        {
            _updateHUDCallback = callback;
        }

        public void SetMoveCount(int moveCount)
        {
            _moveCount = moveCount;
        }

        public void RecordUndoSnapshot()
        {
            if (_bottles == null || _historyService == null) return;
            var states = new BottleState[_bottles.Length];
            for (int i = 0; i < _bottles.Length; i++)
                states[i] = _bottles[i]?.State;
            _historyService.RecordSnapshot(states);
        }

        public void Undo()
        {
            if (_historyService == null || !_historyService.CanUndo) return;

            _historyService.Undo();
            var snapshots = _historyService.LastSnapshot;
            if (snapshots == null || _bottles == null) return;

            for (int i = 0; i < snapshots.Length && i < _bottles.Length; i++)
            {
                if (_bottles[i] == null || _bottles[i].State == null) continue;
                _bottles[i].State.ReplaceLayers(snapshots[i]);
                _bottles[i].UpdateVisualsFromState();
            }

            _moveCount = Mathf.Max(0, _moveCount - 1);
            _updateHUDCallback?.Invoke(_moveCount);
            BottleLogger.LogInfo($"Undo. Moves: {_moveCount}");
        }

        public int GetCurrentMoveCount()
        {
            return _moveCount;
        }
    }
}