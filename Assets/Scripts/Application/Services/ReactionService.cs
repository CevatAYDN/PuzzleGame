using System;
using System.Collections.Generic;
using PuzzleGame.Domain.Models;
using PuzzleGame.Application.Configuration.FeatureSystem;
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
        /// <exception cref="ArgumentNullException">If bottles is null.</exception>
        List<ReactionResult> CheckReactions(IBottleView[] bottles, ReactionSystemData config);
    }

    public class ReactionService : IReactionService
    {
        public List<ReactionResult> CheckReactions(IBottleView[] bottles, ReactionSystemData config)
        {
            if (bottles == null) throw new ArgumentNullException(nameof(bottles));
            var results = new List<ReactionResult>();
            if (config == null || !config.enableReactions) return results;

            foreach (var bottle in bottles)
            {
                if (bottle == null) continue;
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

            for (int i = 0; i < state.LayerCount - 1; i++)
            {
                LiquidLayer layerA;
                LiquidLayer layerB;
                try
                {
                    layerA = state.GetLayerAt(i);
                    layerB = state.GetLayerAt(i + 1);
                }
                catch (ArgumentOutOfRangeException)
                {
                    // Defensive: LayerCount changed mid-iteration (shouldn't happen but fail loud).
                    throw new InvalidOperationException(
                        $"ReactionService: bottle '{bottle.GameObject.name}' state mutated during reaction scan.");
                }

                var colorTypeA = layerA.ColorType;
                var colorTypeB = layerB.ColorType;

                if (colorTypeA == LiquidColor.None || colorTypeB == LiquidColor.None)
                    continue;

                foreach (var rule in config.reactionRules)
                {
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

            EventAggregator.Publish(new BottleExplodedEvent(
                result.BottleIndex,
                position));

            bottle.State.Clear();

            BottleLogger.LogInfo($"[ReactionService] EXPLOSION at {bottle.GameObject.name}! Bottle emptied.");
        }

        private void ProcessTransform(IBottleView bottle, ReactionResult result)
        {
            var state = bottle.State;

            var resultColor = result.Rule.resultColor;
            var domainColor = (DomainColor)resultColor.ToDefaultColor();

            int layerIndexA = result.AffectedLayers[0];
            int layerIndexB = result.AffectedLayers[1];

            try
            {
                state.ReplaceAtIndex(layerIndexA, new LiquidLayer(domainColor, 1f, resultColor));
                state.RemoveAtIndex(layerIndexB);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                throw new InvalidOperationException(
                    $"ReactionService.ProcessTransform: layer index out of range for bottle '{bottle.GameObject.name}'.",
                    ex);
            }

            EventAggregator.Publish(new ReactionTriggeredEvent(
                result.BottleIndex,
                ReactionRule.ReactionType.Transform,
                resultColor.ToDefaultColor(),
                resultColor.ToDefaultColor()));

            BottleLogger.LogInfo($"[ReactionService] TRANSFORM at {bottle.GameObject.name}: colors combined into {resultColor}");
        }

        private void ProcessBubble(IBottleView bottle, ReactionResult result)
        {
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

    public class ReactionResult
    {
        public int BottleIndex { get; set; }
        public ReactionRule Rule { get; set; }
        public int[] AffectedLayers { get; set; }
    }
}
