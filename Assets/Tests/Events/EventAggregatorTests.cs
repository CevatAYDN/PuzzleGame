using NUnit.Framework;
using PuzzleGame.Events;
using PuzzleGame.Domain.Models;
using PuzzleGame.Logging;
using UnityEngine;

namespace PuzzleGame.Events.Tests
{
    public class EventAggregatorTests
    {
        [SetUp]
        public void SetUp()
        {
            EventAggregator.Clear(); // Önce temizle (önceki testten kalma varsa)
            BottleLogger.SetLevel(BottleLogger.Level.Error, false);
        }

        [TearDown]
        public void TearDown()
        {
            BottleLogger.SetLevel(BottleLogger.Level.Error, false); // SetUp ile aynı
            EventAggregator.Clear();
        }

        // ── Subscribe / Publish ─────────────────────────────────────────────

        [Test]
        public void SubscribeAndPublish_HandlerIsCalled()
        {
            bool called = false;
            EventAggregator.Subscribe<TestEvent>(e => called = true);

            EventAggregator.Publish(new TestEvent());

            Assert.That(called, Is.True);
        }

        [Test]
        public void SubscribeAndPublish_HandlerReceivesCorrectData()
        {
            TestEvent received = default;
            EventAggregator.Subscribe<TestEvent>(e => received = e);

            var sent = new TestEvent { Value = 42 };
            EventAggregator.Publish(sent);

            Assert.That(received.Value, Is.EqualTo(42));
        }

        [Test]
        public void Publish_WithoutSubscribers_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => EventAggregator.Publish(new TestEvent()));
        }

        [Test]
        public void Subscribe_NullHandler_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => EventAggregator.Subscribe<TestEvent>(null));
        }

        // ── Multiple subscribers ────────────────────────────────────────────

        [Test]
        public void MultipleSubscribers_AllCalled()
        {
            int callCount = 0;
            EventAggregator.Subscribe<TestEvent>(e => callCount++);
            EventAggregator.Subscribe<TestEvent>(e => callCount++);

            EventAggregator.Publish(new TestEvent());

            Assert.That(callCount, Is.EqualTo(2));
        }

        [Test]
        public void MultipleSubscribers_ThrowingOne_DoesNotAffectOthers()
        {
            int callCount = 0;
            EventAggregator.Subscribe<TestEvent>(e => throw new System.Exception("Boom"));
            EventAggregator.Subscribe<TestEvent>(e => callCount++);

            EventAggregator.Publish(new TestEvent());

            Assert.That(callCount, Is.EqualTo(1));
        }

        // ── Unsubscribe ─────────────────────────────────────────────────────

        [Test]
        public void Unsubscribe_HandlerIsNotCalled()
        {
            bool called = false;
            System.Action<TestEvent> handler = e => called = true;

            EventAggregator.Subscribe(handler);
            EventAggregator.Unsubscribe(handler);
            EventAggregator.Publish(new TestEvent());

            Assert.That(called, Is.False);
        }

        [Test]
        public void Unsubscribe_NullHandler_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => EventAggregator.Unsubscribe<TestEvent>(null));
        }

        [Test]
        public void Unsubscribe_NonExistentHandler_DoesNotThrow()
        {
            System.Action<TestEvent> handler = e => { };

            Assert.DoesNotThrow(() => EventAggregator.Unsubscribe(handler));
        }

        [Test]
        public void Unsubscribe_WhilePublishing_OtherHandlersStillCalled()
        {
            int callCount = 0;
            System.Action<TestEvent> selfRemoving = null;
            selfRemoving = e =>
            {
                EventAggregator.Unsubscribe(selfRemoving);
            };
            System.Action<TestEvent> other = e => callCount++;

            EventAggregator.Subscribe(selfRemoving);
            EventAggregator.Subscribe(other);

            EventAggregator.Publish(new TestEvent());

            Assert.That(callCount, Is.EqualTo(1));
        }

        // ── Clear ───────────────────────────────────────────────────────────

        [Test]
        public void Clear_RemovesAllSubscribers()
        {
            bool called = false;
            EventAggregator.Subscribe<TestEvent>(e => called = true);

            EventAggregator.Clear();
            EventAggregator.Publish(new TestEvent());

            Assert.That(called, Is.False);
        }

        [Test]
        public void Clear_CanBeCalledMultipleTimes()
        {
            Assert.DoesNotThrow(() => EventAggregator.Clear());
            Assert.DoesNotThrow(() => EventAggregator.Clear());
        }

        // ── Type isolation ──────────────────────────────────────────────────

        [Test]
        public void DifferentEventTypes_AreIsolated()
        {
            bool testEventCalled = false;
            bool otherEventCalled = false;

            EventAggregator.Subscribe<TestEvent>(e => testEventCalled = true);
            EventAggregator.Subscribe<OtherTestEvent>(e => otherEventCalled = true);

            EventAggregator.Publish(new TestEvent());

            Assert.That(testEventCalled, Is.True);
            Assert.That(otherEventCalled, Is.False);
        }

        // ── Game events integration ─────────────────────────────────────────

        [Test]
        public void PourCompletedEvent_CanBePublishedAndReceived()
        {
            PourCompletedEvent received = default;
            EventAggregator.Subscribe<PourCompletedEvent>(e => received = e);

            var source = new BottleState(4);
            var target = new BottleState(4);
            EventAggregator.Publish(new PourCompletedEvent(source, target));

            Assert.That(received.Source, Is.EqualTo(source));
            Assert.That(received.Target, Is.EqualTo(target));
        }

        [Test]
        public void BottleSelectedEvent_CanBePublishedAndReceived()
        {
            BottleSelectedEvent received = default;
            EventAggregator.Subscribe<BottleSelectedEvent>(e => received = e);

            var bottle = new BottleState(4);
            EventAggregator.Publish(new BottleSelectedEvent(bottle));

            Assert.That(received.Bottle, Is.EqualTo(bottle));
        }

        [Test]
        public void LevelCompletedEvent_CanBePublishedAndReceived()
        {
            LevelCompletedEvent received = default;
            EventAggregator.Subscribe<LevelCompletedEvent>(e => received = e);

            EventAggregator.Publish(new LevelCompletedEvent(15));

            Assert.That(received.MoveCount, Is.EqualTo(15));
        }

        // ── Test event types ────────────────────────────────────────────────

        private struct TestEvent
        {
            public int Value { get; set; }
        }

        private struct OtherTestEvent
        {
            public string Data { get; set; }
        }
    }
}