using NUnit.Framework;
using PuzzleGame.Domain.Models;

namespace PuzzleGame.Domain.Tests.Models
{
    public class BottleStateTests
    {
        private BottleState CreateSut(int maxLayers = 4)
        {
            return new BottleState(maxLayers);
        }

        private LiquidLayer Layer(float r, float g, float b, float a = 1f, float amount = 0.25f)
        {
            return new LiquidLayer(new DomainColor(r, g, b, a), amount);
        }

        private DomainColor DC(float r, float g, float b, float a = 1f) => new DomainColor(r, g, b, a);

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

        [Test]
        public void AddLayer_AddsLayerToBottle()
        {
            var bottle = CreateSut();
            var layer = Layer(1f, 0f, 0f); // Red

            bottle.AddLayer(layer);

            Assert.That(bottle.Layers.Count, Is.EqualTo(1));
            var topLayer = bottle.Layers[bottle.Layers.Count - 1];
            Assert.That(topLayer.Color, Is.EqualTo(DC(1f, 0f, 0f)).Within(0.001f));
        }

        [Test]
        public void AddLayer_MultipleLayers_AreOrderedCorrectly()
        {
            var bottle = CreateSut();
            var red = Layer(1f, 0f, 0f);
            var blue = Layer(0f, 0f, 1f, 1f, 0.30f);
            var green = Layer(0f, 1f, 0f, 1f, 0.20f);

            bottle.AddLayer(red);
            bottle.AddLayer(blue);
            bottle.AddLayer(green);

            Assert.That(bottle.Layers.Count, Is.EqualTo(3));
            Assert.That(bottle.Layers[0].Color, Is.EqualTo(DC(1f, 0f, 0f)).Within(0.001f));
            Assert.That(bottle.Layers[1].Color, Is.EqualTo(DC(0f, 0f, 1f)).Within(0.001f));
            Assert.That(bottle.Layers[2].Color, Is.EqualTo(DC(0f, 1f, 0f)).Within(0.001f));
        }

        [Test]
        public void AddLayer_WhenFull_ThrowsException()
        {
            var bottle = CreateSut(2);
            bottle.AddLayer(Layer(1f, 0f, 0f));
            bottle.AddLayer(Layer(0f, 0f, 1f));

            Assert.Throws<System.InvalidOperationException>(() => bottle.AddLayer(Layer(0f, 1f, 0f)));
        }

        [Test]
        public void AddLayer_UpdatesTotalFill()
        {
            var bottle = CreateSut();
            bottle.AddLayer(Layer(1f, 0f, 0f, 1f, 0.25f));
            bottle.AddLayer(Layer(0f, 0f, 1f, 1f, 0.35f));
            Assert.That(bottle.TotalFill, Is.EqualTo(0.60f).Within(0.001f));
        }

        [Test]
        public void PopLayer_RemovesAndReturnsTopLayer()
        {
            var bottle = CreateSut();
            bottle.AddLayer(Layer(1f, 0f, 0f, 1f, 0.25f));
            bottle.AddLayer(Layer(1f, 0f, 0f, 1f, 0.25f));
            bottle.AddLayer(Layer(0f, 0f, 1f, 1f, 0.25f));
            bottle.AddLayer(Layer(0f, 0f, 1f, 1f, 0.25f));

            Assert.That(bottle.Layers.Count, Is.EqualTo(4));

            var popped = bottle.PopTopLayer();
            Assert.That(popped, Is.Not.Null);
            Assert.That(bottle.Layers.Count, Is.EqualTo(3));
        }

        [Test]
        public void PopLayer_WhenEmpty_ThrowsException()
        {
            var bottle = CreateSut();
            Assert.Throws<System.InvalidOperationException>(() => bottle.PopTopLayer());
        }

        [Test]
        public void TopLayer_AfterAddLayer_ReturnsLastAdded()
        {
            var bottle = CreateSut();
            var red = Layer(1f, 0f, 0f);
            var blue = Layer(0f, 0f, 1f);

            bottle.AddLayer(red);
            bottle.AddLayer(blue);

            Assert.That(bottle.TopLayer.Value.Color, Is.EqualTo(DC(0f, 0f, 1f)).Within(0.001f));
            Assert.That(bottle.Layers[0].Color, Is.EqualTo(DC(1f, 0f, 0f)).Within(0.001f));
        }

        [Test]
        public void Clear_RemovesAllLayers()
        {
            var bottle = CreateSut();
            bottle.AddLayer(Layer(1f, 0f, 0f, 1f, 0.25f));
            bottle.AddLayer(Layer(0f, 0f, 1f, 1f, 0.35f));

            bottle.Clear();

            Assert.That(bottle.IsEmpty, Is.True);
            Assert.That(bottle.TotalFill, Is.EqualTo(0f));
            Assert.That(bottle.Layers.Count, Is.EqualTo(0));
        }

        [Test]
        public void IsComplete_WhenAllLayersSameColor_ReturnsTrue()
        {
            var bottle = CreateSut(4);
            var validator = new PuzzleGame.Domain.Services.BottleValidationService(0.05f);
            Assert.That(validator.IsComplete(bottle), Is.False); // empty is not complete

            bottle.AddLayer(Layer(1f, 0f, 0f, 1f, 0.33f));
            bottle.AddLayer(Layer(1f, 0f, 0f, 1f, 0.33f));
            bottle.AddLayer(Layer(1f, 0f, 0f, 1f, 0.34f));

            Assert.That(bottle.IsFull, Is.True);
            Assert.That(validator.IsComplete(bottle), Is.True);
        }

        [Test]
        public void IsComplete_WhenMixedColors_ReturnsFalse()
        {
            var bottle = CreateSut(4);
            var validator = new PuzzleGame.Domain.Services.BottleValidationService(0.05f);
            bottle.AddLayer(Layer(1f, 0f, 0f, 1f, 0.25f));
            bottle.AddLayer(Layer(0f, 0f, 1f, 1f, 0.25f));

            Assert.That(validator.IsComplete(bottle), Is.False);
        }

        [Test]
        public void GetHashCode_SameDomainColors_ProduceSameHashes()
        {
            var bottle1 = CreateSut();
            var a = DC(0.5f, 0.2f, 0.8f);
            bottle1.AddLayer(Layer(0.5f, 0.2f, 0.8f, 1f, 0.25f));

            var bottle2 = CreateSut();
            bottle2.AddLayer(new LiquidLayer(a, 0.25f));

            Assert.That(bottle1.Layers[0].GetHashCode(), Is.EqualTo(bottle2.Layers[0].GetHashCode()));
        }

        [Test]
        public void IsFull_WhenAllSlotsUsed_ReturnsTrue()
        {
            var bottle = CreateSut(4);
            bottle.AddLayer(Layer(1f, 0f, 0f, 1f, 0.25f));
            bottle.AddLayer(Layer(0f, 0f, 1f, 1f, 0.25f));
            bottle.AddLayer(Layer(0f, 1f, 0f, 1f, 0.25f));
            bottle.AddLayer(Layer(1f, 1f, 0f, 1f, 0.25f));

            Assert.That(bottle.IsFull, Is.True);
        }
    }
}
