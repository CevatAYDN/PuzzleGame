using UnityEngine;
using PuzzleGame.Application.Configuration.FeatureSystem;

namespace PuzzleGame.Application.Events
{
    // ═══════════════════════════════════════════════════════════════════════
    // MULTI-LAYER POUR EVENTS  (published by PourService)
    // ═══════════════════════════════════════════════════════════════════════



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
