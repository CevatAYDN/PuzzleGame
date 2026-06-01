using NUnit.Framework;
using BottleShaders.Domain.Models;
using UnityEngine;

namespace BottleShaders.Domain.Tests.Models
{
    public class LiquidLayerTests
    {
        [Test]
        public void Constructor_WithUnityColor_SetsProperties()
        {
            var unityColor = new Color(0.5f, 0.3f, 0.8f, 1f);
            var layer = new LiquidLayer(unityColor, 0.25f);

            Assert.That(layer.Color.ToUnityColor(), Is.EqualTo(unityColor));
            Assert.That(layer.Amount, Is.EqualTo(0.25f));
        }

        [Test]
        public void Constructor_WithDomainColor_SetsProperties()
        {
            var domainColor = new DomainColor(0.5f, 0.3f, 0.8f, 1f);
            var layer = new LiquidLayer(domainColor, 0.25f);

            Assert.That(layer.Color, Is.EqualTo(domainColor));
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
        public void WithColor_UnityColor_ReturnsNewLayerWithDifferentColor()
        {
            var original = new LiquidLayer(Color.red, 0.25f);
            var modified = original.WithColor(Color.blue);

            Assert.That(original.Color.ToUnityColor(), Is.EqualTo(Color.red));
            Assert.That(modified.Color.ToUnityColor(), Is.EqualTo(Color.blue));
            Assert.That(modified.Amount, Is.EqualTo(original.Amount));
        }

        [Test]
        public void WithColor_DomainColor_ReturnsNewLayerWithDifferentColor()
        {
            var original = new LiquidLayer(Color.red, 0.25f);
            var modified = original.WithColor(new DomainColor(0, 0, 1, 1));

            Assert.That(original.Color.ToUnityColor(), Is.EqualTo(Color.red));
            Assert.That(modified.Color.ToUnityColor().r, Is.EqualTo(0).Within(0.001f));
            Assert.That(modified.Amount, Is.EqualTo(original.Amount));
        }

        [Test]
        public void WithAmount_ReturnsNewLayerWithDifferentAmount()
        {
            var original = new LiquidLayer(Color.red, 0.25f);
            var modified = original.WithAmount(0.75f);

            Assert.That(original.Amount, Is.EqualTo(0.25f));
            Assert.That(modified.Amount, Is.EqualTo(0.75f));
        }

        [Test]
        public void WithAmount_NegativeAmount_ClampsToZero()
        {
            var original = new LiquidLayer(Color.red, 0.25f);
            var modified = original.WithAmount(-1f);
            Assert.That(modified.Amount, Is.EqualTo(0f));
        }
    }
}