using System;

namespace PuzzleGame.Application.Interfaces
{
    /// <summary>
    /// Lightweight, type-safe publish/subscribe bus contract.
    /// Decouples systems that should not reference each other directly.
    /// Instance-based — injected via DI for testability.
    /// </summary>
    public interface IEventAggregator
    {
        void Subscribe<T>(Action<T> handler);
        void Unsubscribe<T>(Action<T> handler);
        void Publish<T>(T eventArgs);
        void Clear();
    }
}
