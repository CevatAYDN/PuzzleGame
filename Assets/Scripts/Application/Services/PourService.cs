using System;
using PuzzleGame.Domain;
using PuzzleGame.Domain.Models;
using PuzzleGame.Application.Configuration;
using PuzzleGame.Application.Configuration.FeatureSystem;
using PuzzleGame.Domain.Interfaces;
using PuzzleGame.Application.Events;
using PuzzleGame.Application.Logging;
using PuzzleGame.Application.Interfaces;

namespace PuzzleGame.Application.Services
{
    /// <summary>
    /// Modular pour service that handles both single and multi-layer pours.
    /// Supports data-driven feature flags from LevelData.
    /// IPourService interface lives in Application/Interfaces/IPourService.cs (Fix #6).
    /// </summary>
    public class PourService : IPourService
    {
        private readonly IBottleValidator _validator;
        private readonly IGameHistoryManager _historyManager;
        private readonly IReactionService _reactionService;
        private readonly IEventAggregator _eventAggregator;
        private LevelData _currentLevelData;

        public PourService(IBottleValidator validator, IGameHistoryManager historyManager, IReactionService reactionService, IEventAggregator eventAggregator)
        {
            _validator = validator;
            _historyManager = historyManager;
            _reactionService = reactionService;
            _eventAggregator = eventAggregator;
        }


        public void SetLevelData(LevelData levelData)
        {
            _currentLevelData = levelData;
        }

        public bool TryPour(IBottleView source, IBottleView target, LevelData levelData, IBottleView[] activeBottles)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (levelData == null) throw new ArgumentNullException(nameof(levelData),
                "levelData is required. Default to no-multi-layer if unsure.");

            bool enableMultiLayer = levelData.enableMultiLayerPour;

            return enableMultiLayer
                ? TryMultiLayerPour(source, target, levelData, activeBottles)
                : TrySingleLayerPour(source, target, activeBottles);
        }

        public int GetPourLayerCount(IBottleView source, IBottleView target, LevelData levelData)
        {
            if (source == null || target == null) return 0;

            bool enableMultiLayer = levelData?.enableMultiLayerPour ?? false;
            var sourceState = source.State;
            var targetState = target.State;

            if (sourceState.IsEmpty) return 0;
            if (targetState.IsFull) return 0;

            var topLayerOpt = sourceState.TopLayer;
            if (topLayerOpt == null) return 0;
            var topColor = topLayerOpt.Value.Color;

            if (!enableMultiLayer)
            {
                return _validator.CanPour(sourceState, targetState) ? 1 : 0;
            }

            int maxCapacity = targetState.MaxLayers - targetState.LayerCount;
            int consecutiveCount = 0;

            for (int i = 0; i < sourceState.LayerCount && i < maxCapacity; i++)
            {
                var layerOpt = sourceState.GetLayerAt(sourceState.LayerCount - 1 - i);

                if (_validator.ColorsMatch(layerOpt.Color, topColor))
                {
                    consecutiveCount++;
                }
                else
                {
                    break;
                }
            }

            var config = levelData?.multiLayerPourConfig;
            int minRequired = config?.minConsecutiveForPour ?? BottleConstants.MinEmptyBottles;

            if (config?.pourConsecutiveOnly ?? true)
            {
                return consecutiveCount >= minRequired ? consecutiveCount : 0;
            }

            return consecutiveCount;
        }

        private bool TrySingleLayerPour(IBottleView source, IBottleView target, IBottleView[] activeBottles)
        {
            if (source.State.IsEmpty)
            {
                _eventAggregator.Publish(new PourRejectedEvent(
                    GetBottleIndex(source),
                    GetBottleIndex(target),
                    "source_empty"));
                return false;
            }

            if (!_validator.CanPour(source.State, target.State))
            {
                _eventAggregator.Publish(new PourRejectedEvent(
                    GetBottleIndex(source),
                    GetBottleIndex(target),
                    "validator_rejected"));
                return false;
            }

            LiquidLayer layer = source.State.PopTopLayer();

            try
            {
                target.State.AddLayer(layer);
            }
            catch (InvalidOperationException ex)
            {
                // Validator said can pour but AddLayer failed — invariant violation.
                // Roll back the popped layer.
                source.State.AddLayer(layer);
                throw new InvalidOperationException(
                    "PourService invariant violated: CanPour passed but AddLayer rejected the layer.",
                    ex);
            }

            _historyManager.RecordUndoSnapshot();
            _historyManager.IncrementMoveCount();

            _eventAggregator.Publish(new PourCompletedEvent(source.State, target.State));

            CheckForReactions(source, target, activeBottles);

            BottleLogger.LogInfo($"[PourService] Single layer pour: {source.GameObject.name} → {target.GameObject.name}");
            return true;
        }

        private bool TryMultiLayerPour(IBottleView source, IBottleView target, LevelData levelData, IBottleView[] activeBottles)
        {
            int pourCount = GetPourLayerCount(source, target, levelData);

            if (pourCount == 0)
            {
                BottleLogger.LogDebug("[PourService] Multi-layer pour: no valid pour found");
                return false;
            }

            var topLayerOpt = source.State.TopLayer;
            var topColor = topLayerOpt?.Color ?? default;


            int poured = 0;
            var rolledBackLayers = new System.Collections.Generic.List<LiquidLayer>(pourCount);

            for (int i = 0; i < pourCount; i++)
            {
                LiquidLayer layer;
                try
                {
                    layer = source.State.PopTopLayer();
                }
                catch (InvalidOperationException)
                {
                    // Source ran dry unexpectedly — rollback already-poured layers.
                    break;
                }

                try
                {
                    target.State.AddLayer(layer);
                    poured++;
                }
                catch (InvalidOperationException ex)
                {
                    rolledBackLayers.Add(layer);
                    // Rollback any layers already poured to target this iteration.
                    for (int r = rolledBackLayers.Count - 1; r >= 0; r--)
                    {
                        source.State.AddLayer(rolledBackLayers[r]);
                    }
                    throw new InvalidOperationException(
                        "PourService multi-layer invariant violated.", ex);
                }
            }

            if (poured > 0)
            {
                _historyManager.RecordUndoSnapshot();
                _historyManager.IncrementMoveCount();

                CheckForReactions(source, target, activeBottles);

                BottleLogger.LogInfo($"[PourService] Multi-layer pour: {poured} layers from {source.GameObject.name} → {target.GameObject.name}");
                return true;
            }

            return false;
        }

        // Fix #14: Use BottleIndex property instead of parsing GameObject.name.
        private static int GetBottleIndex(IBottleView bottle) => bottle.BottleIndex;

        // Fix Code Quality #4: Delegate to _validator.ColorsMatch() — no duplicate logic.
        // IsColorMatch private method removed.
        private void CheckForReactions(IBottleView source, IBottleView target, IBottleView[] activeBottles)
        {
            if (_currentLevelData == null || !_currentLevelData.enableReactionSystem)
                return;

            if (_currentLevelData.reactionConfig == null || !_currentLevelData.reactionConfig.enableReactions)
                return;

            if (_reactionService == null) return;
            if (activeBottles == null || activeBottles.Length == 0) return;

            int count = _reactionService.CheckReactions(activeBottles, _currentLevelData.reactionConfig);

            if (count > 0)
            {
                BottleLogger.LogInfo($"[PourService] {count} reaction(s) detected!");
            }
        }
    }
}
