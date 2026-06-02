using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
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
        /// <summary>
        /// Attempt pour from source to target bottle.
        /// Returns true if any pour happened.
        /// </summary>
        bool TryPour(IBottleView source, IBottleView target, LevelData levelData);
        
        /// <summary>
        /// Get the number of layers that would be poured (for preview/UI).
        /// </summary>
        int GetPourLayerCount(IBottleView source, IBottleView target, LevelData levelData);
    }
    
    public class PourService : IPourService
    {
        private readonly IBottleValidator _validator;
        private readonly IGameHistoryManager _historyManager;
        
        public PourService(IBottleValidator validator, IGameHistoryManager historyManager)
        {
            _validator = validator;
            _historyManager = historyManager;
        }
        
        public bool TryPour(IBottleView source, IBottleView target, LevelData levelData)
        {
            if (source == null || target == null)
            {
                BottleLogger.LogWarning("PourService.TryPour: null source or target");
                return false;
            }
            
            // Check if multi-layer pour is enabled for this level
            bool enableMultiLayer = levelData?.enableMultiLayerPour ?? false;
            
            if (enableMultiLayer)
            {
                return TryMultiLayerPour(source, target, levelData);
            }
            else
            {
                return TrySingleLayerPour(source, target);
            }
        }
        
        public int GetPourLayerCount(IBottleView source, IBottleView target, LevelData levelData)
        {
            if (source == null || target == null) return 0;
            
            bool enableMultiLayer = levelData?.enableMultiLayerPour ?? false;
            var sourceState = source.State;
            var targetState = target.State;
            
            if (sourceState.LayerCount == 0 || targetState.LayerCount >= sourceState.MaxLayers)
                return 0;
            
            var topLayer = sourceState.PeekTopLayer();
            if (topLayer == null) return 0;
            var topColor = topLayer.Value.Color;
            
            if (!enableMultiLayer)
            {
                // Single layer: check if single pour is possible
                if (_validator.CanPour(sourceState, targetState))
                    return 1;
                return 0;
            }
            
            // Multi-layer: count consecutive same-colored layers
            int maxCapacity = targetState.MaxLayers - targetState.LayerCount;
            int consecutiveCount = 0;
            
            for (int i = 0; i < sourceState.LayerCount && i < maxCapacity; i++)
            {
                var layer = sourceState.GetLayerAt(sourceState.LayerCount - 1 - i);
                if (layer == null) break;
                
                if (IsColorMatch(layer.Value.Color, topColor))
                {
                    consecutiveCount++;
                }
                else
                {
                    break;
                }
            }
            
            // Check min consecutive requirement
            var config = levelData?.multiLayerPourConfig;
            int minRequired = config?.minConsecutiveForPour ?? 2;
            
            if (config?.pourConsecutiveOnly ?? true)
            {
                return consecutiveCount >= minRequired ? consecutiveCount : 0;
            }
            
            return consecutiveCount;
        }
        
        // ═══════════════════════════════════════════════════════════════════
        // PRIVATE METHODS
        // ═══════════════════════════════════════════════════════════════════
        
        private bool TrySingleLayerPour(IBottleView source, IBottleView target)
        {
            if (!_validator.CanPour(source.State, target.State))
                return false;
            
            var layer = source.State.PopTopLayer();
            if (layer == null) return false;
            
            if (!target.State.AddLayer(layer.Value))
            {
                source.State.AddLayer(layer.Value);
                return false;
            }
            
            // Record move for undo system
            _historyManager?.RecordUndoSnapshot();
            _historyManager?.IncrementMoveCount();
            
            // Publish event for UI/animations
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
                return false;
            }
            
            var topLayer = source.State.PeekTopLayer();
            var topColor = topLayer?.Color ?? new DomainColor(0, 0, 0, 0);
            
            // Publish started event for animations
            EventAggregator.Publish(new MultiLayerPourStartedEvent(
                GetBottleIndex(source),
                GetBottleIndex(target),
                pourCount,
                Color.white));
            
            // Perform the pours
            int poured = 0;
            for (int i = 0; i < pourCount; i++)
            {
                var layerOpt = source.State.PopTopLayer();
                if (layerOpt == null) break;
                
                var layer = layerOpt.Value;
                if (!target.State.AddLayer(layer))
                {
                    // Rollback
                    source.State.AddLayer(layer);
                    break;
                }
                poured++;
            }
            
            if (poured > 0)
            {
                _historyManager?.RecordUndoSnapshot();
                _historyManager?.IncrementMoveCount();
                
                // Publish completed event
                EventAggregator.Publish(new MultiLayerPourCompletedEvent(
                    GetBottleIndex(source),
                    GetBottleIndex(target),
                    poured));
                
                BottleLogger.LogInfo($"[PourService] Multi-layer pour: {poured} layers from {source.GameObject.name} → {target.GameObject.name}");
                return true;
            }
            
            return false;
        }
        
        private int GetBottleIndex(IBottleView bottle)
        {
            // Extract index from bottle name (e.g., "Bottle_01" -> 1)
            var name = bottle.GameObject.name;
            var parts = name.Split('_');
            if (parts.Length > 1 && int.TryParse(parts[parts.Length - 1], out int index))
                return index;
            return 0;
        }
        
        private bool IsColorMatch(DomainColor a, DomainColor b, float tolerance = 0.05f)
        {
            return Mathf.Abs(a.R - b.R) < tolerance &&
                   Mathf.Abs(a.G - b.G) < tolerance &&
                   Mathf.Abs(a.B - b.B) < tolerance &&
                   Mathf.Abs(a.A - b.A) < tolerance;
        }
    }
}
