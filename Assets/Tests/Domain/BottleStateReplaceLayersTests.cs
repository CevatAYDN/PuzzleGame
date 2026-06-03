using NUnit.Framework;
using PuzzleGame.Domain.Services;
using PuzzleGame.Domain.Models;
using PuzzleGame.Domain.Interfaces;

namespace PuzzleGame.Tests.Domain
{
    public class BottleStateReplaceLayersTests
    {
        private BottleState _state;

        [SetUp]
        public void SetUp()
        {
            _state = new BottleState(maxLayers: 4);
        }

        [Test]
        public void ReplaceLayers_FiresOnLayersChangedEvent()
        {
            bool eventFired = false;
            _state.OnLayersChanged += s => eventFired = true;

            var newLayers = new[]
            {
                new LiquidLayer(new DomainColor(1, 0, 0), 0.5f)
            };
            _state.ReplaceLayers(newLayers);

            Assert.IsTrue(eventFired);
        }

        [Test]
        public void ReplaceLayers_DoesNotFireEvent_WhenInvalid()
        {
            bool eventFired = false;
            _state.OnLayersChanged += s => eventFired = true;

            // maxLayers'tan fazla katman — invalid
            var invalidLayers = new LiquidLayer[10];
            for (int i = 0; i < invalidLayers.Length; i++)
                invalidLayers[i] = new LiquidLayer(new DomainColor(0, 1, 0), 0.1f);

            Assert.Throws<System.ArgumentException>(() => _state.ReplaceLayers(invalidLayers));

            Assert.IsFalse(eventFired);
        }

        [Test]
        public void ReplaceLayers_DoesNotFireEvent_WhenNull()
        {
            bool eventFired = false;
            _state.OnLayersChanged += s => eventFired = true;

            Assert.Throws<System.ArgumentNullException>(() => _state.ReplaceLayers(null));

            Assert.IsFalse(eventFired);
        }

        [Test]
        public void ReplaceLayers_UpdatesStateCorrectly()
        {
            var newLayers = new[]
            {
                new LiquidLayer(new DomainColor(1, 0, 0), 0.25f),
                new LiquidLayer(new DomainColor(0, 1, 0), 0.25f)
            };
            _state.ReplaceLayers(newLayers);

            Assert.AreEqual(2, _state.Layers.Count);
            Assert.AreEqual(0.5f, _state.TotalFill);
        }

        [Test]
        public void MultipleSubscribers_AllReceiveEvent()
        {
            int subscriber1Calls = 0;
            int subscriber2Calls = 0;
            _state.OnLayersChanged += s => subscriber1Calls++;
            _state.OnLayersChanged += s => subscriber2Calls++;

            _state.ReplaceLayers(new[]
            {
                new LiquidLayer(new DomainColor(1, 0, 0), 0.5f)
            });

            Assert.AreEqual(1, subscriber1Calls);
            Assert.AreEqual(1, subscriber2Calls);
        }

        [Test]
        public void Unsubscribe_StopsEventDelivery()
        {
            int calls = 0;
            System.Action<BottleState> handler = s => calls++;
            _state.OnLayersChanged += handler;
            _state.OnLayersChanged -= handler;

            _state.ReplaceLayers(new[]
            {
                new LiquidLayer(new DomainColor(1, 0, 0), 0.5f)
            });

            Assert.AreEqual(0, calls);
        }
    }
}