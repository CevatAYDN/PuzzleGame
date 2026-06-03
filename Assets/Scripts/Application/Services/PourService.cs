using System;
using System.Linq;
using UnityEngine;
using PuzzleGame.Domain;
using PuzzleGame.Domain.Models;
using PuzzleGame.Domain.Models.FeatureSystem;
using PuzzleGame.Domain.Interfaces;
using PuzzleGame.Events;
using PuzzleGame.Logging;
using PuzzleGame.Application.Interfaces;

namespace PuzzleGame.Application.Services
{
    /// <summary>
    /// Modular pour service that handles both single and multi-layer pours.
    /// Supports data-driven feature flags from LevelData.
    /// </summary>
    public interface IPourService
    {
        bool TryPour(IBottleView source, IBottleView target, LevelData levelData);
        int GetPourLayerCount(IBottleView source, IBottleView target, LevelData levelData);
    }

    public class PourService : IPourService
    {
        private readonly IBottleValidator _validator;
        private readonly IGameHistoryManager _historyManager;
        private readonly IReactionService _reactionService;
        private LevelData _currentLevelData;

        public PourService(IBottleValidator validator, IGameHistoryManager historyManager, IReactionService reactionService)
        {
            _validator = validator;
            _historyManager = historyManager;
            _reactionService = reactionService;
        }

        public void SetLevelData(LevelData levelData)
        {
            _currentLevelData = levelData;
        }

        public bool TryPour(IBottleView source, IBottleView target, LevelData levelData)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (levelData == null) throw new ArgumentNullException(nameof(levelData),
                "levelData is required. Default to no-multi-layer if unsure.");

            bool enableMultiLayer = levelData.enableMultiLayerPour;

            EventAggregator.Publish(new PourStartedEvent(
                GetBottleIndex(source),
                GetBottleIndex(target),
                enableMultiLayer));

            return enableMultiLayer
                ? TryMultiLayerPour(source, target, levelData)
                : TrySingleLayerPour(source, target);
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

                if (IsColorMatch(layerOpt.Color, topColor))
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

        private bool TrySingleLayerPour(IBottleView source, IBottleView target)
        {
            if (!_validator.CanPour(source.State, target.State))
            {
                EventAggregator.Publish(new PourRejectedEvent(
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

            EventAggregator.Publish(new PourCompletedEvent(source.State, target.State));

            BottleLogger.LogInfo($"[PourService] Single layer pour: {source.GameObject.name} → {target.GameObject.name}");
            return true;
        }

        private bool TryMultiLayerPour(IBottleView source, IBottleView target, LevelData levelData)
        {
            int pourCount = GetPourLayerCount(source, target, levelData);

            if (pourCount == 0)
            {
                BottleLogger.LogDebug("[PourService] Multi-layer pour: no valid pour found");
                EventAggregator.Publish(new PourRejectedEvent(
                    GetBottleIndex(source),
                    GetBottleIndex(target),
                    "no_matching_layers"));
                return false;
            }

            var topLayerOpt = source.State.TopLayer;
            var topColor = topLayerOpt?.Color ?? default;

            EventAggregator.Publish(new MultiLayerPourStartedEvent(
                GetBottleIndex(source),
                GetBottleIndex(target),
                pourCount,
                Color.white));

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

                EventAggregator.Publish(new MultiLayerPourCompletedEvent(
                    GetBottleIndex(source),
                    GetBottleIndex(target),
                    poured));

                CheckForReactions(source, target);

                BottleLogger.LogInfo($"[PourService] Multi-layer pour: {poured} layers from {source.GameObject.name} → {target.GameObject.name}");
                return true;
            }

            return false;
        }

        private int GetBottleIndex(IBottleView bottle)
        {
            var name = bottle.GameObject.name;
            var parts = name.Split('_');
            if (parts.Length > 1 && int.TryParse(parts[parts.Length - 1], out int index))
                return index;
            return 0;
        }

        private bool IsColorMatch(DomainColor a, DomainColor b, float tolerance = BottleConstants.ColorMatchEpsilon)
        {
            return UnityEngine.Mathf.Abs(a.R - b.R) < tolerance &&
                   UnityEngine.Mathf.Abs(a.G - b.G) < tolerance &&
                   UnityEngine.Mathf.Abs(a.B - b.B) < tolerance &&
                   UnityEngine.Mathf.Abs(a.A - b.A) < tolerance;
        }

        private void CheckForReactions(IBottleView source, IBottleView target)
        {
            if (_currentLevelData == null || !_currentLevelData.enableReactionSystem)
                return;

            if (_currentLevelData.reactionConfig == null || !_currentLevelData.reactionConfig.enableReactions)
                return;

            if (_reactionService == null) return;

            var bottles = UnityEngine.Object.FindObjectsByType<UnityEngine.MonoBehaviour>()
                .OfType<IBottleView>()
                .ToArray();

            if (bottles.Length == 0) return;

            var results = _reactionService.CheckReactions(bottles, _currentLevelData.reactionConfig);

            if (results.Count > 0)
            {
                BottleLogger.LogInfo($"[PourService] {results.Count} reaction(s) detected!");
            }
        }
    }
}
