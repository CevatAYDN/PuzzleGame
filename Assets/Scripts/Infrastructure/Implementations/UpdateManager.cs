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
    ///
    /// Fix #5: Uses HashSet for O(1) Contains/Remove instead of List O(n).
    /// Deferred add/remove buffers remain as Lists since they are small and processed once per frame.
    /// </summary>
    [DefaultExecutionOrder(-100)] // Run before other game updates
    public class UpdateManager : MonoBehaviour, IUpdateManager
    {
        private readonly HashSet<IUpdateable> _updateables = new HashSet<IUpdateable>();
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
            else
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

            // Apply deferred adds
            if (_toAdd.Count > 0)
            {
                foreach (var item in _toAdd)
                    _updateables.Add(item);
                _toAdd.Clear();
            }

            // Iterate over a snapshot to avoid modification during iteration
            // (modifications are captured in _toAdd/_toRemove for next frame)
            var snapshot = _updateables.Count > 0 ? new List<IUpdateable>(_updateables) : null;
            if (snapshot != null)
            {
                foreach (var u in snapshot)
                    u.OnUpdate(deltaTime);
            }

            _isUpdating = false;
        }
    }
}
