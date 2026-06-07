using NUnit.Framework;
using PuzzleGame.Application.Events;
using PuzzleGame.Domain.Models;
using PuzzleGame.Application.Logging;
using UnityEngine;

namespace PuzzleGame.Events.Tests
{
    public class EventAggregatorTests
    {
        private EventAggregator _eventAggregator;

        [SetUp]
        public void SetUp()
        {
            _eventAggregator = new EventAggregator();
            MoldLogger.SetLevel(MoldLogger.Level.Error, false);
        }

        [TearDown]
        public void TearDown()
        {
            MoldLogger.SetLevel(MoldLogger.Level.Error, false);
            _eventAggregator?.Clear();
        }

        // ── Subscribe / Publish ─────────────────────────────────────────────

        [Test]
        public void SubscribeAndPublish_HandlerIsCalled()
        {
            bool called = false;
            _eventAggregator.Subscribe<TestEvent>(e => called = true);

            _eventAggregator.Publish(new TestEvent());

            Assert.That(called, Is.True);
        }

        [Test]
        public void SubscribeAndPublish_HandlerReceivesCorrectData()
        {
            TestEvent received = default;
            _eventAggregator.Subscribe<TestEvent>(e => received = e);

            var sent = new TestEvent { Value = 42 };
            _eventAggregator.Publish(sent);

            Assert.That(received.Value, Is.EqualTo(42));
        }

        [Test]
        public void Publish_WithoutSubscribers_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _eventAggregator.Publish(new TestEvent()));
        }

        [Test]
        public void Subscribe_NullHandler_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _eventAggregator.Subscribe<TestEvent>(null));
        }

        // ── Multiple subscribers ────────────────────────────────────────────

        [Test]
        public void MultipleSubscribers_AllCalled()
        {
            int callCount = 0;
            _eventAggregator.Subscribe<TestEvent>(e => callCount++);
            _eventAggregator.Subscribe<TestEvent>(e => callCount++);

            _eventAggregator.Publish(new TestEvent());

            Assert.That(callCount, Is.EqualTo(2));
        }

        [Test]
        public void MultipleSubscribers_ThrowingOne_DoesNotAffectOthers()
        {
            int callCount = 0;
            _eventAggregator.Subscribe<TestEvent>(e => throw new System.Exception("Boom"));
            _eventAggregator.Subscribe<TestEvent>(e => callCount++);

            Assert.Throws<System.Exception>(() => _eventAggregator.Publish(new TestEvent()));

            Assert.That(callCount, Is.EqualTo(1));
        }

        // ── Concurrent publish (Unity is single-threaded, so this is a stress test) ──
        // Note: Lock removal in EventAggregator means this tests main-thread safety only.
        [Test]
        public void Publish_StressTest_DoesNotThrow()
        {
            int callCount = 0;
            const int iterations = 100;
            _eventAggregator.Subscribe<TestEvent>(e => callCount++);

            for (int i = 0; i < iterations; i++)
            {
                _eventAggregator.Publish(new TestEvent());
            }

            Assert.That(callCount, Is.EqualTo(iterations));
        }

        // ── Unsubscribe ─────────────────────────────────────────────────────

        [Test]
        public void Unsubscribe_HandlerIsNotCalled()
        {
            bool called = false;
            System.Action<TestEvent> handler = e => called = true;

            _eventAggregator.Subscribe(handler);
            _eventAggregator.Unsubscribe(handler);
            _eventAggregator.Publish(new TestEvent());

            Assert.That(called, Is.False);
        }

        [Test]
        public void Unsubscribe_NullHandler_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _eventAggregator.Unsubscribe<TestEvent>(null));
        }

        [Test]
        public void Unsubscribe_NonExistentHandler_DoesNotThrow()
        {
            System.Action<TestEvent> handler = e => { };

            Assert.DoesNotThrow(() => _eventAggregator.Unsubscribe(handler));
        }

        [Test]
        public void Unsubscribe_WhilePublishing_OtherHandlersStillCalled()
        {
            int callCount = 0;
            System.Action<TestEvent> selfRemoving = null;
            selfRemoving = e =>
            {
                _eventAggregator.Unsubscribe(selfRemoving);
            };
            System.Action<TestEvent> other = e => callCount++;

            _eventAggregator.Subscribe(selfRemoving);
            _eventAggregator.Subscribe(other);

            _eventAggregator.Publish(new TestEvent());

            Assert.That(callCount, Is.EqualTo(1));
        }

        // ── Clear ───────────────────────────────────────────────────────────

        [Test]
        public void Clear_RemovesAllSubscribers()
        {
            bool called = false;
            _eventAggregator.Subscribe<TestEvent>(e => called = true);

            _eventAggregator.Clear();
            _eventAggregator.Publish(new TestEvent());

            Assert.That(called, Is.False);
        }

        [Test]
        public void Clear_CanBeCalledMultipleTimes()
        {
            Assert.DoesNotThrow(() => _eventAggregator.Clear());
            Assert.DoesNotThrow(() => _eventAggregator.Clear());
        }

        // ── Type isolation ──────────────────────────────────────────────────

        [Test]
        public void DifferentEventTypes_AreIsolated()
        {
            bool testEventCalled = false;
            bool otherEventCalled = false;

            _eventAggregator.Subscribe<TestEvent>(e => testEventCalled = true);
            _eventAggregator.Subscribe<OtherTestEvent>(e => otherEventCalled = true);

            _eventAggregator.Publish(new TestEvent());

            Assert.That(testEventCalled, Is.True);
            Assert.That(otherEventCalled, Is.False);
        }

        // ── Game events integration ─────────────────────────────────────────

        [Test]
        public void CastCompletedEvent_CanBePublishedAndReceived()
        {
            CastCompletedEvent received = default;
            _eventAggregator.Subscribe<CastCompletedEvent>(e => received = e);

            var source = new MoldState(4);
            var target = new MoldState(4);
            _eventAggregator.Publish(new CastCompletedEvent(source, target));

            Assert.That(received.Source, Is.EqualTo(source));
            Assert.That(received.Target, Is.EqualTo(target));
        }


        [Test]
        public void LevelCompletedEvent_CanBePublishedAndReceived()
        {
            LevelCompletedEvent received = default;
            _eventAggregator.Subscribe<LevelCompletedEvent>(e => received = e);

            _eventAggregator.Publish(new LevelCompletedEvent(15));

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

        // ── SubscribeToken (Fix #9: IDisposable token-based subscription) ──

        [Test]
        public void SubscribeToken_HandlerIsCalled()
        {
            bool called = false;
            using (_eventAggregator.SubscribeToken<TestEvent>(e => called = true))
            {
                _eventAggregator.Publish(new TestEvent());
            }
            Assert.That(called, Is.True);
        }

        [Test]
        public void SubscribeToken_DisposeUnsubscribes()
        {
            bool called = false;
            var token = _eventAggregator.SubscribeToken<TestEvent>(e => called = true);
            token.Dispose();

            _eventAggregator.Publish(new TestEvent());
            Assert.That(called, Is.False);
        }

        [Test]
        public void SubscribeToken_DoubleDispose_DoesNotThrow()
        {
            var token = _eventAggregator.SubscribeToken<TestEvent>(e => { });
            token.Dispose();
            Assert.DoesNotThrow(() => token.Dispose());
        }

        [Test]
        public void SubscribeToken_UsingStatement_AutoUnsubscribes()
        {
            int callCount = 0;
            using (_eventAggregator.SubscribeToken<TestEvent>(e => callCount++))
            {
                _eventAggregator.Publish(new TestEvent());
                Assert.That(callCount, Is.EqualTo(1));
            }
            // After using block, subscription should be removed
            _eventAggregator.Publish(new TestEvent());
            Assert.That(callCount, Is.EqualTo(1)); // still 1, not 2
        }

        [Test]
        public void SubscribeToken_MultipleTokens_IndependentDispose()
        {
            int count1 = 0, count2 = 0;
            var token1 = _eventAggregator.SubscribeToken<TestEvent>(e => count1++);
            var token2 = _eventAggregator.SubscribeToken<TestEvent>(e => count2++);

            _eventAggregator.Publish(new TestEvent());
            Assert.That(count1, Is.EqualTo(1));
            Assert.That(count2, Is.EqualTo(1));

            token1.Dispose();
            _eventAggregator.Publish(new TestEvent());
            Assert.That(count1, Is.EqualTo(1)); // unsubscribed
            Assert.That(count2, Is.EqualTo(2)); // still active

            token2.Dispose();
        }

        // ── Clear<T> (Fix #26: Event-type bazlı cleanup) ───────────────────

        [Test]
        public void ClearGeneric_OnlyClearsSpecifiedType()
        {
            bool testCalled = false;
            bool otherCalled = false;
            _eventAggregator.Subscribe<TestEvent>(e => testCalled = true);
            _eventAggregator.Subscribe<OtherTestEvent>(e => otherCalled = true);

            _eventAggregator.Clear<TestEvent>();

            _eventAggregator.Publish(new TestEvent());
            _eventAggregator.Publish(new OtherTestEvent());

            Assert.That(testCalled, Is.False); // cleared
            Assert.That(otherCalled, Is.True); // still active
        }

        [Test]
        public void ClearGeneric_NonExistentType_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _eventAggregator.Clear<TestEvent>());
        }

        [Test]
        public void ClearGeneric_CanBeCalledAfterFullClear()
        {
            _eventAggregator.Subscribe<TestEvent>(e => { });
            _eventAggregator.Clear();
            Assert.DoesNotThrow(() => _eventAggregator.Clear<TestEvent>());
        }
    }
}