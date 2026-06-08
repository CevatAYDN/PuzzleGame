using System;
using System.Collections.Generic;
using PuzzleGame.Domain.Services;
using PuzzleGame.Domain.Interfaces;

namespace PuzzleGame.Editor
{
    /// <summary>
    /// Simple Service Locator for editor-only domain service resolution.
    /// Decouples editor GUI classes from concrete service instantiations.
    /// </summary>
    public static class EditorServiceLocator
    {
        private static readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

        static EditorServiceLocator()
        {
            // Register default instances for editor validation and solving.
            Register<IMoldValidator>(new MoldValidationService(Domain.ForgeConstants.ColorMatchEpsilon));
        }

        public static void Register<T>(T service)
        {
            if (service == null) throw new ArgumentNullException(nameof(service));
            _services[typeof(T)] = service;
        }

        public static T Get<T>()
        {
            if (_services.TryGetValue(typeof(T), out var service))
            {
                return (T)service;
            }
            throw new InvalidOperationException($"Service of type {typeof(T).Name} is not registered.");
        }

        /// <summary>
        /// Removes all registered services and re-applies the defaults. Use this
        /// from editor test <c>[TearDown]</c> to keep tests isolated — otherwise a
        /// fake registered in one test leaks into the next.
        /// </summary>
        public static void Clear()
        {
            _services.Clear();
            // Re-register defaults so the locator remains usable after a clear.
            Register<IMoldValidator>(new MoldValidationService(Domain.ForgeConstants.ColorMatchEpsilon));
        }
    }
}
