using System;
using UnityEngine;
using PuzzleGame.Domain.Models.FeatureSystem;

namespace PuzzleGame.Events
{
    // ═══════════════════════════════════════════════════════════════════════
    // MULTI-LAYER POUR EVENTS
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
    // REACTION SYSTEM EVENTS
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

    public readonly struct LevelFailedEvent
    {
        public string FailureReason { get; }

        public LevelFailedEvent(string reason)
        {
            FailureReason = reason;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // KEY AND LOCK EVENTS
    // ═══════════════════════════════════════════════════════════════════════

    public readonly struct KeyCollectedEvent
    {
        public string KeyId { get; }
        public int BottleIndex { get; }
        public int LayerIndex { get; }

        public KeyCollectedEvent(string keyId, int bottleIndex, int layerIndex)
        {
            KeyId = keyId;
            BottleIndex = bottleIndex;
            LayerIndex = layerIndex;
        }
    }

    public readonly struct LockUnlockedEvent
    {
        public string LockId { get; }
        public string RequiredKeyId { get; }
        public LockData.LockType LockType { get; }

        public LockUnlockedEvent(string lockId, string requiredKeyId, LockData.LockType type)
        {
            LockId = lockId;
            RequiredKeyId = requiredKeyId;
            LockType = type;
        }
    }

    public readonly struct SecretAreaRevealedEvent
    {
        public string AreaId { get; }
        public Vector3 Position { get; }

        public SecretAreaRevealedEvent(string areaId, Vector3 position)
        {
            AreaId = areaId;
            Position = position;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // BONUS OBJECTIVE EVENTS
    // ═══════════════════════════════════════════════════════════════════════

    public readonly struct ObjectiveProgressEvent
    {
        public string ObjectiveId { get; }
        public BonusObjectiveData.ObjectiveType ObjectiveType { get; }
        public int CurrentProgress { get; }
        public int TargetProgress { get; }

        public ObjectiveProgressEvent(string id, BonusObjectiveData.ObjectiveType type, int current, int target)
        {
            ObjectiveId = id;
            ObjectiveType = type;
            CurrentProgress = current;
            TargetProgress = target;
        }
    }

    public readonly struct ObjectiveCompletedEvent
    {
        public string ObjectiveId { get; }
        public int BonusStarsAwarded { get; }

        public ObjectiveCompletedEvent(string id, int bonusStars)
        {
            ObjectiveId = id;
            BonusStarsAwarded = bonusStars;
        }
    }
}
