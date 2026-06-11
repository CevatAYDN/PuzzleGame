using PuzzleGame.Application.Logging;
using UnityEngine;

namespace PuzzleGame.Installers
{
    /// <summary>
    /// Helper for loading ScriptableObject configuration assets from Resources/
    /// with a consistent "Inspector-override → Resources → throw/default" policy.
    /// Fix #16: extracted from GameInstaller to remove 8 near-identical load
    /// blocks and centralise the fail-loudly contract.
    /// </summary>
    internal static class ConfigLoader
    {
        /// <summary>
        /// Returns the supplied instance if non-null, otherwise tries
        /// <c>Resources.Load&lt;T&gt;(resourcesPath)</c>. If both are null, throws
        /// <see cref="System.InvalidOperationException"/> with a uniform message
        /// naming the config and its expected path.
        /// </summary>
        public static T LoadOrThrow<T>(T existing, string resourcesPath, string fieldName)
            where T : ScriptableObject
        {
            if (existing != null) return existing;

            T loaded = Resources.Load<T>(resourcesPath);
            if (loaded == null)
            {
                throw new System.InvalidOperationException(
                    $"{fieldName} asset missing at Resources/{resourcesPath}. " +
                    "Cannot start without it.");
            }
            return loaded;
        }

    }
}
