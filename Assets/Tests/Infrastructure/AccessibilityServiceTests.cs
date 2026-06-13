using NUnit.Framework;
using PuzzleGame.Domain.Models;
using PuzzleGame.Domain.Exceptions;
using PuzzleGame.Infrastructure.Implementations;
using PuzzleGame.Application.Configuration;
using PuzzleGame.Application.Interfaces;
using UnityEngine;

namespace PuzzleGame.Tests.Infrastructure
{
    [TestFixture]
    public class AccessibilityServiceTests
    {
        private AccessibilityConfig _config;
        private AccessibilityService _sut;

        [SetUp]
        public void SetUp()
        {
            // Create a mock/runtime instance of AccessibilityConfig ScriptableObject
            _config = ScriptableObject.CreateInstance<AccessibilityConfig>();
            
            // Set up a standard palette mapping
            _config.patternByColorIndex = new DomainPattern[]
            {
                DomainPattern.None,       // 0 - unused
                DomainPattern.Dots,       // 1 - Red
                DomainPattern.Stripes,    // 2 - Blue
                DomainPattern.None,       // 3 - Green (simulate missing pattern!)
                DomainPattern.Waves,      // 4 - Yellow
                DomainPattern.Zigzag,     // 5 - Orange
                DomainPattern.Checkered,  // 6 - Purple
                DomainPattern.Rings,      // 7 - Cyan
                DomainPattern.Diamonds,   // 8 - Pink
                DomainPattern.Checkered,  // 9 - Brown
                DomainPattern.Stars,      // 10 - White
                DomainPattern.None,       // 11 - Black (solid, none needed)
            };

            _sut = new AccessibilityService(_config);
            // Default to None at start of tests
            _sut.SetColorBlindMode(ColorBlindMode.None);
        }

        [TearDown]
        public void TearDown()
        {
            if (_config != null)
            {
                Object.DestroyImmediate(_config);
            }
        }

        [Test]
        public void GetPatternForColor_WhenDisabled_ReturnsNone()
        {
            _sut.SetColorBlindMode(ColorBlindMode.None);
            
            var pattern = _sut.GetPatternForColor(1); // Red
            
            Assert.That(pattern, Is.EqualTo(DomainPattern.None));
        }

        [Test]
        public void GetPatternForColor_WhenEnabledAndValid_ReturnsConfiguredPattern()
        {
            _sut.SetColorBlindMode(ColorBlindMode.Deuteranopia);
            
            var pattern = _sut.GetPatternForColor(1); // Red
            
            Assert.That(pattern, Is.EqualTo(DomainPattern.Dots));
        }

        [Test]
        public void GetPatternForColor_WhenMissingPatternForColor1To10_ThrowsMissingAccessibilityFallbackException()
        {
            _sut.SetColorBlindMode(ColorBlindMode.Protanopia);
            
            // Color 3 (Green) maps to DomainPattern.None in our test setup
            Assert.Throws<MissingAccessibilityFallbackException>(() =>
            {
                _sut.GetPatternForColor(3);
            });
        }

        [Test]
        public void GetPatternForColor_WhenColorIs11Black_DoesNotThrowEvenIfPatternIsNone()
        {
            _sut.SetColorBlindMode(ColorBlindMode.Tritanopia);
            
            var pattern = _sut.GetPatternForColor(11); // Black
            
            Assert.That(pattern, Is.EqualTo(DomainPattern.None));
        }
    }
}
