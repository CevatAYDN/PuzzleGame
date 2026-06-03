using System.Collections.Generic;
using PuzzleGame.Domain.Interfaces;
using UnityEngine;

namespace PuzzleGame.Infrastructure.Implementations
{
    /// <summary>
    /// Central manager that executes updates for all registered IUpdateable objects
    /// in a single frame update, reducing Native-to-Managed MonoBehaviour.Update overhead.
    /// Handles registration and unregistration safely during updates.
    /// </summary>
    [DefaultExecutionOrder(-100)] // Run before other game updates
    public class UpdateManager : MonoBehaviour, IUpdateManager
    {
        private static UpdateManager _instance;
        private static bool _applicationIsQuitting;

        public static UpdateManager Instance
        {
            get
            {
                if (_applicationIsQuitting)
                {
                    return null;
                }

                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<UpdateManager>();
                    if (_instance == null)
                    {
                        var go = new GameObject("UpdateManager");
                        _instance = go.AddComponent<UpdateManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        private readonly List<IUpdateable> _updateables = new List<IUpdateable>(64);
        private readonly List<IUpdateable> _toAdd = new List<IUpdateable>(16);
        private readonly List<IUpdateable> _toRemove = new List<IUpdateable>(16);
        private bool _isUpdating;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        public void Register(IUpdateable updateable)
        {
            if (updateable == null) return;

            if (_isUpdating)
            {
                if (!_toAdd.Contains(updateable) && !_updateables.Contains(updateable))
                    _toAdd.Add(updateable);
            }
            else
            {
                if (!_updateables.Contains(updateable))
                    _updateables.Add(updateable);
            }
        }

        public void Unregister(IUpdateable updateable)
        {
            if (updateable == null) return;

            if (_isUpdating)
            {
                if (!_toRemove.Contains(updateable))
                    _toRemove.Add(updateable);
            }
            else
            {
                _updateables.Remove(updateable);
            }
        }

        private void Update()
        {
            _isUpdating = true;
            float deltaTime = Time.deltaTime;

            int count = _updateables.Count;
            for (int i = 0; i < count; i++)
            {
                var updateable = _updateables[i];
                bool isDestroyed = updateable == null || (updateable is UnityEngine.Object obj && obj == null);
                if (!isDestroyed)
                {
                    try
                    {
                        updateable.OnUpdate(deltaTime);
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"UpdateManager: Error updating object {updateable.GetType().Name}: {ex}");
                    }
                }
                else
                {
                    // Stale/destroyed reference cleanup to prevent memory leaks
                    if (!_toRemove.Contains(updateable))
                        _toRemove.Add(updateable);
                }
            }

            _isUpdating = false;

            // Apply deferred additions/removals
            if (_toAdd.Count > 0)
            {
                for (int i = 0; i < _toAdd.Count; i++)
                {
                    var item = _toAdd[i];
                    if (!_updateables.Contains(item))
                        _updateables.Add(item);
                }
                _toAdd.Clear();
            }

            if (_toRemove.Count > 0)
            {
                for (int i = 0; i < _toRemove.Count; i++)
                {
                    _updateables.Remove(_toRemove[i]);
                }
                _toRemove.Clear();
            }
        }

        private void OnApplicationQuit()
        {
            _applicationIsQuitting = true;
            _instance = null;
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}
