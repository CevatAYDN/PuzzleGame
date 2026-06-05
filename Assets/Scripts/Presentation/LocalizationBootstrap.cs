using System.Collections;
using PuzzleGame.Application.Interfaces;
using UnityEngine;
using VContainer;

namespace PuzzleGame.Presentation
{
    /// <summary>
    /// Pre-loads the registered IAsyncTranslationProvider at app startup so the
    /// first ILocalizationService.GetString() call doesn't block on UnityWebRequest.
    /// No-op when no async provider is registered (Editor + Standalone where
    /// JsonTranslationProvider's File.ReadAllText is fast enough to skip preloading).
    ///
    /// The try/catch is intentional: Resolve throws when the interface isn't
    /// registered, and we treat that as "sync path is in use" rather than a fault.
    /// </summary>
    public class LocalizationBootstrap : MonoBehaviour
    {
#pragma warning disable CS0649
        [Inject] private IObjectResolver _resolver;
#pragma warning restore CS0649

        private IEnumerator Start()
        {
            if (_resolver == null) yield break;

            IAsyncTranslationProvider async;
            try { async = _resolver.Resolve<IAsyncTranslationProvider>(); }
            catch { yield break; }

            var task = async.LoadAsync();
            while (!task.IsCompleted) yield return null;

            if (task.IsFaulted)
                Debug.LogError($"[LocalizationBootstrap] Preload failed: {task.Exception}");
        }
    }
}
