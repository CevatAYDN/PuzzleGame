using NUnit.Framework;
using PuzzleGame.Domain.Models;
using PuzzleGame.Infrastructure;
using UnityEngine;

namespace PuzzleGame.Tests.Infrastructure
{
    /// <summary>
    /// Unit tests for ColorAdapter (Domain ↔ Unity Color round-trip).
    /// Fix Test Audit: 0 → full coverage of FromUnity, ToUnity, round-trips.
    /// </summary>
    [TestFixture]
    public class ColorAdapterTests
    {
        private ColorAdapter _sut;

        [SetUp]
        public void SetUp() => _sut = new ColorAdapter();

        // ─── FromUnity ──────────────────────────────────────────────────────────

        [Test]
        public void FromUnity_ConvertsChannelsCorrectly()
        {
            var unityColor = new Color(0.8f, 0.4f, 0.2f, 1f);
            DomainColor result = _sut.FromUnity(unityColor);

            Assert.AreEqual(0.8f, result.R, 0.001f);
            Assert.AreEqual(0.4f, result.G, 0.001f);
            Assert.AreEqual(0.2f, result.B, 0.001f);
            Assert.AreEqual(1.0f, result.A, 0.001f);
        }

        [Test]
        [TestCase(0f, 0f, 0f, 0f)]     // Black transparent
        [TestCase(1f, 1f, 1f, 1f)]     // White opaque
        [TestCase(1f, 0f, 0f, 1f)]     // Pure red
        [TestCase(0f, 1f, 0f, 1f)]     // Pure green
        [TestCase(0f, 0f, 1f, 1f)]     // Pure blue
        public void FromUnity_TestCases(float r, float g, float b, float a)
        {
            var c = new Color(r, g, b, a);
            var d = _sut.FromUnity(c);
            Assert.AreEqual(r, d.R, 0.001f);
            Assert.AreEqual(g, d.G, 0.001f);
            Assert.AreEqual(b, d.B, 0.001f);
            Assert.AreEqual(a, d.A, 0.001f);
        }

        // ─── ToUnity ────────────────────────────────────────────────────────────

        [Test]
        public void ToUnity_ConvertsChannelsCorrectly()
        {
            var domain = new DomainColor(0.3f, 0.6f, 0.9f, 0.5f);
            Color result = _sut.ToUnity(domain);

            Assert.AreEqual(0.3f, result.r, 0.001f);
            Assert.AreEqual(0.6f, result.g, 0.001f);
            Assert.AreEqual(0.9f, result.b, 0.001f);
            Assert.AreEqual(0.5f, result.a, 0.001f);
        }

        // ─── Round-trip ─────────────────────────────────────────────────────────

        [Test]
        public void RoundTrip_UnityToDomainToUnity_PreservesValues()
        {
            var original = new Color(0.73f, 0.12f, 0.55f, 0.88f);
            var domain = _sut.FromUnity(original);
            var back = _sut.ToUnity(domain);

            Assert.AreEqual(original.r, back.r, 0.001f, "R channel round-trip failed.");
            Assert.AreEqual(original.g, back.g, 0.001f, "G channel round-trip failed.");
            Assert.AreEqual(original.b, back.b, 0.001f, "B channel round-trip failed.");
            Assert.AreEqual(original.a, back.a, 0.001f, "A channel round-trip failed.");
        }

        [Test]
        public void RoundTrip_DomainToUnityToDomain_PreservesValues()
        {
            var original = new DomainColor(0.25f, 0.50f, 0.75f, 1f);
            var unity = _sut.ToUnity(original);
            var back = _sut.FromUnity(unity);

            Assert.AreEqual(original.R, back.R, 0.001f);
            Assert.AreEqual(original.G, back.G, 0.001f);
            Assert.AreEqual(original.B, back.B, 0.001f);
            Assert.AreEqual(original.A, back.A, 0.001f);
        }
    }
}
