using System;

namespace PuzzleGame.Application.Interfaces
{
    /// <summary>
    /// Captures and compares runtime memory snapshots. Used for leak
    /// detection (e.g. after N level loads) and baseline tracking.
    /// No-op friendly: callers can detect missing/zero values and fall
    /// back to log-only mode.
    /// </summary>
    public interface IMemorySnapshotService
    {
        /// <summary>
        /// Capture the current process memory state. Returns a struct
        /// snapshot — safe to store in a local variable.
        /// </summary>
        MemorySnapshot Capture();

        /// <summary>
        /// Compare two snapshots and return a diff with verdict. Verdict
        /// thresholds come from <see cref="PuzzleGame.Domain.ForgeConstants"/>.
        /// </summary>
        MemorySnapshotDiff Compare(MemorySnapshot baseline, MemorySnapshot current);
    }

    /// <summary>
    /// Single-point memory measurement. Readonly struct for value semantics
    /// and immutability — every capture is a snapshot in time.
    /// </summary>
    public readonly struct MemorySnapshot
    {
        /// <summary>Total reserved bytes (managed + native + graphics).
        /// Source: <c>UnityEngine.Profiling.Profiler.GetTotalReservedMemoryLong</c>.</summary>
        public long TotalReservedBytes { get; }

        /// <summary>Total allocated bytes (currently in use).
        /// Source: <c>UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong</c>.</summary>
        public long TotalAllocatedBytes { get; }

        /// <summary>Mono heap used bytes.
        /// Source: <c>UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong</c>.</summary>
        public long MonoUsedBytes { get; }

        /// <summary>Managed (GC) heap bytes.
        /// Source: <c>System.GC.GetTotalMemory(false)</c>.</summary>
        public long GcUsedBytes { get; }

        /// <summary>UTC timestamp when the snapshot was captured.</summary>
        public DateTime CapturedAt { get; }

        public MemorySnapshot(
            long totalReservedBytes,
            long totalAllocatedBytes,
            long monoUsedBytes,
            long gcUsedBytes,
            DateTime capturedAt)
        {
            TotalReservedBytes = totalReservedBytes;
            TotalAllocatedBytes = totalAllocatedBytes;
            MonoUsedBytes = monoUsedBytes;
            GcUsedBytes = gcUsedBytes;
            CapturedAt = capturedAt;
        }
    }

    /// <summary>
    /// Verdict for a memory delta. Calibrated against
    /// <see cref="PuzzleGame.Domain.ForgeConstants.MemoryWarningDeltaBytes"/>
    /// and <see cref="PuzzleGame.Domain.ForgeConstants.MemoryCriticalDeltaBytes"/>.
    /// </summary>
    public enum MemoryHealth
    {
        Normal,
        Warning,
        Critical
    }

    /// <summary>
    /// Result of comparing a baseline snapshot against a current snapshot.
    /// Stores per-metric deltas plus a verdict computed against thresholds.
    /// </summary>
    public readonly struct MemorySnapshotDiff
    {
        public MemorySnapshot Baseline { get; }
        public MemorySnapshot Current { get; }
        public long DeltaTotalReservedBytes { get; }
        public long DeltaTotalAllocatedBytes { get; }
        public long DeltaMonoUsedBytes { get; }
        public long DeltaGcUsedBytes { get; }
        public MemoryHealth Verdict { get; }

        public MemorySnapshotDiff(
            MemorySnapshot baseline,
            MemorySnapshot current,
            long warningThresholdBytes,
            long criticalThresholdBytes)
        {
            Baseline = baseline;
            Current = current;
            DeltaTotalReservedBytes = current.TotalReservedBytes - baseline.TotalReservedBytes;
            DeltaTotalAllocatedBytes = current.TotalAllocatedBytes - baseline.TotalAllocatedBytes;
            DeltaMonoUsedBytes = current.MonoUsedBytes - baseline.MonoUsedBytes;
            DeltaGcUsedBytes = current.GcUsedBytes - baseline.GcUsedBytes;
            Verdict = ComputeVerdict(DeltaTotalAllocatedBytes, warningThresholdBytes, criticalThresholdBytes);
        }

        private static MemoryHealth ComputeVerdict(
            long deltaAllocated,
            long warningThreshold,
            long criticalThreshold)
        {
            // Negative deltas (GC reclaimed memory) are always Normal.
            if (deltaAllocated < 0) return MemoryHealth.Normal;
            if (deltaAllocated >= criticalThreshold) return MemoryHealth.Critical;
            if (deltaAllocated >= warningThreshold) return MemoryHealth.Warning;
            return MemoryHealth.Normal;
        }
    }
}
