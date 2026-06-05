using System.Collections.Generic;
using PuzzleGame.Application.Configuration;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Logging;
using PuzzleGame.Application.Events;
using PuzzleGame.Domain;
using PuzzleGame.Domain.Models;
using PuzzleGame.Domain.Services;
using UnityEngine;

namespace PuzzleGame.Application.Services
{
    /// <summary>
    /// Suggests the next best cast using the in-process <see cref="OreSortSolver"/>.
    /// Charges coins via <see cref="ICoinWallet"/> on success.
    /// Resets per-level counter on <see cref="LevelSelectedEvent"/>.
    /// </summary>
    public sealed class HintService : IHintService
    {
        private const string LogTag = "[HintService]";

        private readonly ICoinWallet _wallet;
        private readonly EconomyConfig _config;
        private readonly IActiveMoldsProvider _molds;
        private readonly IEventAggregator _events;

        private int _hintsUsedThisLevel;

        public int Cost => _config != null ? _config.hintCost : 0;
        public int RemainingHintsForCurrentLevel =>
            _config == null ? 0 : Mathf.Max(0, _config.maxHintPerLevel - _hintsUsedThisLevel);

        public HintService(
            ICoinWallet wallet,
            EconomyConfig config,
            IActiveMoldsProvider molds,
            IEventAggregator events)
        {
            _wallet = wallet;
            _config = config;
            _molds = molds;
            _events = events;
            _events.Subscribe<LevelSelectedEvent>(_ => _hintsUsedThisLevel = 0);
        }

        public bool TryGetHint(LevelData currentLevel, out int sourceMoldIndex, out int targetMoldIndex)
        {
            sourceMoldIndex = -1;
            targetMoldIndex = -1;

            if (currentLevel == null) return false;
            if (_config != null && _hintsUsedThisLevel >= _config.maxHintPerLevel)
            {
                MoldLogger.LogInfo($"{LogTag} Daily hint limit reached for this level.");
                return false;
            }
            if (!_wallet.TrySpend(Cost, "hint"))
            {
                MoldLogger.LogInfo($"{LogTag} Insufficient coins for hint.");
                return false;
            }

            var molds = _molds?.Molds;
            if (molds == null || molds.Length < 2) return false;

            var initial = SnapshotMolds(molds, currentLevel.maxLayersPerMold);
            var result = OreSortSolver.Solve(initial, currentLevel.maxLayersPerMold);
            if (!result.IsSolvable || result.SolutionPath == null || result.SolutionPath.Count == 0)
            {
                MoldLogger.LogInfo($"{LogTag} No solution found — puzzle may already be solved.");
                return false;
            }

            var first = result.SolutionPath[0];
            sourceMoldIndex = first.FromIndex;
            targetMoldIndex = first.ToIndex;
            _hintsUsedThisLevel++;
            return true;
        }

        private static List<List<OreLayer>> SnapshotMolds(IMoldView[] molds, int maxLayers)
        {
            var snapshot = new List<List<OreLayer>>(molds.Length);
            for (int i = 0; i < molds.Length; i++)
            {
                var layers = new List<OreLayer>();
                var state = molds[i].State;
                if (state?.Layers != null)
                {
                    for (int j = 0; j < state.Layers.Count; j++)
                    {
                        if (state.Layers[j].Amount > 0f)
                            layers.Add(state.Layers[j]);
                    }
                }
                snapshot.Add(layers);
            }
            return snapshot;
        }
    }
}
