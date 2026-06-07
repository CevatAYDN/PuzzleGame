using System;
using System.Buffers;
using System.Collections.Generic;
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
    /// </summary>
    public class CastService : ICastService
    {
        private readonly IMoldValidator _validator;
        private readonly IGameHistoryManager _historyManager;
        private readonly IReactionService _reactionService;
        private readonly IEventAggregator _eventAggregator;
        private readonly IErrorIndicatorService _errorIndicator;
        private LevelData _currentLevelData;

        public CastService(IMoldValidator validator, IGameHistoryManager historyManager, IReactionService reactionService, IEventAggregator eventAggregator, IErrorIndicatorService errorIndicator)
        {
            _validator = validator;
            _historyManager = historyManager;
            _reactionService = reactionService;
            _eventAggregator = eventAggregator;
            _errorIndicator = errorIndicator;
        }

        public void SetLevelData(LevelData levelData) => _currentLevelData = levelData;

        public bool TryCast(IMoldView source, IMoldView target, LevelData levelData, IMoldView[] activeMolds)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (levelData == null) throw new ArgumentNullException(nameof(levelData), "levelData is required.");

            return levelData.enableMultiLayerCast
                ? TryMultiLayerCast(source, target, levelData, activeMolds)
                : TrySingleLayerCast(source, target, activeMolds);
        }

        public int GetCastLayerCount(IMoldView source, IMoldView target, LevelData levelData)
        {
            if (source == null || target == null) return 0;

            bool enableMultiLayer = levelData?.enableMultiLayerCast ?? false;
            var sourceState = source.State;
            var targetState = target.State;

            if (sourceState.IsEmpty || targetState.IsFull) return 0;

            var topLayerOpt = sourceState.TopLayer;
            if (topLayerOpt == null) return 0;
            var topColor = topLayerOpt.Value.Color;

            if (!enableMultiLayer)
                return _validator.CanCast(sourceState, targetState) ? 1 : 0;

            int maxCapacity = targetState.MaxLayers - targetState.LayerCount;
            int consecutiveCount = 0;

            for (int i = 0; i < sourceState.LayerCount && i < maxCapacity; i++)
            {
                var layerOpt = sourceState.GetLayerAt(sourceState.LayerCount - 1 - i);

                if (_validator.ColorsMatch(layerOpt.Color, topColor))
                    consecutiveCount++;
                else break;
            }

            var config = levelData?.multiLayerCastConfig;
            int minRequired = config?.minConsecutiveForCast ?? ForgeConstants.MinEmptyMolds;

            if (config?.CastConsecutiveOnly ?? true)
                return consecutiveCount >= minRequired ? consecutiveCount : 0;

            return consecutiveCount;
        }

        private bool TrySingleLayerCast(IMoldView source, IMoldView target, IMoldView[] activeMolds)
        {
            if (source.State.IsEmpty)
            {
                _eventAggregator.Publish(new CastRejectedEvent(GetMoldIndex(source), GetMoldIndex(target), "source_empty"));
                _errorIndicator?.ShowErrorOnMold(GetMoldIndex(source), "source_empty");
                return false;
            }

            if (!_validator.CanCast(source.State, target.State))
            {
                _eventAggregator.Publish(new CastRejectedEvent(GetMoldIndex(source), GetMoldIndex(target), "validator_rejected"));
                _errorIndicator?.ShowErrorOnMold(GetMoldIndex(target), "validator_rejected");
                return false;
            }

            _historyManager.RecordUndoSnapshot();
            OreLayer layer = source.State.PopTopLayer();

            try
            {
                target.State.AddLayer(layer);
            }
            catch (InvalidOperationException ex)
            {
                source.State.AddLayer(layer);
                throw new InvalidOperationException("CastService invariant violated: CanCast passed but AddLayer rejected.", ex);
            }

            FinalizeCast(source, target, activeMolds, 1);
            return true;
        }

        private bool TryMultiLayerCast(IMoldView source, IMoldView target, LevelData levelData, IMoldView[] activeMolds)
        {
            int castCount = GetCastLayerCount(source, target, levelData);
            if (castCount == 0) return false;

            _historyManager.RecordUndoSnapshot();

            int casted = 0;
            OreLayer[] rollbackBuffer = ArrayPool<OreLayer>.Shared.Rent(castCount);

            try
            {
                for (int i = 0; i < castCount; i++)
                {
                    if (source.State.IsEmpty)
                    {
                        Rollback(source, target, casted, rollbackBuffer);
                        return false;
                    }

                    OreLayer layer = source.State.PopTopLayer();

                    if (target.State.IsFull)
                    {
                        source.State.AddLayer(layer);
                        Rollback(source, target, casted, rollbackBuffer);
                        return false;
                    }

                    target.State.AddLayer(layer);
                    rollbackBuffer[casted] = layer;
                    casted++;
                }

                if (casted > 0)
                {
                    FinalizeCast(source, target, activeMolds, casted);
                    return true;
                }

                return false;
            }
            finally
            {
                if (rollbackBuffer != null)
                {
                    ArrayPool<OreLayer>.Shared.Return(rollbackBuffer, clearArray: true);
                }
            }
        }

        private void Rollback(IMoldView source, IMoldView target, int castedCount, OreLayer[] buffer)
        {
            for (int r = castedCount - 1; r >= 0; r--)
            {
                target.State.PopTopLayer();
                source.State.AddLayer(buffer[r]);
            }
        }

        private void FinalizeCast(IMoldView source, IMoldView target, IMoldView[] activeMolds, int count)
        {
            _historyManager.IncrementMoveCount();

            _eventAggregator.Publish(new CastCompletedEvent(source.State, target.State));
            CheckForReactions(source, target, activeMolds);

            MoldLogger.LogInfo($"[CastService] Casted {count} layers: {source.GameObject.name} → {target.GameObject.name}");
        }

        private static int GetMoldIndex(IMoldView Mold) => Mold.MoldIndex;

        private void CheckForReactions(IMoldView source, IMoldView target, IMoldView[] activeMolds)
        {
            if (_currentLevelData == null || !_currentLevelData.enableReactionSystem) return;
            if (_currentLevelData.reactionConfig == null || !_currentLevelData.reactionConfig.enableReactions) return;
            if (_reactionService == null || activeMolds == null || activeMolds.Length == 0) return;

            int count = _reactionService.CheckReactions(activeMolds, _currentLevelData.reactionConfig);

            if (count > 0)
            {
                MoldLogger.LogInfo($"[CastService] {count} reaction(s) detected!");
            }
        }
    }
}
