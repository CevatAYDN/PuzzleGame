using System.Reflection;
using NUnit.Framework;
using PuzzleGame.Presentation;
using UnityEngine;

namespace PuzzleGame.Tests.Presentation
{
    [TestFixture]
    public class ErrorIndicatorBootstrapTests
    {
        [Test]
        public void EnsureExists_HasCorrectSignature()
        {
            var method = typeof(ErrorIndicatorBootstrap).GetMethod(
                "EnsureExists",
                BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(method, "EnsureExists() static method must exist");
            Assert.AreEqual(typeof(ErrorIndicatorController), method.ReturnType);
            Assert.AreEqual(0, method.GetParameters().Length, "EnsureExists() must take no parameters");
        }

        [Test]
        public void AutoCreatedName_ContainsErrorIndicator()
        {
            Assert.That(ErrorIndicatorBootstrap.AutoCreatedName, Does.Contain("ErrorIndicator"));
        }

        [Test]
        public void AutoCreatedName_ContainsAutoCreatedHint()
        {
            Assert.That(ErrorIndicatorBootstrap.AutoCreatedName, Does.Contain("auto-created"));
        }

        [Test, Explicit("Requires Unity runtime; run as PlayMode test")]
        public void EnsureExists_NoExistingController_CreatesNew()
        {
            var preExisting = Object.FindAnyObjectByType<ErrorIndicatorController>();
            Object.DestroyImmediate(preExisting);

            var result = ErrorIndicatorBootstrap.EnsureExists();

            Assert.IsNotNull(result);
            Assert.IsNotNull(result.gameObject);
            Assert.AreEqual(ErrorIndicatorBootstrap.AutoCreatedName, result.gameObject.name);

            Object.DestroyImmediate(result.gameObject);
        }

        [Test, Explicit("Requires Unity runtime; run as PlayMode test")]
        public void EnsureExists_ExistingController_ReturnsExisting()
        {
            var preExisting = Object.FindAnyObjectByType<ErrorIndicatorController>();
            Object.DestroyImmediate(preExisting);
            var first = ErrorIndicatorBootstrap.EnsureExists();
            var second = ErrorIndicatorBootstrap.EnsureExists();

            Assert.AreSame(first, second, "Second call must return the existing instance, not create a duplicate");

            Object.DestroyImmediate(first.gameObject);
        }
    }
}
