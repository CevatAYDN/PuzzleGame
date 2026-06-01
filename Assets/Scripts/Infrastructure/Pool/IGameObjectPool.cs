using UnityEngine;

namespace PuzzleGame.Infrastructure.Pool
{
    /// <summary>
    /// Generic pool contract. Type-safe, capacity-bound.
    /// </summary>
    public interface IGameObjectPool<T> where T : Component
    {
        T Rent(Transform parent = null);
        void Return(T instance);
        int CountInactive { get; }
        int CountAll { get; }
        void Prewarm(int count, Transform parent = null);
        void DestroyAll();
    }
}
