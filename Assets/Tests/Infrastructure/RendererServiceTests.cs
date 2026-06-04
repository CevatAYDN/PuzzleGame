using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using PuzzleGame.Domain;
using PuzzleGame.Domain.Models;
using PuzzleGame.Infrastructure.Implementations;
using PuzzleGame.Infrastructure;

namespace PuzzleGame.Tests.Infrastructure
{
    [TestFixture]
    public class RendererServiceTests
    {
        private RendererService _sut;
        private GameObject _go;
        private MeshRenderer _renderer;

        [SetUp]
        public void SetUp()
        {
            _sut = new RendererService(new ColorAdapter());
            _go = new GameObject("TestRendererOwner");
            _renderer = _go.AddComponent<MeshRenderer>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
        }

        [Test]
        public void UpdateGlass_IsEmpty_AppliesGlassProperties()
        {
            Assert.DoesNotThrow(() => _sut.UpdateGlass(_renderer, isEmpty: true, new DomainColor(0, 0, 0, 0), null));
            
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            _renderer.GetPropertyBlock(block, 0);
            
            Color actual = block.GetColor("_Color");
            Assert.AreEqual(1.0f, actual.r, 0.01f);
        }

        [Test]
        public void UpdateGlass_NotEmpty_AppliesTintedProperties()
        {
            var tint = new DomainColor(1f, 0f, 0f, 1f); // Red
            Assert.DoesNotThrow(() => _sut.UpdateGlass(_renderer, isEmpty: false, tint, null));
            
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            _renderer.GetPropertyBlock(block, 0);
            
            Color actual = block.GetColor("_Color");
            Assert.AreEqual(0.25f, actual.a, 0.01f);
        }

        [Test]
        public void UpdateOre_ValidLayers_DoesNotThrow()
        {
            var layers = new List<OreLayer>
            {
                new OreLayer(new DomainColor(0.8f, 0f, 0f, 1f), 1f),
                new OreLayer(new DomainColor(0.8f, 0f, 0f, 1f), 1f), // consecutive same color gets merged
                new OreLayer(new DomainColor(0f, 0.8f, 0f, 1f), 1f)
            };

            Assert.DoesNotThrow(() => _sut.UpdateOre(_renderer, layers, 3f, null));
        }

        [Test]
        public void UpdateOre_NullArguments_ThrowsException()
        {
            Assert.Throws<System.ArgumentNullException>(() => _sut.UpdateOre(null, new List<OreLayer>(), 1f, null));
            Assert.Throws<System.ArgumentNullException>(() => _sut.UpdateOre(_renderer, null, 1f, null));
        }
    }
}
