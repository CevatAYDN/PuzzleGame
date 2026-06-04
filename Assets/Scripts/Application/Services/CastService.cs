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
    /// Modular Cast service that handles both single and multi-layer Casts.
    /// Supports data-driven feature flags from LevelData.
    /// ICastService interface lives in Application/Interfaces/ICastService.cs (Fix #6).
    /// </summary>
    public class CastService : ICastService
    {
        private readonly IMoldValidator _validator;
        private readonly IGameHistoryManager _historyManager;
        private readonly IReactionService _reactionService;
        private readonly IEventAggregator _eventAggregator;
        private LevelData _currentLevelData;

        public CastService(IMoldValidator validator, IGameHistoryManager historyManager, IReactionService reactionService, IEventAggregator eventAggregator)
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

        public bool TryCast(IMoldView source, IMoldView target, LevelData levelData, IMoldView[] activeMolds)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (levelData == null) throw new ArgumentNullException(nameof(levelData),
                "levelData is required. Default to no-multi-layer if unsure.");

            bool enableMultiLayer = levelData.enableMultiLayerCast;

            return enableMultiLayer
                ? TryMultiLayerCast(source, target, levelData, activeMolds)
                : TrySingleLayerCast(source, target, activeMolds);
        }

        public int GetCastLayerCount(IMoldView source, IMoldView target, LevelData levelData)
        {
            if (source == null || target == null) return 0;

            bool enableMultiLayer = levelData?.enableMultiLayerCast ?? false;
            var sourceState = source.State;
            var targetState = target.State;

            if (sourceState.IsEmpty) return 0;
            if (targetState.IsFull) return 0;

            var topLayerOpt = sourceState.TopLayer;
            if (topLayerOpt == null) return 0;
            var topColor = topLayerOpt.Value.Color;

            if (!enableMultiLayer)
            {
                return _validator.CanCast(sourceState, targetState) ? 1 : 0;
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

            var config = levelData?.multiLayerCastConfig;
            int minRequired = config?.minConsecutiveForCast ?? ForgeConstants.MinEmptyMolds;

            if (config?.CastConsecutiveOnly ?? true)
            {
                return consecutiveCount >= minRequired ? consecutiveCount : 0;
            }

            return consecutiveCount;
        }

        private bool TrySingleLayerCast(IMoldView source, IMoldView target, IMoldView[] activeMolds)
        {
            if (source.State.IsEmpty)
            {
                _eventAggregator.Publish(new CastRejectedEvent(
                    GetMoldIndex(source),
                    GetMoldIndex(target),
                    "source_empty"));
                return false;
            }

            if (!_validator.CanCast(source.State, target.State))
            {
                _eventAggregator.Publish(new CastRejectedEvent(
                    GetMoldIndex(source),
                    GetMoldIndex(target),
                    "validator_rejected"));
                return false;
            }

            OreLayer layer = source.State.PopTopLayer();

            try
            {
                target.State.AddLayer(layer);
            }
            catch (InvalidOperationException ex)
            {
                // Validator said can Cast but AddLayer failed — invariant violation.
                // Roll back the popped layer.
                source.State.AddLayer(layer);
                throw new InvalidOperationException(
                    "CastService invariant violated: CanCast passed but AddLayer rejected the layer.",
                    ex);
            }

            _historyManager.RecordUndoSnapshot();
            _historyManager.IncrementMoveCount();

            _eventAggregator.Publish(new CastCompletedEvent(source.State, target.State));

            CheckForReactions(source, target, activeMolds);

            MoldLogger.LogInfo($"[CastService] Single layer Cast: {source.GameObject.name} → {target.GameObject.name}");
            return true;
        }

        private bool TryMultiLayerCast(IMoldView source, IMoldView target, LevelData levelData, IMoldView[] activeMolds)
        {
            int CastCount = GetCastLayerCount(source, target, levelData);

            if (CastCount == 0)
            {
                MoldLogger.LogDebug("[CastService] Multi-layer Cast: no valid Cast found");
                return false;
            }

            var topLayerOpt = source.State.TopLayer;
            var topColor = topLayerOpt?.Color ?? default;


            int Casted = 0;
            var rolledBackLayers = new System.Collections.Generic.List<OreLayer>(CastCount);

            for (int i = 0; i < CastCount; i++)
            {
                OreLayer layer;
                try
                {
                    layer = source.State.PopTopLayer();
                }
                catch (InvalidOperationException)
                {
                    // Source ran dry unexpectedly — rollback already-Casted layers.
                    // FIX: Complete rollback of all Casted layers on early break
                    for (int r = Casted - 1; r >= 0; r--)
                    {
                        target.State.PopTopLayer();
                        source.State.AddLayer(rolledBackLayers[r]);
                    }
                    return false;
                }

                try
                {
                    target.State.AddLayer(layer);
                    Casted++;
                }
                catch (InvalidOperationException ex)
                {
                    rolledBackLayers.Add(layer);
                    // Rollback any layers already Casted to target this iteration.
                    for (int r = Casted - 1; r >= 0; r--)
                    {
                        target.State.PopTopLayer();
                        source.State.AddLayer(rolledBackLayers[r]);
                    }
                    throw new InvalidOperationException(
                        "CastService multi-layer invariant violated.", ex);
                }
            }

            if (Casted > 0)
            {
                _historyManager.RecordUndoSnapshot();
                _historyManager.IncrementMoveCount();

                CheckForReactions(source, target, activeMolds);

                MoldLogger.LogInfo($"[CastService] Multi-layer Cast: {Casted} layers from {source.GameObject.name} → {target.GameObject.name}");
                return true;
            }

            return false;
        }

        // Fix #14: Use MoldIndex property instead of parsing GameObject.name.
        private static int GetMoldIndex(IMoldView Mold) => Mold.MoldIndex;

        // Fix Code Quality #4: Delegate to _validator.ColorsMatch() — no duplicate logic.
        // IsColorMatch private method removed.
        private void CheckForReactions(IMoldView source, IMoldView target, IMoldView[] activeMolds)
        {
            if (_currentLevelData == null || !_currentLevelData.enableReactionSystem)
                return;

            if (_currentLevelData.reactionConfig == null || !_currentLevelData.reactionConfig.enableReactions)
                return;

            if (_reactionService == null) return;
            if (activeMolds == null || activeMolds.Length == 0) return;

            int count = _reactionService.CheckReactions(activeMolds, _currentLevelData.reactionConfig);

            if (count > 0)
            {
                MoldLogger.LogInfo($"[CastService] {count} reaction(s) detected!");
            }
        }
    }
}
