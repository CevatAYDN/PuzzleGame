using System;
using System.Collections.Generic;
using BottleShaders.Logging;

namespace BottleShaders.Events
{
    /// <summary>
    /// Lightweight, type-safe publish/subscribe bus.
    /// Use it to decouple systems that should not reference each other directly
    /// (e.g. GameManager → UI, AudioManager, ParticleManager).
    ///
    /// Usage:
    ///   EventAggregator.Subscribe&lt;PourCompletedEvent&gt;(OnPourCompleted);
    ///   EventAggregator.Publish(new PourCompletedEvent(source, target));
    ///   EventAggregator.Unsubscribe&lt;PourCompletedEvent&gt;(OnPourCompleted);
    /// </summary>
    public static class EventAggregator
    {
        private static readonly Dictionary<Type, List<Delegate>> _subscribers =
            new Dictionary<Type, List<Delegate>>();

        // ── Subscribe / Unsubscribe ──────────────────────────────────────────

        public static void Subscribe<T>(Action<T> handler)
        {
            if (handler == null) return;

            var type = typeof(T);
            if (!_subscribers.TryGetValue(type, out var list))
            {
                list = new List<Delegate>();
                _subscribers[type] = list;
            }
            list.Add(handler);
        }

        public static void Unsubscribe<T>(Action<T> handler)
        {
            if (handler == null) return;

            if (_subscribers.TryGetValue(typeof(T), out var list))
                list.Remove(handler);
        }

        // ── Publish ──────────────────────────────────────────────────────────

        public static void Publish<T>(T eventArgs)
        {
            if (!_subscribers.TryGetValue(typeof(T), out var list) || list.Count == 0)
                return;

            // Iterate over a snapshot so handlers can safely unsubscribe during dispatch
            var snapshot = list.ToArray();
            foreach (var handler in snapshot)
            {
                try
                {
                    ((Action<T>)handler).Invoke(eventArgs);
                }
                catch (Exception ex)
                {
                    BottleLogger.LogError($"EventAggregator: handler threw for event {typeof(T).Name}: {ex}");
                }
            }
        }

        /// <summary>Removes all subscribers — call on scene unload to prevent stale references.</summary>
        public static void Clear() => _subscribers.Clear();
    }
}
