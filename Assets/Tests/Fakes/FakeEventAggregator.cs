using System;
using System.Collections.Generic;
using PuzzleGame.Application.Interfaces;

namespace PuzzleGame.Tests.Fakes
{
    /// <summary>
    /// Controllable fake for IEventAggregator. Records published events by type
    /// and tracks subscribe/unsubscribe counts.
    /// </summary>
    public class FakeEventAggregator : IEventAggregator
    {
        public int SubscribeCallCount { get; private set; }
        public int UnsubscribeCallCount { get; private set; }
        public int PublishCallCount { get; private set; }
        public int ClearCallCount { get; private set; }

        /// <summary>All published events in order, boxed.</summary>
        public List<object> PublishedEvents { get; } = new List<object>();

        /// <summary>If true, Subscribe immediately calls the handler with a default instance.</summary>
        public bool FireOnSubscribe { get; set; }

        public void Subscribe<T>(Action<T> handler)
        {
            SubscribeCallCount++;
            if (FireOnSubscribe)
                handler(default);
        }

        public IDisposable SubscribeToken<T>(Action<T> handler)
        {
            SubscribeCallCount++;
            if (FireOnSubscribe)
                handler(default);
            return new AnonymousDisposable(() => Unsubscribe(handler));
        }

        public void Unsubscribe<T>(Action<T> handler)
        {
            UnsubscribeCallCount++;
        }

        public void Publish<T>(T eventArgs)
        {
            PublishCallCount++;
            PublishedEvents.Add(eventArgs);
        }

        public void Clear()
        {
            ClearCallCount++;
            PublishedEvents.Clear();
        }

        public void Clear<T>()
        {
            ClearCallCount++;
            PublishedEvents.RemoveAll(e => e is T);
        }

        /// <summary>Returns count of published events of type <typeparamref name="T"/>.</summary>
        public int CountOf<T>() => PublishedEvents.FindAll(e => e is T).Count;

        public T LastOf<T>() where T : struct
        {
            for (int i = PublishedEvents.Count - 1; i >= 0; i--)
                if (PublishedEvents[i] is T t) return t;
            return default;
        }

        public T LastOfClass<T>() where T : class
        {
            for (int i = PublishedEvents.Count - 1; i >= 0; i--)
                if (PublishedEvents[i] is T t) return t;
            return null;
        }

        private sealed class AnonymousDisposable : IDisposable
        {
            private readonly Action _dispose;
            public AnonymousDisposable(Action dispose) => _dispose = dispose;
            public void Dispose() => _dispose();
        }
    }
}
