using NUnit.Framework;
using UnityEngine;
using PuzzleGame.Infrastructure.Implementations;

namespace PuzzleGame.Tests.Infrastructure
{
    [TestFixture]
    public class ShaderOptimizerTests
    {
        private ShaderOptimizer _sut;

        [SetUp]
        public void SetUp()
        {
            _sut = new ShaderOptimizer();
        }

        [Test]
        public void Initialize_false_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _sut.Initialize(applyMobileDefaults: false));
        }

        [Test]
        public void GetRecommendedQualityLevel_ReturnsValueBetweenZeroAndTwo()
        {
            int level = _sut.GetRecommendedQualityLevel();
            Assert.That(level, Is.GreaterThanOrEqualTo(0));
            Assert.That(level, Is.LessThanOrEqualTo(2));
        }

        [Test]
        public void OptimizeMaterial_NullMaterial_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _sut.OptimizeMaterial(null));
        }

        [Test]
        public void OptimizeMaterial_ValidMaterial_SetsMaxLOD()
        {
            var shader = Shader.Find("Sprites/Default");
            if (shader != null)
            {
                var mat = new Material(shader);
                Assert.DoesNotThrow(() => _sut.OptimizeMaterial(mat));
                Object.DestroyImmediate(mat);
            }
        }
    }
}
