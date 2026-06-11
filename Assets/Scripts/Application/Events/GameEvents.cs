using PuzzleGame.Domain.Models;
using PuzzleGame.Application.Interfaces;
using UnityEngine;

namespace PuzzleGame.Application.Events
{
    public readonly struct GameStateChangedEvent
    {
        public GameState Previous { get; }
        public GameState Current { get; }

        public GameStateChangedEvent(GameState previous, GameState current)
        {
            Previous = previous;
            Current = current;
        }
    }

    public readonly struct CastCompletedEvent
    {
        public MoldState Source { get; }
        public MoldState Target { get; }

        public CastCompletedEvent(MoldState source, MoldState target)
        {
            Source = source;
            Target = target;
        }
    }

    /// <summary>
    /// Published when a Cast attempt is rejected by validator or Cast logic.
    /// Reason is a stable string code (e.g. "validator_rejected", "no_matching_layers").
    /// </summary>
    public readonly struct CastRejectedEvent
    {
        public int SourceMoldIndex { get; }
        public int TargetMoldIndex { get; }
        public string Reason { get; }

        public CastRejectedEvent(int sourceIndex, int targetIndex, string reason)
        {
            SourceMoldIndex = sourceIndex;
            TargetMoldIndex = targetIndex;
            Reason = reason;
        }
    }

    public readonly struct LevelCompletedEvent
    {
        public int MoveCount { get; }
        public int Stars { get; }
        public float CompletionTimeSeconds { get; }

        public LevelCompletedEvent(int moveCount, int stars, float completionTimeSeconds = 0f)
        {
            MoveCount = moveCount;
            Stars = stars;
            CompletionTimeSeconds = completionTimeSeconds;
        }
    }

    /// <summary>
    /// Published when a pour operation encounters an error at the service level.
    /// Carries semantic error codes and indices for UI / debug overlay consumption.
    /// </summary>
    public readonly struct PourErrorEvent
    {
        /// <summary>Pool index of the source mold (or -1 if unknown).</summary>
        public int SourceIndex { get; }
        /// <summary>Pool index of the target mold (or -1 if unknown).</summary>
        public int TargetIndex { get; }
        /// <summary>Stable error code (e.g. "vfx_null", "validator_rejected", "pool_exhausted").</summary>
        public string ErrorCode { get; }
        /// <summary>Human-readable developer message.</summary>
        public string Message { get; }

        public PourErrorEvent(int sourceIndex, int targetIndex, string errorCode, string message)
        {
            SourceIndex = sourceIndex;
            TargetIndex = targetIndex;
            ErrorCode = errorCode ?? "unknown";
            Message = message ?? string.Empty;
        }
    }

    /// <summary>Published when debug mode is toggled on/off (e.g. via F2 overlay).</summary>
    public readonly struct DebugStateChangedEvent
    {
        public bool Enabled { get; }
        public DebugStateChangedEvent(bool enabled) => Enabled = enabled;
    }

    /// <summary>Published whenever a mold's state is mutated (layer add/remove/clear).</summary>
    public readonly struct MoldStateMutatedEvent
    {
        public int MoldIndex { get; }
        public MoldDebugState State { get; }
        public MoldStateMutatedEvent(int moldIndex, MoldDebugState state)
        {
            MoldIndex = moldIndex;
            State = state;
        }
    }

    /// <summary>
    /// Published by StreamRenderer to report VFX component status.
    /// Throttled — only emitted every 30 frames to avoid event spam.
    /// ComponentId is EntityId (Unity 6 stable identifier), replacing the deprecated
    /// int GetInstanceID() — see InputHandlerService for the same migration rationale.
    /// </summary>
    public readonly struct VFXStatusEvent
    {
        public EntityId ComponentId { get; }
        public bool IsActive { get; }
        public float Intensity { get; }
        public int ParticleCount { get; }
        public string Status { get; }

        public VFXStatusEvent(EntityId componentId, bool isActive, float intensity, int particleCount, string status)
        {
            ComponentId = componentId;
            IsActive = isActive;
            Intensity = intensity;
            ParticleCount = particleCount;
            Status = status ?? "unknown";
        }

        public static VFXStatusEvent Missing(EntityId componentId)
        {
            return new VFXStatusEvent(componentId, false, 0f, 0, "missing");
        }

        public static VFXStatusEvent Active(EntityId componentId, float intensity, int particleCount)
        {
            return new VFXStatusEvent(componentId, true, intensity, particleCount, "active");
        }
    }

    // S15 FIX: UndoPreCastEvent kaldırıldı.
    // Döküm öncesi undo snapshot kaydı artık InputHandlerService callback üzerinden
    // doğrudan GameHistoryManagementService'e yapılıyor (decoupled, type-safe).

    /// <summary>
    /// Published by IAudioSettingsService when the player changes any audio
    /// preference (toggles, volumes, reset). Carries the new snapshot.
    /// </summary>
    public readonly struct AudioSettingsChangedEvent
    {
        public AudioPreferences NewSettings { get; }

        public AudioSettingsChangedEvent(AudioPreferences newSettings)
        {
            NewSettings = newSettings;
        }
    }

    /// <summary>
    /// Published by IPowerUpService when a power-up is activated.
    /// Gameplay systems (PourSystemController, MoldSpawner, etc.) listen
    /// and apply the effect.
    /// </summary>
    public readonly struct PowerUpActivatedEvent
    {
        public PowerUpType Type { get; }
        public int MoldIndex { get; }

        public PowerUpActivatedEvent(PowerUpType type, int moldIndex)
        {
            Type = type;
            MoldIndex = moldIndex;
        }
    }

    public readonly struct MoveCountUpdatedEvent
    {
        public int MovesUsed { get; }
        public int MovesRemaining { get; }
        public bool IsLimitedMode { get; }

        public MoveCountUpdatedEvent(int movesUsed, int movesRemaining, bool isLimitedMode)
        {
            MovesUsed = movesUsed;
            MovesRemaining = movesRemaining;
            IsLimitedMode = isLimitedMode;
        }
    }

    public readonly struct HintHighlightEvent
    {
        public int SourceIndex { get; }
        public int TargetIndex { get; }

        public HintHighlightEvent(int sourceIndex, int targetIndex)
        {
            SourceIndex = sourceIndex;
            TargetIndex = targetIndex;
        }
    }
}