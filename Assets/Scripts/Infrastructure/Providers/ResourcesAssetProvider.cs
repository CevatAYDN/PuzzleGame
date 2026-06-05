using System;
using UnityEngine;
using PuzzleGame.Application.Logging;

namespace PuzzleGame.Infrastructure.Providers
{
    /// <summary>
    /// Fallback synchronous asset provider utilizing Unity's Resources system.
    /// Used when ENABLE_ADDRESSABLES is not defined.
    /// </summary>
    public class ResourcesAssetProvider : IAssetProvider
    {
        public void Initialize()
        {
            MoldLogger.LogInfo("[ResourcesAssetProvider] Initialized (Resources fallback).");
        }

        public void LoadAssetAsync<T>(string address, Action<T> onComplete) where T : UnityEngine.Object
        {
            var asset = Resources.Load<T>(address);
            if (asset == null)
            {
                MoldLogger.LogError($"[ResourcesAssetProvider] Failed to load '{address}' from Resources.");
            }
            onComplete?.Invoke(asset);
        }

        public void Release<T>(T asset) where T : UnityEngine.Object
        {
            // Resources does not require explicit manual unloading like Addressables
        }

        public void InstantiateAsync(string address, Transform parent, Action<GameObject> onComplete)
        {
            var prefab = Resources.Load<GameObject>(address);
            if (prefab == null)
            {
                MoldLogger.LogError($"[ResourcesAssetProvider] Failed to load prefab '{address}' from Resources.");
                onComplete?.Invoke(null);
                return;
            }
            var go = UnityEngine.Object.Instantiate(prefab, parent);
            onComplete?.Invoke(go);
        }

        public void ReleaseInstance(GameObject instance)
        {
            if (instance != null)
            {
                UnityEngine.Object.Destroy(instance);
            }
        }
    }
}
