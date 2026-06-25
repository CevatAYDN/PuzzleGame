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
        void Subscribe<T>(Action<T> handler) where T : struct;

        /// <summary>
        /// Subscribes and returns an IDisposable token. Dispose() auto-unsubscribes.
        /// Preferred over manual Subscribe/Unsubscribe for RAII-style cleanup.
        /// </summary>
        IDisposable SubscribeToken<T>(Action<T> handler) where T : struct;
        void Unsubscribe<T>(Action<T> handler) where T : struct;
        void Publish<T>(T eventArgs) where T : struct;
        void Clear();

        /// <summary>Clears only subscriptions of the specified type.</summary>
        void Clear<T>() where T : struct;
    }
}
