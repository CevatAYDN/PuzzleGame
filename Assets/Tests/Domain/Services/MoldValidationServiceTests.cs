using NUnit.Framework;
using PuzzleGame.Domain.Models;
using PuzzleGame.Domain.Services;

namespace PuzzleGame.Domain.Tests.Services
{
    public class MoldValidationServiceTests
    {
        private MoldValidationService _validator;
        private MoldState _source;
        private MoldState _target;

        [SetUp]
        public void Setup()
        {
            _validator = new MoldValidationService(0.05f);
            _source = new MoldState(4);
            _target = new MoldState(4);
        }

        private DomainColor DC(float r, float g, float b, float a = 1f) => new DomainColor(r, g, b, a);
        private OreLayer Layer(DomainColor c, float a = 0.25f) => new OreLayer(c, a);

        private void FillMold(MoldState Mold, int count, DomainColor? color = null)
        {
            DomainColor c = color ?? DC(1f, 0f, 0f);
            for (int i = 0; i < count; i++)
                Mold.AddLayer(Layer(c, 0.25f));
        }

        private DomainColor Red() => OreColor.Red.ToDefaultDomainColor();
        private DomainColor Blue() => OreColor.Blue.ToDefaultDomainColor();
        private DomainColor Green() => OreColor.Green.ToDefaultDomainColor();

        // ── ColorsMatch ─────────────────────────────────────────────────────

        [Test]
        public void ColorsMatch_IdenticalColors_ReturnsTrue()
        {
            bool match = _validator.ColorsMatch(DC(1,0,0), DC(1,0,0));
            Assert.That(match, Is.True);
        }

        [Test]
        public void ColorsMatch_SimilarWithinTolerance_ReturnsTrue()
        {
            var a = DC(0.50f, 0.20f, 0.80f);
            var b = DC(0.52f, 0.22f, 0.78f);
            Assert.That(_validator.ColorsMatch(a, b), Is.True);
        }

        [Test]
        public void ColorsMatch_Different_ReturnsFalse()
        {
            Assert.That(_validator.ColorsMatch(DC(1,0,0), DC(0,0,1)), Is.False);
        }

        [Test]
        public void ColorsMatch_DifferentAlpha_ReturnsFalse()
        {
            var a = DC(1, 0, 0, 0.8f);
            var b = DC(1, 0, 0, 0.5f);
            Assert.That(_validator.ColorsMatch(a, b), Is.False);
        }

        [Test]
        public void ColorsMatch_BarelyOutsideTolerance_ReturnsFalse()
        {
            var a = DC(0.50f, 0.20f, 0.80f);
            var b = DC(0.56f, 0.26f, 0.86f);
            Assert.That(_validator.ColorsMatch(a, b), Is.False);
        }

        [Test]
        public void Constructor_WithCustomTolerance_UsesIt()
        {
            var strictValidator = new MoldValidationService(0.01f);
            var a = DC(0.50f, 0.20f, 0.80f);
            var b = DC(0.54f, 0.24f, 0.84f);

            Assert.That(strictValidator.ColorsMatch(a, b), Is.False, "Strict tolerance should reject");
            Assert.That(_validator.ColorsMatch(a, b), Is.True, "Default tolerance should accept");
        }

        // ── CanCast ─────────────────────────────────────────────────────────

        [Test]
        public void CanCast_FromEmptySource_ReturnsFalse()
        {
            Assert.That(_validator.CanCast(_source, _target), Is.False);
        }

        [Test]
        public void CanCast_IntoFullTarget_ReturnsFalse()
        {
            _source.AddLayer(Layer(Red()));
            FillMold(_target, 4);
            Assert.That(_validator.CanCast(_source, _target), Is.False);
        }

        [Test]
        public void CanCast_SameMold_ReturnsFalse()
        {
            _source.AddLayer(Layer(Red()));
            Assert.That(_validator.CanCast(_source, _source), Is.False);
        }

        [Test]
        public void CanCast_IntoEmptyTarget_ReturnsTrue()
        {
            _source.AddLayer(Layer(Red()));
            Assert.That(_validator.CanCast(_source, _target), Is.True);
        }

        [Test]
        public void CanCast_FromEmptyTopLayerIntoEmptyTarget_ReturnsFalse()
        {
            var emptyLayer = new OreLayer(DC(0, 0, 0, 0), 0.25f, OreColor.None);
            _source.AddLayer(emptyLayer);
            Assert.That(_validator.CanCast(_source, _target), Is.False);
        }

        [Test]
        public void CanCast_MatchingColors_ReturnsTrue()
        {
            _source.AddLayer(Layer(Red()));
            _target.AddLayer(Layer(Red()));
            Assert.That(_validator.CanCast(_source, _target), Is.True);
        }

        [Test]
        public void CanCast_DifferentColors_ReturnsFalse()
        {
            _source.AddLayer(Layer(Red()));
            _target.AddLayer(Layer(Blue()));
            Assert.That(_validator.CanCast(_source, _target), Is.False);
        }

        [Test]
        public void CanCast_SourceIsNull_ThrowsArgumentNullException()
        {
            Assert.Throws<System.ArgumentNullException>(() => _validator.CanCast(null, _target));
        }

        [Test]
        public void CanCast_TargetIsNull_ThrowsArgumentNullException()
        {
            _source.AddLayer(Layer(Red()));
            Assert.Throws<System.ArgumentNullException>(() => _validator.CanCast(_source, null));
        }

        [Test]
        public void CanCast_BothNull_ThrowsArgumentNullException()
        {
            Assert.Throws<System.ArgumentNullException>(() => _validator.CanCast(null, null));
        }

        [Test]
        public void CanCast_MatchingWithinTolerance_ReturnsTrue()
        {
            _source.AddLayer(Layer(DC(0.50f, 0.20f, 0.80f)));
            _target.AddLayer(Layer(DC(0.52f, 0.22f, 0.78f)));
            Assert.That(_validator.CanCast(_source, _target), Is.True);
        }

        // ── IsComplete ──────────────────────────────────────────────────────

        [Test]
        public void IsComplete_EmptyMold_ReturnsFalse()
        {
            // Fix #2: An empty mold is NOT complete — puzzle only ends when every
            // ore is sorted into a uniformly full mold. Returning true here would
            // silently pass win-checks on half-finished levels.
            Assert.That(_validator.IsComplete(_source), Is.False);
        }

        [Test]
        public void IsComplete_FullSingleColor_ReturnsTrue()
        {
            FillMold(_source, 4, Red());
            Assert.That(_validator.IsComplete(_source), Is.True);
        }

        [Test]
        public void IsComplete_FullSingleColorWithTolerance_ReturnsTrue()
        {
            _source.AddLayer(Layer(DC(0.50f, 0.20f, 0.80f)));
            _source.AddLayer(Layer(DC(0.52f, 0.22f, 0.78f)));
            _source.AddLayer(Layer(DC(0.50f, 0.20f, 0.80f)));
            _source.AddLayer(Layer(DC(0.52f, 0.22f, 0.78f)));
            Assert.That(_validator.IsComplete(_source), Is.True);
        }

        [Test]
        public void IsComplete_NotFull_ReturnsFalse()
        {
            _source.AddLayer(Layer(Red()));
            Assert.That(_validator.IsComplete(_source), Is.False);
        }

        [Test]
        public void IsComplete_MixedColors_ReturnsFalse()
        {
            _source.AddLayer(Layer(Red()));
            _source.AddLayer(Layer(Blue()));
            _source.AddLayer(Layer(Green()));
            Assert.That(_validator.IsComplete(_source), Is.False);
        }

        [Test]
        public void IsComplete_FullButTwoColors_ReturnsFalse()
        {
            FillMold(_source, 2, Red());
            FillMold(_source, 2, Blue());
            Assert.That(_validator.IsComplete(_source), Is.False);
        }

        [Test]
        public void IsComplete_Null_ThrowsArgumentNullException()
        {
            Assert.Throws<System.ArgumentNullException>(() => _validator.IsComplete(null));
        }

        // ── CanMultiCast ────────────────────────────────────────────────────

        [Test]
        public void CanMultiCast_NullSources_ThrowsArgumentNullException()
        {
            Assert.Throws<System.ArgumentNullException>(() => _validator.CanMultiCast(null, _target));
        }

        [Test]
        public void CanMultiCast_EmptySourcesArray_ThrowsArgumentNullException()
        {
            Assert.Throws<System.ArgumentNullException>(() => _validator.CanMultiCast(new MoldState[0], _target));
        }

        [Test]
        public void CanMultiCast_NullTarget_ThrowsArgumentNullException()
        {
            var sources = new[] { _source };
            Assert.Throws<System.ArgumentNullException>(() => _validator.CanMultiCast(sources, null));
        }

        [Test]
        public void CanMultiCast_EmptyTarget_ReturnsFalse()
        {
            _source.AddLayer(Layer(Red()));
            Assert.That(_validator.CanMultiCast(new[] { _source }, _target), Is.False);
        }

        [Test]
        public void CanMultiCast_FullTarget_ReturnsFalse()
        {
            _source.AddLayer(Layer(Red()));
            FillMold(_target, 4, Blue());
            Assert.That(_validator.CanMultiCast(new[] { _source }, _target), Is.False);
        }

        [Test]
        public void CanMultiCast_InsufficientSpace_ReturnsFalse()
        {
            // Target has 3 layers (1 free slot), but 2 sources → needs 2 slots.
            var src1 = new MoldState(4);
            var src2 = new MoldState(4);
            src1.AddLayer(Layer(Red()));
            src2.AddLayer(Layer(Red()));
            FillMold(_target, 3, Blue());

            Assert.That(_validator.CanMultiCast(new[] { src1, src2 }, _target), Is.False);
        }

        [Test]
        public void CanMultiCast_NullSourceInArray_ReturnsFalse()
        {
            var src1 = new MoldState(4);
            src1.AddLayer(Layer(Red()));
            _target.AddLayer(Layer(Blue()));

            Assert.That(_validator.CanMultiCast(new MoldState[] { src1, null }, _target), Is.False);
        }

        [Test]
        public void CanMultiCast_EmptySource_ReturnsFalse()
        {
            _target.AddLayer(Layer(Blue()));
            Assert.That(_validator.CanMultiCast(new[] { _source }, _target), Is.False);
        }

        [Test]
        public void CanMultiCast_SourceEqualsTarget_ReturnsFalse()
        {
            _source.AddLayer(Layer(Red()));
            _target.AddLayer(Layer(Blue()));
            Assert.That(_validator.CanMultiCast(new[] { _source, _source }, _target), Is.False);
        }

        [Test]
        public void CanMultiCast_SameColorSources_ReturnsTrue()
        {
            var src1 = new MoldState(4);
            var src2 = new MoldState(4);
            src1.AddLayer(Layer(Red()));
            src2.AddLayer(Layer(Red()));
            _target.AddLayer(Layer(Blue()));
            _target.AddLayer(Layer(Blue())); // 2 layers → 2 free slots

            Assert.That(_validator.CanMultiCast(new[] { src1, src2 }, _target), Is.True);
        }

        [Test]
        public void CanMultiCast_DifferentColors_ReturnsFalse()
        {
            var src1 = new MoldState(4);
            var src2 = new MoldState(4);
            src1.AddLayer(Layer(Red()));
            src2.AddLayer(Layer(Blue()));
            _target.AddLayer(Layer(Blue()));
            _target.AddLayer(Layer(Blue()));

            Assert.That(_validator.CanMultiCast(new[] { src1, src2 }, _target), Is.False);
        }

        [Test]
        public void CanMultiCast_SingleSource_ReturnsTrue()
        {
            _source.AddLayer(Layer(Red()));
            _target.AddLayer(Layer(Blue()));
            _target.AddLayer(Layer(Blue()));

            Assert.That(_validator.CanMultiCast(new[] { _source }, _target), Is.True);
        }

        [Test]
        public void CanMultiCast_FrozenSourceTopLayer_ReturnsFalse()
        {
            var frozenLayer = new OreLayer(DC(1f, 0f, 0f), 0.25f, OreColor.Red, false, LayerModifier.Frozen);
            _source.AddLayer(frozenLayer);
            _target.AddLayer(Layer(Blue()));
            _target.AddLayer(Layer(Blue()));

            Assert.That(_validator.CanMultiCast(new[] { _source }, _target), Is.False);
        }

        [Test]
        public void CanMultiCast_ColorsWithinTolerance_ReturnsTrue()
        {
            var src1 = new MoldState(4);
            var src2 = new MoldState(4);
            src1.AddLayer(Layer(DC(0.50f, 0.20f, 0.80f)));
            src2.AddLayer(Layer(DC(0.52f, 0.22f, 0.78f)));
            _target.AddLayer(Layer(Blue()));
            _target.AddLayer(Layer(Blue()));

            Assert.That(_validator.CanMultiCast(new[] { src1, src2 }, _target), Is.True);
        }
    }
}
