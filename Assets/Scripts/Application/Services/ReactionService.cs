using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using PuzzleGame.Domain.Models;
using PuzzleGame.Domain.Models.FeatureSystem;
using PuzzleGame.Events;
using PuzzleGame.Logging;
using PuzzleGame.Application.Interfaces;

namespace PuzzleGame.Application.Services
{
    /// <summary>
    /// Handles chemical reactions between colors after pours.
    /// Supports:
    /// - Explosion: Bottle disappears, creating empty space
    /// - Transform: Colors convert to new color
    /// </summary>
    public interface IReactionService
    {
        /// <summary>
        /// Check for reactions in all bottles after a pour.
        /// Returns list of bottles that had reactions.
        /// </summary>
        List<ReactionResult> CheckReactions(IBottleView[] bottles, ReactionSystemData config);
    }

    public class ReactionService : IReactionService
    {
        public List<ReactionResult> CheckReactions(IBottleView[] bottles, ReactionSystemData config)
        {
            var results = new List<ReactionResult>();
            
            if (bottles == null || config == null || !config.enableReactions)
                return results;
            
            foreach (var bottle in bottles)
            {
                var result = CheckBottleReactions(bottle, config);
                if (result != null)
                {
                    results.Add(result);
                    ProcessReactionResult(bottle, result);
                }
            }
            
            return results;
        }

        private ReactionResult CheckBottleReactions(IBottleView bottle, ReactionSystemData config)
        {
            var state = bottle.State;
            if (state.LayerCount < 2) return null;
            
            // Check consecutive layer pairs for reactions
            for (int i = 0; i < state.LayerCount - 1; i++)
            {
                var layerA = state.GetLayerAt(i);
                var layerB = state.GetLayerAt(i + 1);
                
                if (layerA == null || layerB == null) continue;
                
                // Use LiquidColor enum for exact matching (no tolerance issues!)
                var colorTypeA = layerA.Value.ColorType;
                var colorTypeB = layerB.Value.ColorType;
                
                // Skip if either color is None/unknown
                if (colorTypeA == LiquidColor.None || colorTypeB == LiquidColor.None)
                    continue;
                
                // Find matching rule
                foreach (var rule in config.reactionRules)
                {
                    // Check if colors match rule (order independent)
                    if (IsColorMatch(colorTypeA, colorTypeB, rule))
                    {
                        return new ReactionResult
                        {
                            BottleIndex = GetBottleIndex(bottle),
                            Rule = rule,
                            AffectedLayers = new[] { i, i + 1 }
                        };
                    }
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// Check if two LiquidColors match a reaction rule (order independent).
        /// </summary>
        private bool IsColorMatch(LiquidColor a, LiquidColor b, ReactionRule rule)
        {
            return (a == rule.colorA && b == rule.colorB) ||
                   (a == rule.colorB && b == rule.colorA);
        }

        private void ProcessReactionResult(IBottleView bottle, ReactionResult result)
        {
            switch (result.Rule.reactionType)
            {
                case ReactionRule.ReactionType.Explode:
                    ProcessExplosion(bottle, result);
                    break;
                    
                case ReactionRule.ReactionType.Transform:
                    ProcessTransform(bottle, result);
                    break;
                    
                case ReactionRule.ReactionType.Bubble:
                    ProcessBubble(bottle, result);
                    break;
            }
        }

        private void ProcessExplosion(IBottleView bottle, ReactionResult result)
        {
            var position = bottle.Transform.position;
            
            // Publish explosion event for animations/effects
            EventAggregator.Publish(new BottleExplodedEvent(
                result.BottleIndex,
                position));
            
            // Clear bottle contents (create empty space)
            bottle.State.Clear();
            
            BottleLogger.LogInfo($"[ReactionService] EXPLOSION at {bottle.GameObject.name}! Bottle emptied.");
        }

        private void ProcessTransform(IBottleView bottle, ReactionResult result)
        {
            var state = bottle.State;
            
            // Use LiquidColor enum directly - convert to DomainColor for LiquidLayer
            var resultColor = result.Rule.resultColor;
            var domainColor = (DomainColor)resultColor.ToDefaultColor();

            // Convert the two layers to the new color
            int layerIndexA = result.AffectedLayers[0];
            int layerIndexB = result.AffectedLayers[1];

            // Replace both layers with single layer of new color
            state.ReplaceAtIndex(layerIndexA, new LiquidLayer(domainColor, 1f, resultColor));
            state.RemoveAtIndex(layerIndexB);

            // Publish reaction event (convert resultColor to Unity Color for event)
            EventAggregator.Publish(new ReactionTriggeredEvent(
                result.BottleIndex,
                ReactionRule.ReactionType.Transform,
                resultColor.ToDefaultColor(),
                resultColor.ToDefaultColor()));

            BottleLogger.LogInfo($"[ReactionService] TRANSFORM at {bottle.GameObject.name}: colors combined into {resultColor}");
        }

        private void ProcessBubble(IBottleView bottle, ReactionResult result)
        {
            // Bubble effect - just visual, no gameplay change
            EventAggregator.Publish(new ReactionTriggeredEvent(
                result.BottleIndex,
                ReactionRule.ReactionType.Bubble,
                result.Rule.colorA.ToDefaultColor(),
                result.Rule.colorB.ToDefaultColor()));

            BottleLogger.LogDebug($"[ReactionService] BUBBLE at {bottle.GameObject.name}.");
        }

        private int GetBottleIndex(IBottleView bottle)
        {
            var name = bottle.GameObject.name;
            var parts = name.Split('_');
            if (parts.Length > 1 && int.TryParse(parts[parts.Length - 1], out int index))
                return index;
            return 0;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // HELPER CLASSES
    // ═══════════════════════════════════════════════════════════════════════

    public class ReactionResult
    {
        public int BottleIndex { get; set; }
        public ReactionRule Rule { get; set; }
        public int[] AffectedLayers { get; set; }
    }
}
