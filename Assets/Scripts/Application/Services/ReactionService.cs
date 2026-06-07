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
            if (state == null || state.LayerCount < 2) return false;

            int lastIndex = state.LayerCount - 1;

            // Fix #6: Removed dead defensive checks `if (i < 0 || i >= state.LayerCount)`.
            // `i` is bounded by the loop header `for (int i = 0; i < lastIndex; i++)` —
            // it can never be negative, and `i < lastIndex` already implies
            // `i < state.LayerCount`. The `i+1` is similarly guaranteed by the
            // iteration bound. The checks obscured the real algorithm.
            for (int i = 0; i < lastIndex; i++)
            {
                var layerA = state.GetLayerAt(i);
                var layerB = state.GetLayerAt(i + 1);

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
                            AffectedLayerB = i + 1,
                            // Fix #17: carry designer-tuned transform amount into
                            // ProcessTransform instead of relying on a hardcoded
                            // local value there. Clamp defensively because serialized
                            // assets can be edited outside the Inspector.
                            TransformedLayerAmount = Clamp01(config.transformedLayerAmount)
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

            Mold.State.Clear();
            Mold.UpdateVisualsFromState();

            _eventAggregator.Publish(new MoldExplodedEvent(
                result.MoldIndex,
                position));

            MoldLogger.LogInfo($"[ReactionService] EXPLOSION at {Mold.GameObject.name}! Mold emptied.");
        }

        private void ProcessTransform(IMoldView Mold, ReactionResult result)
        {
            var state = Mold.State;
            if (state == null) return;

            var resultColor = result.Rule.resultColor;
            var domainColor = (DomainColor)resultColor.ToDefaultDomainColor();

            int layerIndexA = result.AffectedLayerA;
            int layerIndexB = result.AffectedLayerB;
            int layerCount = state.LayerCount;

            if (layerIndexA < 0 || layerIndexA >= layerCount) return;
            if (layerIndexB < 0 || layerIndexB >= layerCount) return;

            // Fix #17: Use the designer-tuned transform amount from
            // ReactionSystemData instead of deriving a potentially over-full
            // amount from the two input layers.
            float combinedAmount = result.TransformedLayerAmount;

            if (layerIndexA >= 0 && layerIndexA < layerCount)
            {
                state.ReplaceAtIndex(layerIndexA, new OreLayer(domainColor, combinedAmount, resultColor));
            }

            if (layerIndexB >= 0 && layerIndexB < state.LayerCount)
            {
                state.RemoveAtIndex(layerIndexB);
            }

            Mold.UpdateVisualsFromState();

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

        private static float Clamp01(float value)
        {
            if (value < 0f) return 0f;
            if (value > 1f) return 1f;
            return value;
        }
    }

    public struct ReactionResult
    {
        public int MoldIndex { get; set; }
        public ReactionRule Rule { get; set; }
        public int AffectedLayerA { get; set; }
        public int AffectedLayerB { get; set; }
        public float TransformedLayerAmount { get; set; }
    }
}
