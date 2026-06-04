using NUnit.Framework;
using PuzzleGame.Domain.Models;

namespace PuzzleGame.Domain.Tests.Models
{
    public class MoldStateTests
    {
        private MoldState CreateSut(int maxLayers = 4)
        {
            return new MoldState(maxLayers);
        }

        private OreLayer Layer(float r, float g, float b, float a = 1f, float amount = 0.25f)
        {
            return new OreLayer(new DomainColor(r, g, b, a), amount);
        }

        private DomainColor DC(float r, float g, float b, float a = 1f) => new DomainColor(r, g, b, a);

        [Test]
        public void Constructor_SetsMaxLayers()
        {
            var Mold = CreateSut(5);
            Assert.That(Mold.MaxLayers, Is.EqualTo(5));
        }

        [Test]
        public void NewMold_IsEmpty()
        {
            var Mold = CreateSut();
            Assert.That(Mold.IsEmpty, Is.True);
        }

        [Test]
        public void NewMold_IsNotFull()
        {
            var Mold = CreateSut();
            Assert.That(Mold.IsFull, Is.False);
        }

        [Test]
        public void NewMold_TotalFillIsZero()
        {
            var Mold = CreateSut();
            Assert.That(Mold.TotalFill, Is.EqualTo(0f));
        }

        [Test]
        public void NewMold_TopLayerIsNull()
        {
            var Mold = CreateSut();
            Assert.That(Mold.TopLayer, Is.Null);
        }

        [Test]
        public void NewMold_LayersIsEmpty()
        {
            var Mold = CreateSut();
            Assert.That(Mold.Layers, Is.Empty);
        }

        [Test]
        public void AddLayer_AddsLayerToMold()
        {
            var Mold = CreateSut();
            var layer = Layer(1f, 0f, 0f); // Red

            Mold.AddLayer(layer);

            Assert.That(Mold.Layers.Count, Is.EqualTo(1));
            var topLayer = Mold.Layers[Mold.Layers.Count - 1];
            Assert.That(topLayer.Color, Is.EqualTo(DC(1f, 0f, 0f)).Within(0.001f));
        }

        [Test]
        public void AddLayer_MultipleLayers_AreOrderedCorrectly()
        {
            var Mold = CreateSut();
            var red = Layer(1f, 0f, 0f);
            var blue = Layer(0f, 0f, 1f, 1f, 0.30f);
            var green = Layer(0f, 1f, 0f, 1f, 0.20f);

            Mold.AddLayer(red);
            Mold.AddLayer(blue);
            Mold.AddLayer(green);

            Assert.That(Mold.Layers.Count, Is.EqualTo(3));
            Assert.That(Mold.Layers[0].Color, Is.EqualTo(DC(1f, 0f, 0f)).Within(0.001f));
            Assert.That(Mold.Layers[1].Color, Is.EqualTo(DC(0f, 0f, 1f)).Within(0.001f));
            Assert.That(Mold.Layers[2].Color, Is.EqualTo(DC(0f, 1f, 0f)).Within(0.001f));
        }

        [Test]
        public void AddLayer_WhenFull_ThrowsException()
        {
            var Mold = CreateSut(2);
            Mold.AddLayer(Layer(1f, 0f, 0f));
            Mold.AddLayer(Layer(0f, 0f, 1f));

            Assert.Throws<System.InvalidOperationException>(() => Mold.AddLayer(Layer(0f, 1f, 0f)));
        }

        [Test]
        public void AddLayer_UpdatesTotalFill()
        {
            var Mold = CreateSut();
            Mold.AddLayer(Layer(1f, 0f, 0f, 1f, 0.25f));
            Mold.AddLayer(Layer(0f, 0f, 1f, 1f, 0.35f));
            Assert.That(Mold.TotalFill, Is.EqualTo(0.60f).Within(0.001f));
        }

        [Test]
        public void PopLayer_RemovesAndReturnsTopLayer()
        {
            var Mold = CreateSut();
            Mold.AddLayer(Layer(1f, 0f, 0f, 1f, 0.25f));
            Mold.AddLayer(Layer(1f, 0f, 0f, 1f, 0.25f));
            Mold.AddLayer(Layer(0f, 0f, 1f, 1f, 0.25f));
            Mold.AddLayer(Layer(0f, 0f, 1f, 1f, 0.25f));

            Assert.That(Mold.Layers.Count, Is.EqualTo(4));

            var popped = Mold.PopTopLayer();
            Assert.That(popped, Is.Not.Null);
            Assert.That(Mold.Layers.Count, Is.EqualTo(3));
        }

        [Test]
        public void PopLayer_WhenEmpty_ThrowsException()
        {
            var Mold = CreateSut();
            Assert.Throws<System.InvalidOperationException>(() => Mold.PopTopLayer());
        }

        [Test]
        public void TopLayer_AfterAddLayer_ReturnsLastAdded()
        {
            var Mold = CreateSut();
            var red = Layer(1f, 0f, 0f);
            var blue = Layer(0f, 0f, 1f);

            Mold.AddLayer(red);
            Mold.AddLayer(blue);

            Assert.That(Mold.TopLayer.Value.Color, Is.EqualTo(DC(0f, 0f, 1f)).Within(0.001f));
            Assert.That(Mold.Layers[0].Color, Is.EqualTo(DC(1f, 0f, 0f)).Within(0.001f));
        }

        [Test]
        public void Clear_RemovesAllLayers()
        {
            var Mold = CreateSut();
            Mold.AddLayer(Layer(1f, 0f, 0f, 1f, 0.25f));
            Mold.AddLayer(Layer(0f, 0f, 1f, 1f, 0.35f));

            Mold.Clear();

            Assert.That(Mold.IsEmpty, Is.True);
            Assert.That(Mold.TotalFill, Is.EqualTo(0f));
            Assert.That(Mold.Layers.Count, Is.EqualTo(0));
        }

        [Test]
        public void IsComplete_WhenAllLayersSameColor_ReturnsTrue()
        {
            var Mold = CreateSut(3);
            var validator = new PuzzleGame.Domain.Services.MoldValidationService(0.05f);
            Assert.That(validator.IsComplete(Mold), Is.True); // empty is complete

            Mold.AddLayer(Layer(1f, 0f, 0f, 1f, 0.33f));
            Mold.AddLayer(Layer(1f, 0f, 0f, 1f, 0.33f));
            Mold.AddLayer(Layer(1f, 0f, 0f, 1f, 0.34f));

            Assert.That(Mold.IsFull, Is.True);
            Assert.That(validator.IsComplete(Mold), Is.True);
        }

        [Test]
        public void IsComplete_WhenMixedColors_ReturnsFalse()
        {
            var Mold = CreateSut(4);
            var validator = new PuzzleGame.Domain.Services.MoldValidationService(0.05f);
            Mold.AddLayer(Layer(1f, 0f, 0f, 1f, 0.25f));
            Mold.AddLayer(Layer(0f, 0f, 1f, 1f, 0.25f));

            Assert.That(validator.IsComplete(Mold), Is.False);
        }

        [Test]
        public void GetHashCode_SameDomainColors_ProduceSameHashes()
        {
            var Mold1 = CreateSut();
            var a = DC(0.5f, 0.2f, 0.8f);
            Mold1.AddLayer(Layer(0.5f, 0.2f, 0.8f, 1f, 0.25f));

            var Mold2 = CreateSut();
            Mold2.AddLayer(new OreLayer(a, 0.25f));

            Assert.That(Mold1.Layers[0].GetHashCode(), Is.EqualTo(Mold2.Layers[0].GetHashCode()));
        }

        [Test]
        public void IsFull_WhenAllSlotsUsed_ReturnsTrue()
        {
            var Mold = CreateSut(4);
            Mold.AddLayer(Layer(1f, 0f, 0f, 1f, 0.25f));
            Mold.AddLayer(Layer(0f, 0f, 1f, 1f, 0.25f));
            Mold.AddLayer(Layer(0f, 1f, 0f, 1f, 0.25f));
            Mold.AddLayer(Layer(1f, 1f, 0f, 1f, 0.25f));

            Assert.That(Mold.IsFull, Is.True);
        }
    }
}
