using System;
using System.Collections.Generic;
using PuzzleGame.Domain.Models;
using PuzzleGame.Application.Configuration.FeatureSystem;
using PuzzleGame.Application.Events;
using PuzzleGame.Application.Logging;
using PuzzleGame.Application.Interfaces;

namespace PuzzleGame.Application.Services
{
    /// <summary>
    /// Handles chemical reactions between colors after Casts.
    /// Supports:
    /// - Explosion: Mold disappears, creating empty space
    /// - Transform: Colors convert to new color
    /// </summary>
    public class ReactionService : IReactionService
    {
        private readonly IColorAdapter _colorAdapter;
        private readonly IEventAggregator _eventAggregator;

        public ReactionService(IColorAdapter colorAdapter, IEventAggregator eventAggregator)
        {
            _colorAdapter = colorAdapter ?? throw new ArgumentNullException(nameof(colorAdapter));
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
        }

        public int CheckReactions(IMoldView[] Molds, ReactionSystemData config)
        {
            if (Molds == null) throw new ArgumentNullException(nameof(Molds));
            if (config == null || !config.enableReactions) return 0;

            int count = 0;
            foreach (var Mold in Molds)
            {
                if (Mold == null) continue;
                if (CheckMoldReactions(Mold, config, out ReactionResult result))
                {
                    count++;
                    ProcessReactionResult(Mold, result);
                }
            }

            return count;
        }

        private bool CheckMoldReactions(IMoldView Mold, ReactionSystemData config, out ReactionResult result)
        {
            result = default;
            var state = Mold.State;
            if (state.LayerCount < 2) return false;

            for (int i = 0; i < state.LayerCount - 1; i++)
            {
                OreLayer layerA;
                OreLayer layerB;
                try
                {
                    layerA = state.GetLayerAt(i);
                    layerB = state.GetLayerAt(i + 1);
                }
                catch (ArgumentOutOfRangeException)
                {
                    throw new InvalidOperationException(
                        $"ReactionService: Mold '{Mold.GameObject.name}' state mutated during reaction scan.");
                }

                var colorTypeA = layerA.ColorType;
                var colorTypeB = layerB.ColorType;

                if (colorTypeA == OreColor.None || colorTypeB == OreColor.None)
                    continue;

                foreach (var rule in config.reactionRules)
                {
                    if (IsColorMatch(colorTypeA, colorTypeB, rule))
                    {
                        result = new ReactionResult
                        {
                            MoldIndex = GetMoldIndex(Mold),
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

        private bool IsColorMatch(OreColor a, OreColor b, ReactionRule rule)
        {
            return (a == rule.colorA && b == rule.colorB) ||
                   (a == rule.colorB && b == rule.colorA);
        }

        private void ProcessReactionResult(IMoldView Mold, ReactionResult result)
        {
            switch (result.Rule.reactionType)
            {
                case ReactionRule.ReactionType.Explode:
                    ProcessExplosion(Mold, result);
                    break;

                case ReactionRule.ReactionType.Transform:
                    ProcessTransform(Mold, result);
                    break;

                case ReactionRule.ReactionType.Bubble:
                    ProcessBubble(Mold, result);
                    break;
            }
        }

        private void ProcessExplosion(IMoldView Mold, ReactionResult result)
        {
            var position = Mold.Transform.position;

            _eventAggregator.Publish(new MoldExplodedEvent(
                result.MoldIndex,
                position));

            Mold.State.Clear();

            MoldLogger.LogInfo($"[ReactionService] EXPLOSION at {Mold.GameObject.name}! Mold emptied.");
        }

        private void ProcessTransform(IMoldView Mold, ReactionResult result)
        {
            var state = Mold.State;

            var resultColor = result.Rule.resultColor;
            var domainColor = (DomainColor)resultColor.ToDefaultDomainColor();

            int layerIndexA = result.AffectedLayerA;
            int layerIndexB = result.AffectedLayerB;

            try
            {
                state.ReplaceAtIndex(layerIndexA, new OreLayer(domainColor, 1f, resultColor));
                state.RemoveAtIndex(layerIndexB);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                throw new InvalidOperationException(
                    $"ReactionService.ProcessTransform: layer index out of range for Mold '{Mold.GameObject.name}'.",
                    ex);
            }

            _eventAggregator.Publish(new ReactionTriggeredEvent(
                result.MoldIndex,
                ReactionRule.ReactionType.Transform,
                _colorAdapter.ToUnity(resultColor.ToDefaultDomainColor()),
                _colorAdapter.ToUnity(resultColor.ToDefaultDomainColor())));

            MoldLogger.LogInfo($"[ReactionService] TRANSFORM at {Mold.GameObject.name}: colors combined into {resultColor}");
        }

        private void ProcessBubble(IMoldView Mold, ReactionResult result)
        {
            _eventAggregator.Publish(new ReactionTriggeredEvent(
                result.MoldIndex,
                ReactionRule.ReactionType.Bubble,
                _colorAdapter.ToUnity(result.Rule.colorA.ToDefaultDomainColor()),
                _colorAdapter.ToUnity(result.Rule.colorB.ToDefaultDomainColor())));

            MoldLogger.LogDebug($"[ReactionService] BUBBLE at {Mold.GameObject.name}.");
        }

        // Fix Code Quality #5: Use IMoldView.MoldIndex instead of fragile GameObject.name parsing.
        private static int GetMoldIndex(IMoldView Mold) => Mold.MoldIndex;
    }

    public struct ReactionResult
    {
        public int MoldIndex { get; set; }
        public ReactionRule Rule { get; set; }
        public int AffectedLayerA { get; set; }
        public int AffectedLayerB { get; set; }
    }
}
