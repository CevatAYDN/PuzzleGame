using System;
using System.Collections.Generic;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Logging;

namespace PuzzleGame.Application.Events
{
    /// <summary>
    /// Lightweight, type-safe publish/subscribe bus.
    /// Instance-based — inject via IEventAggregator for testability.
    /// </summary>
    /// <remarks>
    /// Unity is single-threaded on the main thread, so locking is unnecessary.
    /// This eliminates GC pressure from Monitor allocations.
    /// </remarks>
    public class EventAggregator : IEventAggregator
    {
        private interface ISubscription
        {
            bool IsAlive { get; }
            void Invoke(object eventArgs);
            bool Matches(Delegate d);
        }

        private class Subscription<T> : ISubscription
        {
            private readonly Action<T> _delegate;
            private readonly WeakReference _targetRef;

            public Subscription(Action<T> action)
            {
                _delegate = action ?? throw new ArgumentNullException(nameof(action));
                // Store a weak reference to the target object so GC can collect it
                _targetRef = new WeakReference(action.Target);
            }

            public bool IsAlive => _targetRef.IsAlive;

            public bool Matches(Delegate d)
            {
                return _delegate.Equals(d);
            }

            public void Invoke(object eventArgs)
            {
                if (!IsAlive)
                {
                    // Target object was garbage collected, skip invocation
                    return;
                }
                _delegate((T)eventArgs);
            }
        }

        private readonly Dictionary<Type, List<ISubscription>> _subscribers =
            new Dictionary<Type, List<ISubscription>>();

        private const int MaxPoolSize = 64;

        private readonly Stack<List<ISubscription>> _listPool = new Stack<List<ISubscription>>();

        private List<ISubscription> GetTempList()
        {
            return _listPool.Count > 0 ? _listPool.Pop() : new List<ISubscription>(8);
        }

        private void ReturnTempList(List<ISubscription> list)
        {
            list.Clear();
            if (_listPool.Count < MaxPoolSize)
                _listPool.Push(list);
        }

        public void Subscribe<T>(Action<T> handler)
        {
            if (handler == null) return;

            if (!_subscribers.TryGetValue(typeof(T), out var list))
            {
                list = GetTempList();
                _subscribers[typeof(T)] = list;
            }
            list.Add(new Subscription<T>(handler));
        }

        /// <summary>
        /// Subscribes and returns an IDisposable token. Dispose() auto-unsubscribes.
        /// Preferred over manual Subscribe/Unsubscribe for RAII-style cleanup (Fix #9).
        /// </summary>
        public IDisposable SubscribeToken<T>(Action<T> handler)
        {
            Subscribe(handler);
            return new SubscriptionToken<T>(this, handler);
        }

        /// <summary>
        /// RAII token that auto-unsubscribes when disposed.
        /// </summary>
        private sealed class SubscriptionToken<T> : IDisposable
        {
            private readonly EventAggregator _aggregator;
            private Action<T> _handler;
            private bool _disposed;

            public SubscriptionToken(EventAggregator aggregator, Action<T> handler)
            {
                _aggregator = aggregator;
                _handler = handler;
            }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                if (_handler != null)
                {
                    _aggregator.Unsubscribe(_handler);
                    _handler = null;
                }
            }
        }

        public void Unsubscribe<T>(Action<T> handler)
        {
            if (handler == null) return;

            if (!_subscribers.TryGetValue(typeof(T), out var list))
                return;

            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (list[i].Matches(handler))
                {
                    list.RemoveAt(i);
                }
            }

            if (list.Count == 0)
            {
                _subscribers.Remove(typeof(T));
                ReturnTempList(list);
            }
        }

        public void Publish<T>(T eventArgs)
        {
            List<ISubscription> snapshot = GetTempList();

            if (_subscribers.TryGetValue(typeof(T), out var list) && list.Count > 0)
            {
                // Pre-size the snapshot to avoid List growth allocations during AddRange.
                snapshot.Capacity = list.Count;
                snapshot.AddRange(list);
            }
            else
            {
                ReturnTempList(snapshot);
                return;
            }

            Exception firstException = null;
            try
            {
                foreach (var sub in snapshot)
                {
                    try
                    {
                        sub.Invoke(eventArgs);
                    }
                    catch (Exception ex)
                    {
                        MoldLogger.LogError($"EventAggregator: Subscriber threw on {typeof(T).Name}: {ex}");
                        if (firstException == null)
                        {
                            firstException = ex;
                        }
                    }
                }
                if (firstException != null)
                {
                    throw firstException;
                }
            }
            finally
            {
                ReturnTempList(snapshot);
            }
        }

        public void Clear()
        {
            foreach (var list in _subscribers.Values)
            {
                list.Clear();
                if (_listPool.Count < MaxPoolSize)
                    _listPool.Push(list);
            }
            _subscribers.Clear();
        }

        // Fix #26: Event-type bazlı cleanup — sadece belirli bir event tipini temizler.
        // Clear() tümünü silmek için kullanılır, bu metot ise belirli bir event tipine
        // abone olanları seçici olarak temizler.
        public void Clear<T>()
        {
            var type = typeof(T);
            if (_subscribers.TryGetValue(type, out var list))
            {
                list.Clear();
                _subscribers.Remove(type);
                if (_listPool.Count < MaxPoolSize)
                    _listPool.Push(list);
            }
        }
    }
}