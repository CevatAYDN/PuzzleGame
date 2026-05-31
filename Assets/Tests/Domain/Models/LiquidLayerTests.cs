using NUnit.Framework;
using BottleShaders.Domain.Models;
using UnityEngine;

namespace BottleShaders.Domain.Tests.Models
{
    public class LiquidLayerTests
    {
        [Test]
        public void Constructor_WithValidColorAndAmount_SetsProperties()
        {
            var color = new Color(0.5f, 0.3f, 0.8f, 1f);
            var layer = new LiquidLayer(color, 0.25f);

            Assert.That(layer.Color, Is.EqualTo(color));
            Assert.That(layer.Amount, Is.EqualTo(0.25f));
        }

        [Test]
        public void Constructor_WithNegativeAmount_ClampsToZero()
        {
            var layer = new LiquidLayer(Color.red, -0.5f);

            Assert.That(layer.Amount, Is.EqualTo(0f));
        }

        [Test]
        public void Constructor_WithZeroAmount_AmountIsZero()
        {
            var layer = new LiquidLayer(Color.blue, 0f);

            Assert.That(layer.Amount, Is.EqualTo(0f));
        }

        [Test]
        public void IsEmpty_WithTransparentColor_ReturnsTrue()
        {
            var layer = new LiquidLayer(new Color(0, 0, 0, 0), 0.25f);

            Assert.That(layer.IsEmpty, Is.True);
        }

        [Test]
        public void IsEmpty_WithZeroAmount_ReturnsTrue()
        {
            var layer = new LiquidLayer(Color.red, 0f);

            Assert.That(layer.IsEmpty, Is.True);
        }

        [Test]
        public void IsEmpty_WithVisibleColorAndPositiveAmount_ReturnsFalse()
        {
            var layer = new LiquidLayer(new Color(1, 0, 0, 1), 0.25f);

            Assert.That(layer.IsEmpty, Is.False);
        }

        [Test]
        public void WithColor_ReturnsNewLayerWithDifferentColor()
        {
            var original = new LiquidLayer(Color.red, 0.25f);
            var modified = original.WithColor(Color.blue);

            Assert.That(original.Color, Is.EqualTo(Color.red), "Original should be unchanged");
            Assert.That(modified.Color, Is.EqualTo(Color.blue));
            Assert.That(modified.Amount, Is.EqualTo(original.Amount));
        }

        [Test]
        public void WithAmount_ReturnsNewLayerWithDifferentAmount()
        {
            var original = new LiquidLayer(Color.red, 0.25f);
            var modified = original.WithAmount(0.75f);

            Assert.That(original.Amount, Is.EqualTo(0.25f), "Original should be unchanged");
            Assert.That(modified.Amount, Is.EqualTo(0.75f));
            Assert.That(modified.Color, Is.EqualTo(original.Color));
        }

        [Test]
        public void WithAmount_NegativeAmount_ClampsToZero()
        {
            var original = new LiquidLayer(Color.red, 0.25f);
            var modified = original.WithAmount(-1f);

            Assert.That(modified.Amount, Is.EqualTo(0f));
        }

        [Test]
        public void IsEmpty_AlphaJustAboveThreshold_ReturnsFalse()
        {
            var layer = new LiquidLayer(new Color(1, 0, 0, 0.02f), 0.25f);

            Assert.That(layer.IsEmpty, Is.False);
        }

        [Test]
        public void IsEmpty_AmountJustAboveThreshold_ReturnsFalse()
        {
            var layer = new LiquidLayer(Color.red, 0.002f);

            Assert.That(layer.IsEmpty, Is.False);
        }
    }
}