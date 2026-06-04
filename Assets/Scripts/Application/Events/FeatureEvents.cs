using UnityEngine;
using PuzzleGame.Application.Configuration.FeatureSystem;

namespace PuzzleGame.Application.Events
{
    // ═══════════════════════════════════════════════════════════════════════
    // MULTI-LAYER Cast EVENTS  (published by CastService)
    // ═══════════════════════════════════════════════════════════════════════



    // ═══════════════════════════════════════════════════════════════════════
    // REACTION SYSTEM EVENTS  (published by ReactionService)
    // ═══════════════════════════════════════════════════════════════════════

    public readonly struct ReactionTriggeredEvent
    {
        public int MoldIndex { get; }
        public ReactionRule.ReactionType ReactionType { get; }
        public Color ColorA { get; }
        public Color ColorB { get; }

        public ReactionTriggeredEvent(int moldIndex, ReactionRule.ReactionType type, Color colorA, Color colorB)
        {
            MoldIndex = moldIndex;
            ReactionType = type;
            ColorA = colorA;
            ColorB = colorB;
        }
    }

    public readonly struct MoldExplodedEvent
    {
        public int MoldIndex { get; }
        public Vector3 ExplosionPosition { get; }

        public MoldExplodedEvent(int moldIndex, Vector3 position)
        {
            MoldIndex = moldIndex;
            ExplosionPosition = position;
        }
    }
}
