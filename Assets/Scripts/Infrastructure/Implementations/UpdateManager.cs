using System.Collections.Generic;
using PuzzleGame.Application.Interfaces;
using UnityEngine;

namespace PuzzleGame.Infrastructure.Implementations
{
    /// <summary>
    /// Central manager that executes updates for all registered IUpdateable objects
    /// in a single frame update, reducing Native-to-Managed MonoBehaviour.Update overhead.
    /// Handles registration and unregistration safely during updates.
    ///
    /// No longer a singleton — managed by VContainer DI (Lifetime.Singleton).
    /// </summary>
    [DefaultExecutionOrder(-100)] // Run before other game updates
    public class UpdateManager : MonoBehaviour, IUpdateManager
    {
        private readonly List<IUpdateable> _updateables = new List<IUpdateable>(64);
        private readonly List<IUpdateable> _toAdd = new List<IUpdateable>(16);
        private readonly List<IUpdateable> _toRemove = new List<IUpdateable>(16);
        private bool _isUpdating;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        public void Register(IUpdateable updateable)
        {
            if (updateable == null) return;

            if (_isUpdating)
                _toAdd.Add(updateable);
            else if (!_updateables.Contains(updateable))
                _updateables.Add(updateable);
        }

        public void Unregister(IUpdateable updateable)
        {
            if (updateable == null) return;

            if (_isUpdating)
                _toRemove.Add(updateable);
            else
                _updateables.Remove(updateable);
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            _isUpdating = true;

            // Deferred remove first (cleanup from previous frame)
            if (_toRemove.Count > 0)
            {
                foreach (var item in _toRemove)
                    _updateables.Remove(item);
                _toRemove.Clear();
            }

            for (int i = 0; i < _updateables.Count; i++)
            {
                var updatable = _updateables[i];
                if (updatable != null)
                    updatable.OnUpdate(deltaTime);
            }

            _isUpdating = false;

            // Deferred add
            if (_toAdd.Count > 0)
            {
                foreach (var item in _toAdd)
                {
                    if (!_updateables.Contains(item))
                        _updateables.Add(item);
                }
                _toAdd.Clear();
            }
        }
    }
}
