using System;
using NUnit.Framework;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Domain;
using PuzzleGame.Infrastructure.Implementations;

namespace PuzzleGame.Tests.Infrastructure
{
    /// <summary>
    /// Validates <see cref="UnityMemorySnapshotService"/> against the
    /// Unity Profiler API. In Editor mode, <c>Profiler.GetTotalReservedMemoryLong</c>
    /// returns the editor's memory stats (non-zero, but noisy). Tests assert
    /// structural correctness rather than absolute values.
    /// </summary>
    [TestFixture]
    public class UnityMemorySnapshotServiceTests
    {
        [Test]
        public void Capture_ReturnsSnapshotWithTimestamp()
        {
            var service = new UnityMemorySnapshotService();
            var before = DateTime.UtcNow.AddSeconds(-1);
            var snapshot = service.Capture();
            var after = DateTime.UtcNow.AddSeconds(1);

            Assert.GreaterOrEqual(snapshot.CapturedAt, before, "CapturedAt must be >= test start");
            Assert.LessOrEqual(snapshot.CapturedAt, after, "CapturedAt must be <= test end");
        }

        [Test]
        public void Capture_AllocatedLessThanOrEqualReserved()
        {
            var service = new UnityMemorySnapshotService();
            var snapshot = service.Capture();

            // Unity invariant: allocated ≤ reserved.
            // In Editor, both are positive and follow this property.
            if (snapshot.TotalReservedBytes > 0)
            {
                Assert.LessOrEqual(snapshot.TotalAllocatedBytes, snapshot.TotalReservedBytes,
                    "Allocated must be ≤ reserved (Unity invariant)");
            }
        }

        [Test]
        public void Capture_MonoBytesAreNonNegative()
        {
            var service = new UnityMemorySnapshotService();
            var snapshot = service.Capture();

            Assert.GreaterOrEqual(snapshot.MonoUsedBytes, 0L);
            Assert.GreaterOrEqual(snapshot.GcUsedBytes, 0L);
            Assert.GreaterOrEqual(snapshot.TotalAllocatedBytes, 0L);
            Assert.GreaterOrEqual(snapshot.TotalReservedBytes, 0L);
        }

        [Test]
        public void Compare_IdenticalSnapshots_NormalVerdict()
        {
            var service = new UnityMemorySnapshotService();
            var baseline = service.Capture();
            var current = baseline; // value-type copy

            var diff = service.Compare(baseline, current);
            Assert.AreEqual(MemoryHealth.Normal, diff.Verdict);
            Assert.AreEqual(0, diff.DeltaTotalAllocatedBytes);
            Assert.AreEqual(0, diff.DeltaTotalReservedBytes);
            Assert.AreEqual(0, diff.DeltaMonoUsedBytes);
            Assert.AreEqual(0, diff.DeltaGcUsedBytes);
        }

        [Test]
        public void Compare_HugeDelta_VerdictCritical()
        {
            var service = new UnityMemorySnapshotService();
            // Construct synthetic snapshots to force Critical verdict without
            // depending on Editor memory behavior.
            var baseline = new MemorySnapshot(
                totalReservedBytes: 0,
                totalAllocatedBytes: 0,
                monoUsedBytes: 0,
                gcUsedBytes: 0,
                capturedAt: DateTime.UtcNow);
            var current = new MemorySnapshot(
                totalReservedBytes: 0,
                totalAllocatedBytes: ForgeConstants.MemoryCriticalDeltaBytes,
                monoUsedBytes: 0,
                gcUsedBytes: 0,
                capturedAt: DateTime.UtcNow);

            var diff = service.Compare(baseline, current);
            Assert.AreEqual(MemoryHealth.Critical, diff.Verdict);
        }
    }
}
