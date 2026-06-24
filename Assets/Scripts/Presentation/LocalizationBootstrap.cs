using System.Collections;
using PuzzleGame.Application.Interfaces;
using UnityEngine;
using Nexus.Core;

namespace PuzzleGame.Presentation
{
    /// <summary>
    /// Pre-loads the registered IAsyncTranslationProvider at app startup so the
    /// first ILocalizationService.GetString() call doesn't block on UnityWebRequest.
    /// No-op when no async provider is registered (Editor + Standalone where
    /// JsonTranslationProvider's File.ReadAllText is fast enough to skip preloading).
    /// </summary>
    public class LocalizationBootstrap : MonoBehaviour
    {
#pragma warning disable CS0649
        [Inject] private NexusDI _resolver;
#pragma warning restore CS0649

        private IEnumerator Start()
        {
            if (_resolver == null) yield break;

            if (!_resolver.IsRegistered(typeof(IAsyncTranslationProvider)))
            {
                yield break;
            }

            var async = _resolver.Resolve<IAsyncTranslationProvider>();
            var task = async.LoadAsync();
            while (!task.IsCompleted) yield return null;

            if (task.IsFaulted)
                Debug.LogError($"[LocalizationBootstrap] Preload failed: {task.Exception}");
        }
    }
}
