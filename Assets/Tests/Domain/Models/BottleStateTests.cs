using NUnit.Framework;
using BottleShaders.Domain.Models;
using UnityEngine;
using System.Linq;

namespace BottleShaders.Domain.Tests.Models
{
    public class BottleStateTests
    {
        private BottleState CreateSut(int maxLayers = 4)
        {
            return new BottleState(maxLayers);
        }

        private LiquidLayer Layer(Color color, float amount = 0.25f)
        {
            return new LiquidLayer(color, amount);
        }

        // ── Initial state ───────────────────────────────────────────────────

        [Test]
        public void Constructor_SetsMaxLayers()
        {
            var bottle = CreateSut(5);

            Assert.That(bottle.MaxLayers, Is.EqualTo(5));
        }

        [Test]
        public void NewBottle_IsEmpty()
        {
            var bottle = CreateSut();

            Assert.That(bottle.IsEmpty, Is.True);
        }

        [Test]
        public void NewBottle_IsNotFull()
        {
            var bottle = CreateSut();

            Assert.That(bottle.IsFull, Is.False);
        }

        [Test]
        public void NewBottle_TotalFillIsZero()
        {
            var bottle = CreateSut();

            Assert.That(bottle.TotalFill, Is.EqualTo(0f));
        }

        [Test]
        public void NewBottle_TopLayerIsNull()
        {
            var bottle = CreateSut();

            Assert.That(bottle.TopLayer, Is.Null);
        }

        [Test]
        public void NewBottle_LayersIsEmpty()
        {
            var bottle = CreateSut();

            Assert.That(bottle.Layers, Is.Empty);
        }

        // ── AddLayer ────────────────────────────────────────────────────────

        [Test]
        public void AddLayer_AddsLayerToBottle()
        {
            var bottle = CreateSut();
            var layer = Layer(Color.red);

            bool added = bottle.AddLayer(layer);

            Assert.That(added, Is.True);
            Assert.That(bottle.Layers.Count, Is.EqualTo(1));
            Assert.That(bottle.TopLayer, Is.EqualTo(layer));
        }

        [Test]
        public void AddLayer_MultipleLayers_AreInOrder()
        {
            var bottle = CreateSut();
            var red = Layer(Color.red);
            var blue = Layer(Color.blue, 0.30f);
            var green = Layer(Color.green, 0.20f);

            bottle.AddLayer(red);
            bottle.AddLayer(blue);
            bottle.AddLayer(green);

            Assert.That(bottle.Layers.Count, Is.EqualTo(3));
            Assert.That(bottle.Layers[0].Color, Is.EqualTo(Color.red));
            Assert.That(bottle.Layers[1].Color, Is.EqualTo(Color.blue));
            Assert.That(bottle.Layers[2].Color, Is.EqualTo(Color.green));
        }

        [Test]
        public void AddLayer_WhenFull_ReturnsFalse()
        {
            var bottle = CreateSut(2);
            bottle.AddLayer(Layer(Color.red));
            bottle.AddLayer(Layer(Color.blue));

            bool added = bottle.AddLayer(Layer(Color.green));

            Assert.That(added, Is.False);
            Assert.That(bottle.Layers.Count, Is.EqualTo(2));
        }

        [Test]
        public void AddLayer_UpdatesTotalFill()
        {
            var bottle = CreateSut();
            bottle.AddLayer(Layer(Color.red, 0.25f));
            bottle.AddLayer(Layer(Color.blue, 0.35f));

            Assert.That(bottle.TotalFill, Is.EqualTo(0.60f).Within(0.001f));
        }

        [Test]
        public void IsFull_WhenMaxLayersAndFullFill_ReturnsTrue()
        {
            var bottle = CreateSut(4);
            bottle.AddLayer(Layer(Color.red, 0.25f));
            bottle.AddLayer(Layer(Color.red, 0.25f));
            bottle.AddLayer(Layer(Color.blue, 0.25f));
            bottle.AddLayer(Layer(Color.blue, 0.25f));

            Assert.That(bottle.IsFull, Is.True);
        }

        [Test]
        public void IsFull_WhenMaxLayersAndFillAboveThreshold_ReturnsTrue()
        {
            var bottle = CreateSut(4);
            bottle.AddLayer(Layer(Color.red, 0.25f));
            bottle.AddLayer(Layer(Color.red, 0.25f));
            bottle.AddLayer(Layer(Color.blue, 0.25f));
            bottle.AddLayer(Layer(Color.blue, 0.25f));

            Assert.That(bottle.IsFull, Is.True);
        }

        [Test]
        public void IsFull_WhenNotMaxLayers_ReturnsFalse()
        {
            var bottle = CreateSut(4);
            bottle.AddLayer(Layer(Color.red, 0.25f));
            bottle.AddLayer(Layer(Color.blue, 0.25f));

            Assert.That(bottle.IsFull, Is.False);
        }

        // ── PopTopLayer ─────────────────────────────────────────────────────

        [Test]
        public void PopTopLayer_FromEmpty_ReturnsNull()
        {
            var bottle = CreateSut();

            var layer = bottle.PopTopLayer();

            Assert.That(layer, Is.Null);
        }

        [Test]
        public void PopTopLayer_RemovesAndReturnsTopLayer()
        {
            var bottle = CreateSut();
            var red = Layer(Color.red);
            var blue = Layer(Color.blue);
            bottle.AddLayer(red);
            bottle.AddLayer(blue);

            var popped = bottle.PopTopLayer();

            Assert.That(popped, Is.EqualTo(blue));
            Assert.That(bottle.Layers.Count, Is.EqualTo(1));
            Assert.That(bottle.Layers[0], Is.EqualTo(red));
        }

        [Test]
        public void PopTopLayer_UpdatesTotalFill()
        {
            var bottle = CreateSut();
            bottle.AddLayer(Layer(Color.red, 0.25f));
            bottle.AddLayer(Layer(Color.blue, 0.35f));
            Assert.That(bottle.TotalFill, Is.EqualTo(0.60f).Within(0.001f));

            bottle.PopTopLayer();

            Assert.That(bottle.TotalFill, Is.EqualTo(0.25f).Within(0.001f));
        }

        [Test]
        public void PopTopLayer_AllLayers_BecomesEmpty()
        {
            var bottle = CreateSut();
            bottle.AddLayer(Layer(Color.red));
            bottle.AddLayer(Layer(Color.blue));

            bottle.PopTopLayer();
            bottle.PopTopLayer();

            Assert.That(bottle.IsEmpty, Is.True);
            Assert.That(bottle.TopLayer, Is.Null);
        }

        // ── Clear ───────────────────────────────────────────────────────────

        [Test]
        public void Clear_RemovesAllLayers()
        {
            var bottle = CreateSut();
            bottle.AddLayer(Layer(Color.red));
            bottle.AddLayer(Layer(Color.blue));
            bottle.AddLayer(Layer(Color.green));

            bottle.Clear();

            Assert.That(bottle.IsEmpty, Is.True);
            Assert.That(bottle.TotalFill, Is.EqualTo(0f));
        }

        // ── IsEmpty / IsFull edge cases ─────────────────────────────────────

        [Test]
        public void IsEmpty_AfterAddThenPopAll_ReturnsTrue()
        {
            var bottle = CreateSut();
            bottle.AddLayer(Layer(Color.red));
            bottle.PopTopLayer();

            Assert.That(bottle.IsEmpty, Is.True);
        }

        [Test]
        public void IsFull_ExactMaxLayersAndFill_ReturnsTrue()
        {
            var bottle = CreateSut(3);
            bottle.AddLayer(Layer(Color.red, 0.33f));
            bottle.AddLayer(Layer(Color.blue, 0.33f));
            bottle.AddLayer(Layer(Color.green, 0.34f));

            Assert.That(bottle.IsFull, Is.True);
        }

        [Test]
        public void Layers_ImplementsReadOnlyInterface()
        {
            var bottle = CreateSut();
            bottle.AddLayer(Layer(Color.red));
            bottle.AddLayer(Layer(Color.blue));

            var layers = bottle.Layers;

            Assert.That(layers, Is.InstanceOf<System.Collections.Generic.IReadOnlyList<LiquidLayer>>());
            Assert.That(layers, Is.InstanceOf<System.Collections.Generic.IReadOnlyCollection<LiquidLayer>>());
            Assert.That(layers.Count, Is.EqualTo(2));
        }

        [Test]
        public void ToString_ReturnsExpectedFormat()
        {
            var bottle = CreateSut(4);
            bottle.AddLayer(Layer(Color.red, 0.25f));

            string result = bottle.ToString();

            Assert.That(result, Does.Contain("BottleState"));
            Assert.That(result, Does.Contain("1/4"));
        }
    }
}