using NUnit.Framework;
using UnityEngine;
using System.Reflection;
using PuzzleGame.Infrastructure.Implementations;
using PuzzleGame.Application.Interfaces;

namespace PuzzleGame.Tests.Infrastructure
{
    [TestFixture]
    public class UpdateManagerTests
    {
        private GameObject _go;
        private UpdateManager _sut;

        private class FakeUpdateable : IUpdateable
        {
            public int UpdateCount { get; private set; }
            public float LastDeltaTime { get; private set; }

            public void OnUpdate(float deltaTime)
            {
                UpdateCount++;
                LastDeltaTime = deltaTime;
            }
        }

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("UpdateManager_TestOwner");
            _sut = _go.AddComponent<UpdateManager>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
        }

        [Test]
        public void RegisterAndUnregister_UpdatesObjectsCorrectly()
        {
            var updateable = new FakeUpdateable();
            _sut.Register(updateable);

            // Trigger Update via reflection since it's private
            var updateMethod = typeof(UpdateManager).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(updateMethod, "Update method should be found via reflection.");
            
            updateMethod.Invoke(_sut, null);

            Assert.AreEqual(1, updateable.UpdateCount);

            _sut.Unregister(updateable);
            updateMethod.Invoke(_sut, null);

            Assert.AreEqual(1, updateable.UpdateCount, "Should not update after unregistering.");
        }
    }
}
