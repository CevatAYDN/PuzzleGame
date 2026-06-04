using PuzzleGame.Domain.Models;

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

        public LevelCompletedEvent(int moveCount)
        {
            MoveCount = moveCount;
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
    /// </summary>
    public readonly struct VFXStatusEvent
    {
        public int ComponentId { get; }
        public bool IsActive { get; }
        public float Intensity { get; }
        public int ParticleCount { get; }
        public string Status { get; }

        public VFXStatusEvent(int componentId, bool isActive, float intensity, int particleCount, string status)
        {
            ComponentId = componentId;
            IsActive = isActive;
            Intensity = intensity;
            ParticleCount = particleCount;
            Status = status ?? "unknown";
        }

        public static VFXStatusEvent Missing(int componentId)
        {
            return new VFXStatusEvent(componentId, false, 0f, 0, "missing");
        }

        public static VFXStatusEvent Active(int componentId, float intensity, int particleCount)
        {
            return new VFXStatusEvent(componentId, true, intensity, particleCount, "active");
        }
    }

    // S15 FIX: UndoPreCastEvent kaldırıldı.
    // Döküm öncesi undo snapshot kaydı artık InputHandlerService callback üzerinden
    // doğrudan GameHistoryManagementService'e yapılıyor (decoupled, type-safe).
}