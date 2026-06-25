using System;
using System.Collections.Generic;
using Nexus.Core;
using PuzzleGame.Application.Interfaces;

namespace PuzzleGame.Application.Events
{
    /// <summary>
    /// Bridges IEventAggregator (legacy interface) to Nexus ISignalBus.
    /// All Publish/Subscribe calls go through Nexus SignalBus, gaining 0-GC,
    /// causal tracing, plugin interceptors, and error recovery.
    /// Replace registrations of EventAggregator with this class to leverage Nexus fully.
    /// </summary>
    public sealed class NexusEventAggregator : IEventAggregator, IDisposable
    {
        private readonly ISignalBus _signalBus;
        private readonly Dictionary<Type, List<(Delegate handler, ISignalSubscription sub)>> _subscriptions = new();

        public NexusEventAggregator(ISignalBus signalBus)
        {
            _signalBus = signalBus ?? throw new ArgumentNullException(nameof(signalBus));
        }

        /// <summary>
        /// Provides direct access to the underlying Nexus SignalBus for advanced use
        /// (e.g. FireAsync, FireThreadSafe, FireNextFrame).
        /// </summary>
        public ISignalBus SignalBus => _signalBus;

        public void Subscribe<T>(Action<T> handler) where T : struct
        {
            var sub = _signalBus.Subscribe(handler);
            var type = typeof(T);
            if (!_subscriptions.TryGetValue(type, out var list))
            {
                list = new List<(Delegate, ISignalSubscription)>();
                _subscriptions[type] = list;
            }
            list.Add((handler, sub));
        }

        public IDisposable SubscribeToken<T>(Action<T> handler) where T : struct
        {
            Subscribe(handler);
            return new SubscriptionToken<T>(this, handler);
        }

        public void Unsubscribe<T>(Action<T> handler) where T : struct
        {
            var type = typeof(T);
            if (!_subscriptions.TryGetValue(type, out var list))
                return;

            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (list[i].handler.Equals(handler) || list[i].handler == handler)
                {
                    list[i].sub.Dispose();
                    list.RemoveAt(i);
                }
            }

            if (list.Count == 0)
                _subscriptions.Remove(type);
        }

        public void Publish<T>(T eventArgs) where T : struct
        {
            if (_signalBus is ISignalBus bus)
            {
                // All game events are readonly structs — perfect for Nexus Fire<T>
                bus.Fire(eventArgs);
            }
        }

        public void Clear()
        {
            foreach (var kvp in _subscriptions)
            {
                foreach (var (_, sub) in kvp.Value)
                {
                    sub.Dispose();
                }
            }
            _subscriptions.Clear();
        }

        public void Clear<T>() where T : struct
        {
            var type = typeof(T);
            if (_subscriptions.TryGetValue(type, out var list))
            {
                foreach (var (_, sub) in list)
                    sub.Dispose();
                _subscriptions.Remove(type);
            }
        }

        public void Dispose()
        {
            Clear();
        }

        private sealed class SubscriptionToken<T> : IDisposable where T : struct
        {
            private readonly NexusEventAggregator _parent;
            private Action<T> _handler;
            private bool _disposed;

            public SubscriptionToken(NexusEventAggregator parent, Action<T> handler)
            {
                _parent = parent;
                _handler = handler;
            }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                if (_handler != null)
                {
                    _parent.Unsubscribe(_handler);
                    _handler = null;
                }
            }
        }
    }
}
