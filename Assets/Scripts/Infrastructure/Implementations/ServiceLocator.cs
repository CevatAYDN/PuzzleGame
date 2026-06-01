using System;
using System.Collections.Generic;
using UnityEngine;

namespace PuzzleGame.Infrastructure.Implementations
{
    /// <summary>
    /// Minimal service locator. VContainer gelince tüm Register()/Resolve()
    /// çağrıları VContainer API'sine redirect edilir — sadece bu dosya değişir.
    /// </summary>
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> _services = new Dictionary<Type, object>(32);
        private static bool _initialized;

        public static T Get<T>() where T : class
        {
            if (_services.TryGetValue(typeof(T), out var svc)) return svc as T;
            Debug.LogError($"[ServiceLocator] {typeof(T).Name} not registered.");
            return null;
        }

        public static void Set<T>(T instance) where T : class
        {
            if (instance != null) _services[typeof(T)] = instance;
        }

        public static void Clear() { _services.Clear(); _initialized = false; }
        public static bool IsInitialized => _initialized;
        public static void MarkInitialized() => _initialized = true;
    }
}
