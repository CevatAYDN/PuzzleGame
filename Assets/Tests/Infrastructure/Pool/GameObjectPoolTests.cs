using NUnit.Framework;
using PuzzleGame.Infrastructure.Pool;
using UnityEngine;

namespace PuzzleGame.Tests.Infrastructure.Pool
{
    public class GameObjectPoolTests
    {
        private class TestComponent : MonoBehaviour { }

        private GameObjectPool<TestComponent> _pool;
        private TestComponent _prefab;

        [SetUp]
        public void Setup()
        {
            var go = new GameObject("TestPrefab");
            go.SetActive(false);
            _prefab = go.AddComponent<TestComponent>();
            _pool = new GameObjectPool<TestComponent>(_prefab, 5);
        }

        [TearDown]
        public void Teardown()
        {
            // Clean up ALL pooled objects (both active and inactive)
            _pool.DestroyAll();
            if (_prefab != null)
                Object.DestroyImmediate(_prefab.gameObject);
        }

        [Test]
        public void Constructor_WithNullPrefab_Throws()
        {
            Assert.That(() => new GameObjectPool<TestComponent>(null, 5),
                Throws.ArgumentNullException);
        }

        [Test]
        public void Rent_ReturnsActiveInstance()
        {
            var instance = _pool.Rent();
            Assert.That(instance, Is.Not.Null);
            Assert.That(instance.gameObject.activeInHierarchy, Is.True);
        }

        [Test]
        public void CountAll_AfterRent_Increases()
        {
            Assert.That(_pool.CountAll, Is.EqualTo(0));
            _pool.Rent();
            Assert.That(_pool.CountAll, Is.EqualTo(1));
        }

        [Test]
        public void Return_SetsInactive()
        {
            var instance = _pool.Rent();
            _pool.Return(instance);
            Assert.That(instance.gameObject.activeSelf, Is.False);
            Assert.That(_pool.CountInactive, Is.EqualTo(1));
        }

        [Test]
        public void Rent_AfterReturn_GivesSameInstance()
        {
            var first = _pool.Rent();
            _pool.Return(first);

            var second = _pool.Rent();
            Assert.That(ReferenceEquals(second, first), Is.True);
        }

        [Test]
        public void Rent_AfterMultipleReturns_GivesOneOfThem()
        {
            var a = _pool.Rent();
            var b = _pool.Rent();

            _pool.Return(a);
            _pool.Return(b);

            var reused = _pool.Rent();
            Assert.That(ReferenceEquals(reused, a) || ReferenceEquals(reused, b), Is.True);
        }

        [Test]
        public void Return_Twice_DoesNotDuplicate()
        {
            var instance = _pool.Rent();
            _pool.Return(instance);
            _pool.Return(instance); // second call is no-op

            Assert.That(_pool.CountInactive, Is.EqualTo(1));
        }

        [Test]
        public void Return_UnknownInstance_DoesNotAddToPool()
        {
            var foreign = new GameObject("Foreign").AddComponent<TestComponent>();
            _pool.Return(foreign);
            Assert.That(_pool.CountInactive, Is.EqualTo(0));
            Object.DestroyImmediate(foreign.gameObject);
        }

        [Test]
        public void Rent_BeyondMaxSize_DestroysOldest()
        {
            var smallPool = new GameObjectPool<TestComponent>(_prefab, 2);
            var instances = new System.Collections.Generic.List<TestComponent>();

            // Rent 2, return 2 (stack has 2)
            for (int i = 0; i < 2; i++) instances.Add(smallPool.Rent());
            foreach (var inst in instances) smallPool.Return(inst);
            instances.Clear();

            Assert.That(smallPool.CountInactive, Is.EqualTo(2));

            // Rent 2 more, return 3rd → first should be destroyed
            var c = smallPool.Rent();
            smallPool.Return(c);
            // MaxSize=2, after returning overflow, oldest is destroyed
            Assert.That(smallPool.CountInactive, Is.EqualTo(2));
        }

        [Test]
        public void Prewarm_PopulatesStack()
        {
            _pool.Prewarm(3);
            Assert.That(_pool.CountInactive, Is.EqualTo(3));
            Assert.That(_pool.CountAll, Is.EqualTo(3));
        }

        [Test]
        public void OnRent_Callback_Invoked()
        {
            bool called = false;
            var pool = new GameObjectPool<TestComponent>(_prefab, 5,
                onRent: c => called = true);

            pool.Rent();
            Assert.That(called, Is.True);
        }

        [Test]
        public void OnReturn_Callback_Invoked()
        {
            bool called = false;
            var pool = new GameObjectPool<TestComponent>(_prefab, 5,
                onReturn: c => called = true);

            var inst = pool.Rent();
            pool.Return(inst);
            Assert.That(called, Is.True);
        }
    }
}
