using System;
using System.Collections.Generic;
using UnityEngine;

namespace PuzzleGame.Infrastructure.Pool
{
    /// <summary>
    /// Generic pooled MonoBehaviour factory.
    /// - Rent(): returns active instance (new or recycled)
    /// - Return(): deactivates and returns to pool
    /// - MaxSize guard: oldest inactive is destroyed if pool overflows
    /// - Prewarm(): pre-instantiates N inactive copies
    /// </summary>
    public class GameObjectPool<T> : IGameObjectPool<T> where T : Component
    {
        private readonly T _prefab;
        private readonly Stack<T> _inactive = new Stack<T>();
        private readonly HashSet<T> _active = new HashSet<T>();
        private readonly int _maxSize;
        private readonly Action<T> _onRent;
        private readonly Action<T> _onReturn;

        public int CountInactive => _inactive.Count;
        public int CountAll => _inactive.Count + _active.Count;

        public GameObjectPool(T prefab, int maxSize,
                              Action<T> onRent = null,
                              Action<T> onReturn = null)
        {
            if (prefab == null)
                throw new ArgumentNullException(nameof(prefab));

            _prefab = prefab;
            _maxSize = Mathf.Max(1, maxSize);
            _onRent = onRent;
            _onReturn = onReturn;
        }

        public T Rent(Transform parent = null)
        {
            T instance = null;

            // Try reuse
            while (instance == null && _inactive.Count > 0)
            {
                instance = _inactive.Pop();
                if (instance == null)
                    continue; // destroyed externally
            }

            if (instance == null)
            {
                instance = UnityEngine.Object.Instantiate(_prefab, parent);
                instance.gameObject.name = _prefab.name;
            }
            else if (parent != null)
            {
                instance.transform.SetParent(parent);
            }

            instance.gameObject.SetActive(true);
            _active.Add(instance);
            _onRent?.Invoke(instance);

            return instance;
        }

        public void Return(T instance)
        {
            if (instance == null) return;
            if (!_active.Remove(instance)) return; // already returned or unknown

            instance.gameObject.SetActive(false);
            instance.transform.SetParent(null);

            _onReturn?.Invoke(instance);

            // Overflow guard: destroy oldest inactive
            if (_inactive.Count >= _maxSize)
            {
                var oldest = _inactive.Count > 0 ? _inactive.Pop() : null;
                if (oldest != null)
                    UnityEngine.Object.Destroy(oldest.gameObject);
            }

            _inactive.Push(instance);
        }

        public void Prewarm(int count, Transform parent = null)
        {
            if (count <= 0) return;

            var temp = new List<T>(count);
            for (int i = 0; i < count; i++)
                temp.Add(Rent(parent));

            foreach (var t in temp)
                Return(t);
        }

        public void DestroyAll()
        {
            foreach (var t in _inactive)
                if (t != null)
                    UnityEngine.Object.Destroy(t.gameObject);
            foreach (var t in _active)
                if (t != null)
                    UnityEngine.Object.Destroy(t.gameObject);
            _inactive.Clear();
            _active.Clear();
        }
    }
}
