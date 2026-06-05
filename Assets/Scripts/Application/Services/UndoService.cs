using PuzzleGame.Application.Configuration;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Logging;
using PuzzleGame.Application.Events;

namespace PuzzleGame.Application.Services
{
    /// <summary>
    /// Coin-gated undo. Delegates to <see cref="IGameHistoryManager.Undo"/>
    /// after charging the configured cost via <see cref="ICoinWallet"/>.
    /// Resets per-level counter on <see cref="LevelSelectedEvent"/>.
    /// </summary>
    public sealed class UndoService : IUndoService
    {
        private const string LogTag = "[UndoService]";

        private readonly ICoinWallet _wallet;
        private readonly IGameHistoryManager _history;
        private readonly EconomyConfig _config;
        private readonly IEventAggregator _events;
        private int _undosUsedThisLevel;

        public int Cost => _config != null ? _config.undoCost : 0;
        public int RemainingUndosForCurrentLevel =>
            _config == null ? 0 : System.Math.Max(0, _config.maxUndoPerLevel - _undosUsedThisLevel);

        public UndoService(
            ICoinWallet wallet,
            IGameHistoryManager history,
            EconomyConfig config,
            IEventAggregator events)
        {
            _wallet = wallet;
            _history = history;
            _config = config;
            _events = events;
            _events.Subscribe<LevelSelectedEvent>(_ => _undosUsedThisLevel = 0);
        }

        public bool TryUndo()
        {
            if (!_history.CanUndo)
            {
                MoldLogger.LogInfo($"{LogTag} History stack empty.");
                return false;
            }
            if (_config != null && _undosUsedThisLevel >= _config.maxUndoPerLevel)
            {
                MoldLogger.LogInfo($"{LogTag} Daily undo limit reached for this level.");
                return false;
            }
            if (!_wallet.TrySpend(Cost, "undo"))
            {
                MoldLogger.LogInfo($"{LogTag} Insufficient coins for undo.");
                return false;
            }

            _history.Undo();
            _undosUsedThisLevel++;
            return true;
        }
    }
}
