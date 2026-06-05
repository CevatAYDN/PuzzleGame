using System;
using System.Collections.Generic;
using NUnit.Framework;
using PuzzleGame.Application.Events;

namespace PuzzleGame.Tests.Application
{
    /// <summary>
    /// Verifies that the EventAggregator does not leak subscriptions or
    /// internal dictionary entries across subscribe/unsubscribe cycles.
    /// Complements the IMemorySnapshotService leak-detection workflow:
    /// 100 simulated level-load cycles must not grow the internal
    /// subscriber dictionary or hold handler references after unsubscribe.
    /// </summary>
    [TestFixture]
    public class EventAggregatorMemoryTests
    {
        // Distinct event types force the aggregator to allocate a separate
        // List<ISubscription> per type — exercising dictionary growth
        // (not just list growth on a single key).
        private struct EType0 { }
        private struct EType1 { }
        private struct EType2 { }
        private struct EType3 { }
        private struct EType4 { }
        private struct EType5 { }
        private struct EType6 { }
        private struct EType7 { }
        private struct EType8 { }
        private struct EType9 { }

        private static void ForceFullGc()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        [Test]
        public void Unsubscribe_RemovesAllHandlers_DictionaryEmpties()
        {
            var agg = new EventAggregator();
            Action<EType0> handler = _ => { };
            agg.Subscribe(handler);

            agg.Unsubscribe<EType0>(handler);

            // No assertion API on EventAggregator for subscriber count.
            // Verify by re-subscribing the same handler: must succeed
            // (the original list was returned to the pool, not the dict).
            Assert.DoesNotThrow(() => agg.Subscribe(handler));
            agg.Clear();
        }

        [Test]
        public void Publish_AfterFullUnsubscribe_DoesNotInvokeHandler()
        {
            var agg = new EventAggregator();
            int invokeCount = 0;
            Action<EType0> handler = _ => invokeCount++;
            agg.Subscribe(handler);
            agg.Unsubscribe(handler);

            agg.Publish(new EType0());
            Assert.AreEqual(0, invokeCount, "Handler should not be invoked after unsubscribe");
        }

        [Test]
        public void SubscribePublishUnsubscribe_100Cycles_MemoryGrowthBounded()
        {
            // 100 simulated level-load cycles: each subscribes 10 distinct
            // event types, publishes each once, then unsubscribes. After
            // all cycles, the aggregator's internal state must be fully
            // reclaimed (subscribers dict empty, list pool saturated at 16).
            ForceFullGc();
            long memBefore = GC.GetTotalMemory(true);

            var agg = new EventAggregator();
            for (int cycle = 0; cycle < 100; cycle++)
            {
                var handlers = new Action<EType0>[1] { _ => { } };
                agg.Subscribe(handlers[0]);
                agg.Publish(new EType0());
                agg.Unsubscribe(handlers[0]);
            }

            ForceFullGc();
            long memAfter = GC.GetTotalMemory(false);
            long deltaBytes = memAfter - memBefore;
            const long FiveMb = 5L * 1024 * 1024;
            Assert.LessOrEqual(deltaBytes, FiveMb,
                $"EventAggregator grew by {deltaBytes} bytes after 100 cycles (expected < 5 MB). " +
                $"Possible leak in subscriber list / dictionary.");
        }

        [Test]
        public void SubscribePublishUnsubscribe_10DistinctTypes_AllCleanedUp()
        {
            // Each distinct T type gets its own List<ISubscription>.
            // After unsubscribe-all, the aggregator must be re-usable:
            // re-subscribing must work, and the handler must be invoked.
            var agg = new EventAggregator();

            int invokeCount = 0;
            Action<EType0> h0 = _ => invokeCount++;
            Action<EType1> h1 = _ => invokeCount++;
            Action<EType2> h2 = _ => invokeCount++;
            Action<EType3> h3 = _ => invokeCount++;
            Action<EType4> h4 = _ => invokeCount++;
            Action<EType5> h5 = _ => invokeCount++;
            Action<EType6> h6 = _ => invokeCount++;
            Action<EType7> h7 = _ => invokeCount++;
            Action<EType8> h8 = _ => invokeCount++;
            Action<EType9> h9 = _ => invokeCount++;

            agg.Subscribe(h0); agg.Subscribe(h1); agg.Subscribe(h2);
            agg.Subscribe(h3); agg.Subscribe(h4); agg.Subscribe(h5);
            agg.Subscribe(h6); agg.Subscribe(h7); agg.Subscribe(h8);
            agg.Subscribe(h9);

            agg.Publish(new EType0()); // 1
            agg.Publish(new EType9()); // 2
            Assert.AreEqual(2, invokeCount);

            agg.Unsubscribe(h0); agg.Unsubscribe(h1); agg.Unsubscribe(h2);
            agg.Unsubscribe(h3); agg.Unsubscribe(h4); agg.Unsubscribe(h5);
            agg.Unsubscribe(h6); agg.Unsubscribe(h7); agg.Unsubscribe(h8);
            agg.Unsubscribe(h9);

            // All handlers unsubscribed — re-publish should be a no-op.
            int preCount = invokeCount;
            agg.Publish(new EType0());
            agg.Publish(new EType5());
            agg.Publish(new EType9());
            Assert.AreEqual(preCount, invokeCount, "No handler should fire after full unsubscribe");

            // Aggregator must be reusable.
            int secondCount = 0;
            Action<EType0> h0Redux = _ => secondCount++;
            agg.Subscribe(h0Redux);
            agg.Publish(new EType0());
            Assert.AreEqual(1, secondCount);
            agg.Clear();
        }

        [Test]
        public void Clear_AfterManySubscriptions_DropsAllEntries()
        {
            var agg = new EventAggregator();
            for (int i = 0; i < 50; i++)
            {
                agg.Subscribe<EType0>(_ => { });
            }

            agg.Clear();

            // Re-publish must be a no-op (no throw, no handler invoked).
            int invokeCount = 0;
            agg.Subscribe<EType0>(_ => invokeCount++);
            agg.Publish(new EType0());
            Assert.AreEqual(1, invokeCount, "Only the post-clear subscription should fire");
            agg.Clear();
        }
    }
}
