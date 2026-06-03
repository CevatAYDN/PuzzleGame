using UnityEngine;
using PuzzleGame.Application.Configuration.FeatureSystem;

namespace PuzzleGame.Events
{
    // ═══════════════════════════════════════════════════════════════════════
    // MULTI-LAYER POUR EVENTS  (published by PourService)
    // ═══════════════════════════════════════════════════════════════════════

    public readonly struct MultiLayerPourStartedEvent
    {
        public int SourceBottleIndex { get; }
        public int TargetBottleIndex { get; }
        public int LayerCount { get; }
        public Color PouringColor { get; }

        public MultiLayerPourStartedEvent(int sourceIndex, int targetIndex, int layerCount, Color color)
        {
            SourceBottleIndex = sourceIndex;
            TargetBottleIndex = targetIndex;
            LayerCount = layerCount;
            PouringColor = color;
        }
    }

    public readonly struct MultiLayerPourCompletedEvent
    {
        public int SourceBottleIndex { get; }
        public int TargetBottleIndex { get; }
        public int LayersPoured { get; }

        public MultiLayerPourCompletedEvent(int sourceIndex, int targetIndex, int layersPoured)
        {
            SourceBottleIndex = sourceIndex;
            TargetBottleIndex = targetIndex;
            LayersPoured = layersPoured;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // REACTION SYSTEM EVENTS  (published by ReactionService)
    // ═══════════════════════════════════════════════════════════════════════

    public readonly struct ReactionTriggeredEvent
    {
        public int BottleIndex { get; }
        public ReactionRule.ReactionType ReactionType { get; }
        public Color ColorA { get; }
        public Color ColorB { get; }

        public ReactionTriggeredEvent(int bottleIndex, ReactionRule.ReactionType type, Color colorA, Color colorB)
        {
            BottleIndex = bottleIndex;
            ReactionType = type;
            ColorA = colorA;
            ColorB = colorB;
        }
    }

    public readonly struct BottleExplodedEvent
    {
        public int BottleIndex { get; }
        public Vector3 ExplosionPosition { get; }

        public BottleExplodedEvent(int bottleIndex, Vector3 position)
        {
            BottleIndex = bottleIndex;
            ExplosionPosition = position;
        }
    }
}
