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

            // Fix #4: Reaction sonrası source mold state'inin güncellenmesi için
            // source ve target referanslarını tut. ReactionService, IMoldView
            // üzerinden state'i modify edebilir ve bu değişiklikler source/target
            // üzerinden otomatik olarak görünür olur.

            return levelData.enableMultiLayerCast
                ? TryMultiLayerCast(source, target, levelData, activeMolds)
                : TrySingleLayerCast(source, target, activeMolds);
        }

        public bool TryMultiCast(IMoldView[] sources, IMoldView target, LevelData levelData, IMoldView[] activeMolds)
        {
            if (sources == null || sources.Length == 0) return false;
            if (target == null) return false;
            if (levelData == null) return false;

            // Pre-validate all sources against target
            var sourceStates = new MoldState[sources.Length];
            for (int i = 0; i < sources.Length; i++)
            {
                if (sources[i] == null) return false;
                sourceStates[i] = sources[i].State;
            }

            if (!_validator.CanMultiCast(sourceStates, target.State))
            {
                MoldLogger.LogWarning("[CastService] Multi-cast validation rejected.");
                return false;
            }

            // Cork check: if target has a cork, each source must match the cork color
            if (target.State.HasCork)
            {
                for (int i = 0; i < sources.Length; i++)
                {
                    if (!_validator.CanBreakCork(sourceStates[i], target.State))
                    {
                        MoldLogger.LogWarning("[CastService] Multi-cast cork blocked by source " + i);
                        return false;
                    }
                }
            }

            // Freeze check: no source can be frozen
            for (int i = 0; i < sources.Length; i++)
            {
                var top = sourceStates[i].TopLayer;
                if (top != null && top.Value.IsFrozen)
                {
                    MoldLogger.LogWarning("[CastService] Multi-cast rejected: source " + i + " top layer is frozen.");
                    return false;
                }
            }

            _historyManager.RecordUndoSnapshot();

            // Break cork if needed
            if (target.State.HasCork)
                target.State.BreakCork();

            // Pop from each source, add to target
            int casted = 0;
            var poppedLayers = new OreLayer[sources.Length];
            try
            {
                for (int i = 0; i < sources.Length; i++)
                {
                    OreLayer layer = sourceStates[i].PopTopLayer();
                    poppedLayers[i] = layer;
                    target.State.AddLayer(layer);
                    casted++;
                }

                ThawFrozenLayers(target);
                FinalizeCast(sources[0], target, activeMolds, casted);
                return true;
            }
            catch (InvalidOperationException ex)
            {
                // Rollback
                for (int r = casted - 1; r >= 0; r--)
                {
                    target.State.PopTopLayer();
                    sourceStates[r].AddLayer(poppedLayers[r]);
                }
                _historyManager.Undo();
                MoldLogger.LogError($"[CastService] Multi-cast failed mid-loop: {ex.Message}");
                return false;
            }
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

            if (target.State.HasCork && !_validator.CanBreakCork(source.State, target.State))
            {
                _eventAggregator.Publish(new CastRejectedEvent(GetMoldIndex(source), GetMoldIndex(target), "cork_blocked"));
                _errorIndicator?.ShowErrorOnMold(GetMoldIndex(target), "cork_blocked");
                return false;
            }

            _historyManager.RecordUndoSnapshot();

            if (target.State.HasCork)
            {
                target.State.BreakCork();
            }
            OreLayer layer = source.State.PopTopLayer();

            try
            {
                target.State.AddLayer(layer);
                ThawFrozenLayers(target);
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
            // Fix #5: Pre-validate EVERYTHING before mutating state and recording the
            // undo snapshot. The previous implementation recorded the snapshot
            // upfront and then rolled back mid-loop if the target filled up — that
            // left a "dirty" no-op snapshot in the undo stack, so a subsequent
            // Undo() (which costs coins via UndoService) would change nothing and
            // silently drain the player's wallet.
            int castCount = GetCastLayerCount(source, target, levelData);
            if (castCount == 0) return false;

            // Sanity-check the actual state still matches what GetCastLayerCount saw.
            // Between the two calls a concurrent cast could have changed things.
            var sourceState = source.State;
            var targetState = target.State;
            if (sourceState.IsEmpty) return false;
            if (targetState.IsFull) return false;

            // Verify source actually has `castCount` consecutive top layers matching.
            // GetCastLayerCount only checks the COUNT, not that they still exist.
            if (sourceState.LayerCount < castCount) return false;
            var topColor = sourceState.TopLayer.HasValue
                ? sourceState.TopLayer.Value.Color
                : default(DomainColor);
            int consecutive = 0;
            for (int i = sourceState.LayerCount - 1; i >= 0 && consecutive < castCount; i--)
            {
                if (_validator.ColorsMatch(sourceState.GetLayerAt(i).Color, topColor))
                    consecutive++;
                else
                    break;
            }
            if (consecutive < castCount) return false;

            int targetFreeSlots = targetState.MaxLayers - targetState.LayerCount;
            if (castCount > targetFreeSlots) return false;

            // All preconditions hold — NOW record the snapshot.
            _historyManager.RecordUndoSnapshot();

            int casted = 0;
            OreLayer[] rollbackBuffer = ArrayPool<OreLayer>.Shared.Rent(castCount);

            try
            {
                for (int i = 0; i < castCount; i++)
                {
                    OreLayer layer = sourceState.PopTopLayer();
                    targetState.AddLayer(layer);
                    rollbackBuffer[casted] = layer;
                    casted++;
                }

                ThawFrozenLayers(target);
                FinalizeCast(source, target, activeMolds, casted);
                return true;
            }
            catch (InvalidOperationException ex)
            {
                // Defensive: if any mutation throws (shouldn't happen after
                // pre-validation, but MoldState fails loudly), restore state and
                // pop the snapshot we just recorded so the undo stack stays clean.
                Rollback(source, target, casted, rollbackBuffer);
                _historyManager.Undo();
                MoldLogger.LogError($"[CastService] Multi-layer cast failed mid-loop: {ex.Message}");
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

        private static void ThawFrozenLayers(IMoldView target)
        {
            var state = target.State;
            int count = state.LayerCount;
            if (count < 2) return;

            for (int i = count - 2; i >= 0; i--)
            {
                var lower = state.GetLayerAt(i);
                if (!lower.IsFrozen) continue;

                var upper = state.GetLayerAt(i + 1);
                if (upper.ColorType == lower.ColorType)
                {
                    state.ReplaceAtIndex(i, lower.WithModifier(LayerModifier.None));
                }
            }
        }

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
