using NUnit.Framework;
using BottleShaders.Domain.Models;
using BottleShaders.Domain.Services;
using UnityEngine;

namespace BottleShaders.Domain.Tests.Services
{
    public class BottleValidationServiceTests
    {
        private BottleValidationService _validator;
        private BottleState _source;
        private BottleState _target;

        [SetUp]
        public void Setup()
        {
            _validator = new BottleValidationService(0.05f);
            _source = new BottleState(4);
            _target = new BottleState(4);
        }

        // ── UnityColorsMatch ────────────────────────────────────────────────

        [Test]
        public void UnityColorsMatch_IdenticalColors_ReturnsTrue()
        {
            bool match = _validator.UnityColorsMatch(Color.red, Color.red);
            Assert.That(match, Is.True);
        }

        [Test]
        public void UnityColorsMatch_SimilarColorsWithinTolerance_ReturnsTrue()
        {
            var a = new Color(0.50f, 0.20f, 0.80f);
            var b = new Color(0.52f, 0.22f, 0.78f);
            bool match = _validator.UnityColorsMatch(a, b);
            Assert.That(match, Is.True);
        }

        [Test]
        public void UnityColorsMatch_DifferentColors_ReturnsFalse()
        {
            bool match = _validator.UnityColorsMatch(Color.red, Color.blue);
            Assert.That(match, Is.False);
        }

        [Test]
        public void UnityColorsMatch_BarelyOutsideTolerance_ReturnsFalse()
        {
            var a = new Color(0.50f, 0.20f, 0.80f);
            var b = new Color(0.56f, 0.26f, 0.86f);
            bool match = _validator.UnityColorsMatch(a, b);
            Assert.That(match, Is.False);
        }

        [Test]
        public void UnityColorsMatch_ExactToleranceBoundary_ReturnsFalse()
        {
            var a = new Color(0.50f, 0.20f, 0.80f);
            var b = new Color(0.55f, 0.25f, 0.85f);
            bool match = _validator.UnityColorsMatch(a, b);
            Assert.That(match, Is.False);
        }

        [Test]
        public void Constructor_WithCustomTolerance_UsesIt()
        {
            var strictValidator = new BottleValidationService(0.01f);
            var a = new Color(0.50f, 0.20f, 0.80f);
            var b = new Color(0.54f, 0.24f, 0.84f);

            bool strictMatch = strictValidator.UnityColorsMatch(a, b);
            bool defaultMatch = _validator.UnityColorsMatch(a, b);

            Assert.That(strictMatch, Is.False, "Strict tolerance should reject");
            Assert.That(defaultMatch, Is.True, "Default tolerance should accept");
        }

        // ── CanPour ─────────────────────────────────────────────────────────

        [Test]
        public void CanPour_FromEmptySource_ReturnsFalse()
        {
            bool canPour = _validator.CanPour(_source, _target);
            Assert.That(canPour, Is.False);
        }

        [Test]
        public void CanPour_IntoFullTarget_ReturnsFalse()
        {
            _source.AddLayer(new LiquidLayer(Color.red, 0.25f));
            FillBottle(_target, 4);
            bool canPour = _validator.CanPour(_source, _target);
            Assert.That(canPour, Is.False);
        }

        [Test]
        public void CanPour_SameBottle_ReturnsFalse()
        {
            _source.AddLayer(new LiquidLayer(Color.red, 0.25f));
            bool canPour = _validator.CanPour(_source, _source);
            Assert.That(canPour, Is.False);
        }

        [Test]
        public void CanPour_IntoEmptyTarget_ReturnsTrue()
        {
            _source.AddLayer(new LiquidLayer(Color.red, 0.25f));
            bool canPour = _validator.CanPour(_source, _target);
            Assert.That(canPour, Is.True);
        }

        [Test]
        public void CanPour_MatchingColors_ReturnsTrue()
        {
            _source.AddLayer(new LiquidLayer(Color.red, 0.25f));
            _target.AddLayer(new LiquidLayer(Color.red, 0.25f));
            bool canPour = _validator.CanPour(_source, _target);
            Assert.That(canPour, Is.True);
        }

        [Test]
        public void CanPour_DifferentColors_ReturnsFalse()
        {
            _source.AddLayer(new LiquidLayer(Color.red, 0.25f));
            _target.AddLayer(new LiquidLayer(Color.blue, 0.25f));
            bool canPour = _validator.CanPour(_source, _target);
            Assert.That(canPour, Is.False);
        }

        [Test]
        public void CanPour_SourceIsNull_ReturnsFalse()
        {
            bool canPour = _validator.CanPour(null, _target);
            Assert.That(canPour, Is.False);
        }

        [Test]
        public void CanPour_TargetIsNull_ReturnsFalse()
        {
            _source.AddLayer(new LiquidLayer(Color.red, 0.25f));
            bool canPour = _validator.CanPour(_source, null);
            Assert.That(canPour, Is.False);
        }

        [Test]
        public void CanPour_BothNull_ReturnsFalse()
        {
            bool canPour = _validator.CanPour(null, null);
            Assert.That(canPour, Is.False);
        }

        [Test]
        public void CanPour_MatchingColorsWithinTolerance_ReturnsTrue()
        {
            var sourceColor = new Color(0.50f, 0.20f, 0.80f);
            var targetColor = new Color(0.52f, 0.22f, 0.78f);
            _source.AddLayer(new LiquidLayer(sourceColor, 0.25f));
            _target.AddLayer(new LiquidLayer(targetColor, 0.25f));
            bool canPour = _validator.CanPour(_source, _target);
            Assert.That(canPour, Is.True);
        }

        // ── IsComplete ──────────────────────────────────────────────────────

        [Test]
        public void IsComplete_EmptyBottle_ReturnsTrue()
        {
            bool complete = _validator.IsComplete(_source);
            Assert.That(complete, Is.True);
        }

        [Test]
        public void IsComplete_FullSingleColor_ReturnsTrue()
        {
            FillBottle(_source, 4, Color.red);
            bool complete = _validator.IsComplete(_source);
            Assert.That(complete, Is.True);
        }

        [Test]
        public void IsComplete_FullSingleColorWithTolerance_ReturnsTrue()
        {
            var mainColor = new Color(0.50f, 0.20f, 0.80f);
            var closeColor = new Color(0.52f, 0.22f, 0.78f);
            _source.AddLayer(new LiquidLayer(mainColor, 0.25f));
            _source.AddLayer(new LiquidLayer(closeColor, 0.25f));
            _source.AddLayer(new LiquidLayer(mainColor, 0.25f));
            _source.AddLayer(new LiquidLayer(closeColor, 0.25f));
            bool complete = _validator.IsComplete(_source);
            Assert.That(complete, Is.True);
        }

        [Test]
        public void IsComplete_NotFull_ReturnsFalse()
        {
            _source.AddLayer(new LiquidLayer(Color.red, 0.25f));
            bool complete = _validator.IsComplete(_source);
            Assert.That(complete, Is.False);
        }

        [Test]
        public void IsComplete_MixedColors_ReturnsFalse()
        {
            _source.AddLayer(new LiquidLayer(Color.red, 0.33f));
            _source.AddLayer(new LiquidLayer(Color.blue, 0.33f));
            _source.AddLayer(new LiquidLayer(Color.green, 0.34f));
            bool complete = _validator.IsComplete(_source);
            Assert.That(complete, Is.False);
        }

        [Test]
        public void IsComplete_FullButTwoColors_ReturnsFalse()
        {
            FillBottle(_source, 2, Color.red);
            FillBottle(_source, 2, Color.blue);
            bool complete = _validator.IsComplete(_source);
            Assert.That(complete, Is.False);
        }

        [Test]
        public void IsComplete_Null_ReturnsFalse()
        {
            bool complete = _validator.IsComplete(null);
            Assert.That(complete, Is.False);
        }

        // ── Helpers ─────────────────────────────────────────────────────────

        private void FillBottle(BottleState bottle, int count, Color? color = null)
        {
            Color c = color ?? Color.red;
            for (int i = 0; i < count; i++)
                bottle.AddLayer(new LiquidLayer(c, 0.25f));
        }
    }
}