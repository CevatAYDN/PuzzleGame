using NUnit.Framework;
using PuzzleGame.Domain.Models;
namespace PuzzleGame.Domain.Tests.Models
{
    public class OreLayerTests
    {
        [Test]
        public void Constructor_WithDomainColor_SetsProperties()
        {
            var domainColor = new DomainColor(0.5f, 0.3f, 0.8f, 1f);
            var layer = new OreLayer(domainColor, 0.25f);

            Assert.That(layer.Color, Is.EqualTo(domainColor));
            Assert.That(layer.Amount, Is.EqualTo(0.25f));
        }

        [Test]
        public void Constructor_WithUnityColorViaAdapter_SetsProperties()
        {
            var domainColor = new DomainColor(0.5f, 0.3f, 0.8f, 1f);
            var layer = new OreLayer(domainColor, 0.25f);

            Assert.That(layer.Color, Is.EqualTo(domainColor));
            Assert.That(layer.Amount, Is.EqualTo(0.25f));
        }

        [Test]
        public void Constructor_WithNegativeAmount_ClampsToZero()
        {
            var layer = new OreLayer(new DomainColor(1f, 0f, 0f), -0.5f);
            Assert.That(layer.Amount, Is.EqualTo(0f));
        }

        [Test]
        public void Constructor_WithZeroAmount_AmountIsZero()
        {
            var layer = new OreLayer(new DomainColor(0f, 0f, 1f), 0f);
            Assert.That(layer.Amount, Is.EqualTo(0f));
        }

        [Test]
        public void IsEmpty_WithTransparentColor_ReturnsTrue()
        {
            var layer = new OreLayer(new DomainColor(0, 0, 0, 0), 0.25f);
            Assert.That(layer.IsEmpty, Is.True);
        }

        [Test]
        public void IsEmpty_WithZeroAmount_ReturnsTrue()
        {
            var layer = new OreLayer(new DomainColor(1f, 0f, 0f), 0f);
            Assert.That(layer.IsEmpty, Is.True);
        }

        [Test]
        public void IsEmpty_WithVisibleColorAndPositiveAmount_ReturnsFalse()
        {
            var layer = new OreLayer(new DomainColor(1, 0, 0, 1), 0.25f);
            Assert.That(layer.IsEmpty, Is.False);
        }

        [Test]
        public void WithColor_ReturnsNewLayerWithDifferentColor()
        {
            var original = new OreLayer(new DomainColor(1, 0, 0, 1), 0.25f);
            var modified = original.WithColor(new DomainColor(0, 0, 1, 1));

            Assert.That(original.Color, Is.EqualTo(new DomainColor(1, 0, 0, 1)));
            Assert.That(modified.Color, Is.EqualTo(new DomainColor(0, 0, 1, 1)));
            Assert.That(modified.Amount, Is.EqualTo(original.Amount));
        }

        [Test]
        public void WithAmount_ReturnsNewLayerWithDifferentAmount()
        {
            var original = new OreLayer(new DomainColor(1, 0, 0, 1), 0.25f);
            var modified = original.WithAmount(0.75f);

            Assert.That(original.Amount, Is.EqualTo(0.25f));
            Assert.That(modified.Amount, Is.EqualTo(0.75f));
        }

        [Test]
        public void WithAmount_NegativeAmount_ClampsToZero()
        {
            var original = new OreLayer(new DomainColor(1, 0, 0, 1), 0.25f);
            var modified = original.WithAmount(-1f);
            Assert.That(modified.Amount, Is.EqualTo(0f));
        }
    }
}
