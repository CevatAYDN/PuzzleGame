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
    }
}