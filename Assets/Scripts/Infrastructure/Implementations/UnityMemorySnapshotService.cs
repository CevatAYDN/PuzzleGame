using System;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Domain;
using UnityEngine.Profiling;

namespace PuzzleGame.Infrastructure.Implementations
{
    /// <summary>
    /// Unity Profiler-backed memory snapshot service. Reads from
    /// <c>UnityEngine.Profiling.Profiler</c> (built-in, no package required)
    /// and <c>System.GC.GetTotalMemory</c>. Caller can later swap this for a
    /// <c>com.unity.memoryprofiler</c>-backed impl for deep snapshot diffs.
    /// </summary>
    public sealed class UnityMemorySnapshotService : IMemorySnapshotService
    {
        public MemorySnapshot Capture()
        {
            return new MemorySnapshot(
                totalReservedBytes: Profiler.GetTotalReservedMemoryLong(),
                totalAllocatedBytes: Profiler.GetTotalAllocatedMemoryLong(),
                monoUsedBytes: Profiler.GetMonoUsedSizeLong(),
                gcUsedBytes: GC.GetTotalMemory(false),
                capturedAt: DateTime.UtcNow
            );
        }

        public MemorySnapshotDiff Compare(MemorySnapshot baseline, MemorySnapshot current)
        {
            return new MemorySnapshotDiff(
                baseline,
                current,
                warningThresholdBytes: ForgeConstants.MemoryWarningDeltaBytes,
                criticalThresholdBytes: ForgeConstants.MemoryCriticalDeltaBytes
            );
        }
    }
}
