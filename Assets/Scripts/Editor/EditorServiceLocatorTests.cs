using NUnit.Framework;
using PuzzleGame.Domain.Interfaces;
using PuzzleGame.Domain.Services;
using PuzzleGame.Domain.Models;
using System.Collections.Generic;

namespace PuzzleGame.Editor.Tests
{
    [TestFixture]
    public class EditorServiceLocatorTests
    {
        [Test]
        public void Get_RegisteredService_ReturnsInstance()
        {
            // Act
            var service = EditorServiceLocator.Get<IMoldValidator>();

            // Assert
            Assert.IsNotNull(service);
            Assert.IsInstanceOf<MoldValidationService>(service);
        }

        [Test]
        public void Register_NewService_RetrievesSameInstance()
        {
            // Arrange
            var testService = new FakeMoldValidator();

            // Act
            EditorServiceLocator.Register<IMoldValidator>(testService);
            var retrieved = EditorServiceLocator.Get<IMoldValidator>();

            // Assert
            Assert.AreSame(testService, retrieved);

            // Cleanup: reset default
            EditorServiceLocator.Register<IMoldValidator>(new MoldValidationService(Domain.ForgeConstants.ColorMatchEpsilon));
        }

        private class FakeMoldValidator : IMoldValidator
        {
            public bool CanCast(MoldState source, MoldState target) => true;
            public bool CanMultiCast(MoldState[] sources, MoldState target) => true;
            public bool CanBreakCork(MoldState source, MoldState target) => true;
            public bool IsComplete(MoldState Mold) => true;
            public bool ColorsMatch(DomainColor a, DomainColor b) => true;
        }
    }
}
