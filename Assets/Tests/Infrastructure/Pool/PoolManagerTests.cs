using NUnit.Framework;
using PuzzleGame.Infrastructure.Pool;
using PuzzleGame.Application.Interfaces;
using UnityEngine;

namespace PuzzleGame.Tests.Infrastructure.Pool
{
    [TestFixture]
    public class PoolManagerTests
    {
        private class TestComponent : MonoBehaviour { }

        private PoolManager _sut;
        private TestComponent _prefab;

        [SetUp]
        public void Setup()
        {
            _sut = new PoolManager();
            var go = new GameObject("TestPrefab");
            go.SetActive(false);
            _prefab = go.AddComponent<TestComponent>();
        }

        [TearDown]
        public void Teardown()
        {
            _sut.Dispose();
            if (_prefab != null)
                Object.DestroyImmediate(_prefab.gameObject);
        }

        [Test]
        public void RegisterAndGetPool_CreatesAndRetrievesSuccessfully()
        {
            var pool = _sut.RegisterPool("TestPool", _prefab, 5);
            Assert.IsNotNull(pool);

            var retrieved = _sut.GetPool<TestComponent>("TestPool");
            Assert.AreSame(pool, retrieved);
        }

        [Test]
        public void RentAndReturn_WorksViaPoolManagerAPI()
        {
            _sut.RegisterPool("TestPool", _prefab, 5);
            
            var rented = _sut.Rent<TestComponent>("TestPool");
            Assert.IsNotNull(rented);

            Assert.DoesNotThrow(() => _sut.Return("TestPool", rented));
        }

        [Test]
        public void LogAllStats_DoesNotThrow()
        {
            _sut.RegisterPool("TestPool", _prefab, 5);
            Assert.DoesNotThrow(() => _sut.LogAllStats());
        }
    }
}
