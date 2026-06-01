using System;
using System.Collections.Generic;
using UnityEngine;

namespace PuzzleGame.Infrastructure.Pool
{
    /// <summary>
    /// Central pool registry. Manages named pools for different object types.
    /// - RegisterPool: creates a new pool by name
    /// - Rent/Return: type-safe access via named pools
    /// - Cleanup: destroy all pooled objects (scene unload)
    /// </summary>
    public sealed class PoolManager
    {
        private readonly Dictionary<string, INamedPool> _pools = new Dictionary<string, INamedPool>();

        public static PoolManager Instance { get; } = new PoolManager();

        private PoolManager() { }

        /// <summary>
        /// Register a pool for a component type under a name.
        /// </summary>
        public IGameObjectPool<T> RegisterPool<T>(string name, T prefab, int maxSize,
                                                   Action<T> onRent = null,
                                                   Action<T> onReturn = null)
            where T : Component
        {
            if (_pools.ContainsKey(name))
                return ((PoolEntry<T>)_pools[name]).Pool;

            var pool = new GameObjectPool<T>(prefab, maxSize, onRent, onReturn);
            var entry = new PoolEntry<T>(pool);
            _pools[name] = entry;
            return pool;
        }

        /// <summary>
        /// Get an existing pool by name.
        /// </summary>
        public IGameObjectPool<T> GetPool<T>(string name) where T : Component
        {
            return _pools.TryGetValue(name, out var entry)
                ? ((PoolEntry<T>)entry).Pool
                : null;
        }

        /// <summary>
        /// Rent an instance from a named pool.
        /// </summary>
        public T Rent<T>(string name, Transform parent = null) where T : Component
        {
            if (!_pools.TryGetValue(name, out var entry))
            {
                Debug.LogError($"[PoolManager] Pool '{name}' not registered.");
                return null;
            }
            return ((PoolEntry<T>)entry).Pool.Rent(parent);
        }

        /// <summary>
        /// Return an instance to a named pool.
        /// </summary>
        public void Return<T>(string name, T instance) where T : Component
        {
            if (!_pools.TryGetValue(name, out var entry))
            {
                Debug.LogError($"[PoolManager] Pool '{name}' not registered.");
                return;
            }
            ((PoolEntry<T>)entry).Pool.Return(instance);
        }

        /// <summary>
        /// Prewarm a named pool.
        /// </summary>
        public void Prewarm(string name, int count, Transform parent = null)
        {
            if (_pools.TryGetValue(name, out var entry))
                entry.Prewarm(count, parent);
        }

        /// <summary>
        /// Log all pool statistics.
        /// </summary>
        public void LogAllStats()
        {
            foreach (var kvp in _pools)
            {
                var e = kvp.Value;
                Debug.Log($"[PoolManager] '{kvp.Key}': active={e.CountActive}, inactive={e.CountInactive}, total={e.CountAll}");
            }
        }

        /// <summary>
        /// Destroy all pooled objects and clear the registry.
        /// Call on scene unload.
        /// </summary>
        public void Cleanup()
        {
            foreach (var kvp in _pools)
                kvp.Value.DestroyAll();
            _pools.Clear();
        }

        /// <summary>
        /// Remove and clean up a specific named pool.
        /// </summary>
        public void RemovePool(string name)
        {
            if (_pools.TryGetValue(name, out var entry))
            {
                entry.DestroyAll();
                _pools.Remove(name);
            }
        }

        // Internal non-generic interface for heterogeneous management
        private interface INamedPool
        {
            int CountInactive { get; }
            int CountActive { get; }
            int CountAll { get; }
            void Prewarm(int count, Transform parent = null);
            void DestroyAll();
        }

        private class PoolEntry<T> : INamedPool where T : Component
        {
            public IGameObjectPool<T> Pool { get; }
            public int CountInactive => Pool.CountInactive;
            public int CountActive => Pool.CountAll - Pool.CountInactive;
            public int CountAll => Pool.CountAll;
            public PoolEntry(IGameObjectPool<T> pool) => Pool = pool;
            public void Prewarm(int count, Transform parent = null) => Pool.Prewarm(count, parent);
            public void DestroyAll() => Pool.DestroyAll();
        }
    }
}
