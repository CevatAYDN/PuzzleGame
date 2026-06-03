using System;
using System.Collections.Generic;
using PuzzleGame.Domain.Models;
using PuzzleGame.Infrastructure;
using PuzzleGame.Application.Configuration.FeatureSystem;
using PuzzleGame.Application.Events;
using PuzzleGame.Application.Logging;
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
        /// Returns number of bottles that had reactions.
        /// </summary>
        /// <exception cref="ArgumentNullException">If bottles is null.</exception>
        int CheckReactions(IBottleView[] bottles, ReactionSystemData config);
    }

    public class ReactionService : IReactionService
    {
        public int CheckReactions(IBottleView[] bottles, ReactionSystemData config)
        {
            if (bottles == null) throw new ArgumentNullException(nameof(bottles));
            if (config == null || !config.enableReactions) return 0;

            int count = 0;
            foreach (var bottle in bottles)
            {
                if (bottle == null) continue;
                if (CheckBottleReactions(bottle, config, out ReactionResult result))
                {
                    count++;
                    ProcessReactionResult(bottle, result);
                }
            }

            return count;
        }

        private bool CheckBottleReactions(IBottleView bottle, ReactionSystemData config, out ReactionResult result)
        {
            result = default;
            var state = bottle.State;
            if (state.LayerCount < 2) return false;

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
                        result = new ReactionResult
                        {
                            BottleIndex = GetBottleIndex(bottle),
                            Rule = rule,
                            AffectedLayerA = i,
                            AffectedLayerB = i + 1
                        };
                        return true;
                    }
                }
            }

            return false;
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
            var domainColor = (DomainColor)resultColor.ToDefaultDomainColor();

            int layerIndexA = result.AffectedLayerA;
            int layerIndexB = result.AffectedLayerB;

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
                ColorAdapter.ToUnity(resultColor.ToDefaultDomainColor()),
                ColorAdapter.ToUnity(resultColor.ToDefaultDomainColor())));

            BottleLogger.LogInfo($"[ReactionService] TRANSFORM at {bottle.GameObject.name}: colors combined into {resultColor}");
        }

        private void ProcessBubble(IBottleView bottle, ReactionResult result)
        {
            EventAggregator.Publish(new ReactionTriggeredEvent(
                result.BottleIndex,
                ReactionRule.ReactionType.Bubble,
                ColorAdapter.ToUnity(result.Rule.colorA.ToDefaultDomainColor()),
                ColorAdapter.ToUnity(result.Rule.colorB.ToDefaultDomainColor())));

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

    public struct ReactionResult
    {
        public int BottleIndex { get; set; }
        public ReactionRule Rule { get; set; }
        public int AffectedLayerA { get; set; }
        public int AffectedLayerB { get; set; }
    }
}
