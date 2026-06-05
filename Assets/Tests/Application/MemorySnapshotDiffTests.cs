using System;
using NUnit.Framework;
using PuzzleGame.Application.Interfaces;

namespace PuzzleGame.Tests.Application
{
    /// <summary>
    /// Unit tests for the pure-POCO <see cref="MemorySnapshotDiff"/> verdict
    /// and delta logic. No Unity / GC dependency — pure C# arithmetic.
    /// </summary>
    [TestFixture]
    public class MemorySnapshotDiffTests
    {
        private const long OneMb = 1024L * 1024;
        private const long Warning = 50 * OneMb;
        private const long Critical = 200 * OneMb;

        private static MemorySnapshot Snapshot(long allocated, long mono = 0, long reserved = 0, long gc = 0)
            => new MemorySnapshot(reserved, allocated, mono, gc, DateTime.UtcNow);

        [Test]
        public void Verdict_Normal_WhenDeltaAllocatedIsZero()
        {
            var baseline = Snapshot(allocated: 100 * OneMb);
            var current = Snapshot(allocated: 100 * OneMb);
            var diff = new MemorySnapshotDiff(baseline, current, Warning, Critical);
            Assert.AreEqual(MemoryHealth.Normal, diff.Verdict);
            Assert.AreEqual(0, diff.DeltaTotalAllocatedBytes);
        }

        [Test]
        public void Verdict_Normal_WhenDeltaBelowWarningThreshold()
        {
            var baseline = Snapshot(allocated: 100 * OneMb);
            var current = Snapshot(allocated: 100 * OneMb + (Warning - 1));
            var diff = new MemorySnapshotDiff(baseline, current, Warning, Critical);
            Assert.AreEqual(MemoryHealth.Normal, diff.Verdict);
        }

        [Test]
        public void Verdict_Warning_WhenDeltaAtWarningThreshold()
        {
            var baseline = Snapshot(allocated: 100 * OneMb);
            var current = Snapshot(allocated: 100 * OneMb + Warning);
            var diff = new MemorySnapshotDiff(baseline, current, Warning, Critical);
            Assert.AreEqual(MemoryHealth.Warning, diff.Verdict);
        }

        [Test]
        public void Verdict_Warning_WhenDeltaBetweenWarningAndCritical()
        {
            var baseline = Snapshot(allocated: 100 * OneMb);
            var current = Snapshot(allocated: 100 * OneMb + Warning + 1);
            var diff = new MemorySnapshotDiff(baseline, current, Warning, Critical);
            Assert.AreEqual(MemoryHealth.Warning, diff.Verdict);
        }

        [Test]
        public void Verdict_Critical_WhenDeltaAtCriticalThreshold()
        {
            var baseline = Snapshot(allocated: 100 * OneMb);
            var current = Snapshot(allocated: 100 * OneMb + Critical);
            var diff = new MemorySnapshotDiff(baseline, current, Warning, Critical);
            Assert.AreEqual(MemoryHealth.Critical, diff.Verdict);
        }

        [Test]
        public void Verdict_Critical_WhenDeltaAboveCriticalThreshold()
        {
            var baseline = Snapshot(allocated: 100 * OneMb);
            var current = Snapshot(allocated: 100 * OneMb + Critical + 500 * OneMb);
            var diff = new MemorySnapshotDiff(baseline, current, Warning, Critical);
            Assert.AreEqual(MemoryHealth.Critical, diff.Verdict);
        }

        [Test]
        public void Verdict_Normal_WhenDeltaNegative_GCReclaimedMemory()
        {
            var baseline = Snapshot(allocated: 100 * OneMb);
            var current = Snapshot(allocated: 50 * OneMb);
            var diff = new MemorySnapshotDiff(baseline, current, Warning, Critical);
            Assert.AreEqual(MemoryHealth.Normal, diff.Verdict, "Negative deltas (GC reclaimed) must be Normal");
            Assert.AreEqual(-50 * OneMb, diff.DeltaTotalAllocatedBytes);
        }

        [Test]
        public void Deltas_ComputeForAllMetrics()
        {
            var baseline = new MemorySnapshot(
                totalReservedBytes: 1000,
                totalAllocatedBytes: 800,
                monoUsedBytes: 600,
                gcUsedBytes: 400,
                capturedAt: DateTime.UtcNow);
            var current = new MemorySnapshot(
                totalReservedBytes: 1500,
                totalAllocatedBytes: 1100,
                monoUsedBytes: 700,
                gcUsedBytes: 500,
                capturedAt: DateTime.UtcNow);

            var diff = new MemorySnapshotDiff(baseline, current, Warning, Critical);
            Assert.AreEqual(500, diff.DeltaTotalReservedBytes);
            Assert.AreEqual(300, diff.DeltaTotalAllocatedBytes);
            Assert.AreEqual(100, diff.DeltaMonoUsedBytes);
            Assert.AreEqual(100, diff.DeltaGcUsedBytes);
        }

        [Test]
        public void Verdict_UsesAllocatedDelta_NotGcDelta()
        {
            // Large GC delta, but small allocated delta — must be Normal.
            var baseline = new MemorySnapshot(
                totalReservedBytes: 0, totalAllocatedBytes: 100,
                monoUsedBytes: 0, gcUsedBytes: 100, capturedAt: DateTime.UtcNow);
            var current = new MemorySnapshot(
                totalReservedBytes: 0, totalAllocatedBytes: 105,
                monoUsedBytes: 0, gcUsedBytes: 5000, capturedAt: DateTime.UtcNow);

            var diff = new MemorySnapshotDiff(baseline, current, Warning, Critical);
            Assert.AreEqual(MemoryHealth.Normal, diff.Verdict);
            Assert.AreEqual(4900, diff.DeltaGcUsedBytes);
            Assert.AreEqual(5, diff.DeltaTotalAllocatedBytes);
        }
    }
}
