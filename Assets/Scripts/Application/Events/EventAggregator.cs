using System;
using System.Collections.Generic;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Logging;

namespace PuzzleGame.Application.Events
{
    /// <summary>
    /// Lightweight, type-safe publish/subscribe bus.
    /// Instance-based — inject via IEventAggregator for testability.
    ///
    /// Usage:
    ///   _eventAggregator.Subscribe&lt;PourCompletedEvent&gt;(OnPourCompleted);
    ///   _eventAggregator.Publish(new PourCompletedEvent(source, target));
    ///   _eventAggregator.Unsubscribe&lt;PourCompletedEvent&gt;(OnPourCompleted);
    /// </summary>
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

            public Subscription(Action<T> action)
            {
                _delegate = action ?? throw new ArgumentNullException(nameof(action));
            }

            public bool IsAlive => true;

            public bool Matches(Delegate d)
            {
                return _delegate.Equals(d);
            }

            public void Invoke(object eventArgs)
            {
                _delegate((T)eventArgs);
            }
        }

        private readonly Dictionary<Type, List<ISubscription>> _subscribers =
            new Dictionary<Type, List<ISubscription>>();

        private const int MaxPoolSize = 16;

        private readonly Stack<List<ISubscription>> _listPool = new Stack<List<ISubscription>>();
        private readonly object _lockObj = new object();

        private List<ISubscription> GetTempList()
        {
            lock (_lockObj)
            {
                return _listPool.Count > 0 ? _listPool.Pop() : new List<ISubscription>(8);
            }
        }

        private void ReturnTempList(List<ISubscription> list)
        {
            list.Clear();
            lock (_lockObj)
            {
                if (_listPool.Count < MaxPoolSize)
                    _listPool.Push(list);
            }
        }

        public void Subscribe<T>(Action<T> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            lock (_lockObj)
            {
                if (!_subscribers.TryGetValue(typeof(T), out var list))
                {
                    list = GetTempList();
                    _subscribers[typeof(T)] = list;
                }
                list.Add(new Subscription<T>(handler));
            }
        }

        public void Unsubscribe<T>(Action<T> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            lock (_lockObj)
            {
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
        }

        public void Publish<T>(T eventArgs)
        {
            // Fix Code Quality #7: Unity is single-threaded on the main thread.
            // Instead of list.ToArray() (GC alloc per Publish), we copy into a pooled List,
            // invoke outside the lock (to avoid re-entrant deadlock), then return the list.
            var snapshot = GetTempList();

            lock (_lockObj)
            {
                if (!_subscribers.TryGetValue(typeof(T), out var list) || list.Count == 0)
                {
                    ReturnTempList(snapshot);
                    return;
                }
                snapshot.AddRange(list);
            }

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
                        BottleLogger.LogError($"EventAggregator: Subscriber threw on {typeof(T).Name}: {ex}");
                    }
                }
            }
            finally
            {
                ReturnTempList(snapshot);
            }
        }

        public void Clear()
        {
            lock (_lockObj)
            {
                foreach (var list in _subscribers.Values)
                {
                    list.Clear();
                    if (_listPool.Count < MaxPoolSize)
                        _listPool.Push(list);
                }
                _subscribers.Clear();
            }
        }
    }
}
