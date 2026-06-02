using System;
using System.Collections.Generic;
using PuzzleGame.Logging;

namespace PuzzleGame.Events
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
        public interface ISubscription
        {
            bool IsAlive { get; }
            void Invoke(object eventArgs);
            bool Matches(Delegate d);
        }

        public class Subscription<T> : ISubscription
        {
            private readonly WeakReference _target;
            private readonly Action<T> _delegate;
            private readonly System.Reflection.MethodInfo _method;
            private readonly bool _isStatic;

            public Subscription(Action<T> action)
            {
                _method = action.Method;
                if (action.Target != null)
                {
                    _target = new WeakReference(action.Target);
                    _isStatic = false;
                }
                else
                {
                    _delegate = action;
                    _isStatic = true;
                }
            }

            public bool IsAlive => _isStatic || (_target != null && _target.IsAlive);

            public bool Matches(Delegate d)
            {
                if (d == null) return false;
                if (_isStatic)
                {
                    return d.Target == null && d.Method == _method;
                }
                return _target != null && _target.Target == d.Target && d.Method == _method;
            }

            public void Invoke(object eventArgs)
            {
                if (_isStatic)
                {
                    _delegate((T)eventArgs);
                }
                else
                {
                    object target = _target?.Target;
                    if (target != null)
                    {
                        var del = (Action<T>)Delegate.CreateDelegate(typeof(Action<T>), target, _method);
                        del((T)eventArgs);
                    }
                }
            }
        }

        private static readonly Dictionary<Type, List<ISubscription>> _subscribers =
            new Dictionary<Type, List<ISubscription>>();

        private const int MaxPoolSize = 16;

        private static readonly Stack<List<ISubscription>> _listPool = new Stack<List<ISubscription>>();
        private static readonly object _lockObj = new object();

        private static List<ISubscription> GetTempList()
        {
            if (_listPool.Count > 0)
                return _listPool.Pop();
            return new List<ISubscription>(16);
        }

        private static void ReleaseTempList(List<ISubscription> list)
        {
            list.Clear();
            if (_listPool.Count < MaxPoolSize)
                _listPool.Push(list);
        }

        // ── Subscribe / Unsubscribe ──────────────────────────────────────────

        public static void Subscribe<T>(Action<T> handler)
        {
            if (handler == null) return;

            var type = typeof(T);
            lock (_lockObj)
            {
                if (!_subscribers.TryGetValue(type, out var list))
                {
                    list = new List<ISubscription>();
                    _subscribers[type] = list;
                }
                list.Add(new Subscription<T>(handler));
            }
        }

        public static void Unsubscribe<T>(Action<T> handler)
        {
            if (handler == null) return;

            lock (_lockObj)
            {
                if (_subscribers.TryGetValue(typeof(T), out var list))
                {
                    for (int i = list.Count - 1; i >= 0; i--)
                    {
                        if (list[i].Matches(handler))
                        {
                            list.RemoveAt(i);
                        }
                    }
                }
            }
        }

        // ── Publish ──────────────────────────────────────────────────────────

        public static void Publish<T>(T eventArgs)
        {
            List<ISubscription> tempList = null;

            lock (_lockObj)
            {
                if (!_subscribers.TryGetValue(typeof(T), out var list) || list.Count == 0)
                    return;

                // Clean dead subscriptions first
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    if (!list[i].IsAlive)
                    {
                        list.RemoveAt(i);
                    }
                }

                if (list.Count == 0) return;

                // Copy to a pooled list to support safe unsubscription/subscription during dispatch
                tempList = GetTempList();
                int originalCount = list.Count;
                for (int i = 0; i < originalCount; i++)
                {
                    tempList.Add(list[i]);
                }
            }

            List<Exception> exceptions = null;
            int count = tempList.Count;
            for (int i = 0; i < count; i++)
            {
                var sub = tempList[i];
                if (!sub.IsAlive) continue;

                try
                {
                    sub.Invoke(eventArgs);
                }
                catch (Exception ex)
                {
                    BottleLogger.LogError($"EventAggregator: handler threw for event {typeof(T).Name}: {ex}");
                    if (exceptions == null)
                    {
                        exceptions = new List<Exception>();
                    }
                    exceptions.Add(ex);
                }
            }

            lock (_lockObj)
            {
                ReleaseTempList(tempList);
            }

            if (exceptions != null)
            {
                if (exceptions.Count == 1)
                {
                    System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(exceptions[0]).Throw();
                }
                else
                {
                    throw new AggregateException($"Multiple exceptions thrown during dispatch of event {typeof(T).Name}", exceptions);
                }
            }
        }

        /// <summary>Removes all subscribers — call on scene unload to prevent stale references.</summary>
        public static void Clear()
        {
            lock (_lockObj)
            {
                _subscribers.Clear();
            }
        }
    }
}
