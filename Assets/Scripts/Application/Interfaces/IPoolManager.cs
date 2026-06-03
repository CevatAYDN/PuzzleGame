using System;
using UnityEngine;

namespace PuzzleGame.Application.Interfaces
{
    /// <summary>
    /// Pool registry contract. Infrastructure'daki PoolManager bu interface'i uygular.
    /// Application katmanı kendi havuz ihtiyaçlarını bu soyutlama üzerinden karşılar.
    /// </summary>
    public interface IPoolManager : IDisposable
    {
        /// <summary>
        /// Register a pool for a component type under a name.
        /// Returns the created pool for direct access.
        /// </summary>
        IGameObjectPool<T> RegisterPool<T>(string name, T prefab, int maxSize,
                                           Action<T> onRent = null,
                                           Action<T> onReturn = null)
            where T : Component;

        /// <summary>
        /// Get an existing pool by name. Returns null if not found.
        /// </summary>
        IGameObjectPool<T> GetPool<T>(string name) where T : Component;

        /// <summary>
        /// Rent an instance from a named pool.
        /// </summary>
        T Rent<T>(string name, Transform parent = null) where T : Component;

        /// <summary>
        /// Return an instance to its named pool.
        /// </summary>
        void Return<T>(string name, T instance) where T : Component;

        /// <summary>
        /// Prewarm a named pool with the specified count.
        /// </summary>
        void Prewarm<T>(string name, int count, Transform parent = null) where T : Component;

        /// <summary>
        /// Remove a pool by name and destroy all its objects.
        /// </summary>
        void RemovePool<T>(string name) where T : Component;

        /// <summary>
        /// Destroy all pools and their objects.
        /// </summary>
        void Cleanup();

        /// <summary>
        /// Log pool statistics for debugging.
        /// </summary>
        void LogAllStats();
    }
}
