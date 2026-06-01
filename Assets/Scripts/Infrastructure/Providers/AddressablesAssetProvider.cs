#if ENABLE_ADDRESSABLES
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace PuzzleGame.Infrastructure.Providers
{
    /// <summary>
    /// Generic async asset provider backed by Addressables.
    /// - Load: async load by address/key
    /// - Release: proper memory release via Addressables
    /// - Instantiate: async instantiate with auto-tracking
    ///
    /// Clean architecture: Infrastructure layer — Unity-specific Addressables wrapper.
    /// </summary>
    public class AddressablesAssetProvider : IAssetProvider
    {
        private bool _initialized;

        public void Initialize()
        {
            if (_initialized) return;
            Addressables.InitializeAsync().Completed += handle =>
            {
                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    _initialized = true;
                    BottleLogger.LogInfo("[AddressablesAssetProvider] Initialized.");
                }
                else
                {
                    BottleLogger.LogError("[AddressablesAssetProvider] Initialization failed.");
                }
            };
        }

        public void LoadAssetAsync<T>(string address, Action<T> onComplete) where T : UnityEngine.Object
        {
            if (!_initialized)
            {
                BottleLogger.LogWarning($"[AddressablesAssetProvider] Not initialized. Loading '{address}' synchronously as fallback.");
                var asset = Resources.Load<T>(address);
                onComplete?.Invoke(asset);
                return;
            }

            var handle = Addressables.LoadAssetAsync<T>(address);
            handle.Completed += h =>
            {
                if (h.Status == AsyncOperationStatus.Succeeded)
                    onComplete?.Invoke(h.Result);
                else
                {
                    BottleLogger.LogError($"[AddressablesAssetProvider] Failed to load '{address}'");
                    onComplete?.Invoke(null);
                }
            };
        }

        public void Release<T>(T asset) where T : UnityEngine.Object
        {
            if (asset != null)
                Addressables.Release(asset);
        }

        public void InstantiateAsync(string address, Transform parent, Action<GameObject> onComplete)
        {
            if (!_initialized)
            {
                BottleLogger.LogWarning($"[AddressablesAssetProvider] Not initialized. Instantiating '{address}' synchronously as fallback.");
                var prefab = Resources.Load<GameObject>(address);
                var go = UnityEngine.Object.Instantiate(prefab, parent);
                onComplete?.Invoke(go);
                return;
            }

            var handle = Addressables.InstantiateAsync(address, parent);
            handle.Completed += h =>
            {
                if (h.Status == AsyncOperationStatus.Succeeded)
                    onComplete?.Invoke(h.Result);
                else
                {
                    BottleLogger.LogError($"[AddressablesAssetProvider] Failed to instantiate '{address}'");
                    onComplete?.Invoke(null);
                }
            };
        }

        public void ReleaseInstance(GameObject instance)
        {
            if (instance != null)
                Addressables.ReleaseInstance(instance);
        }
    }
}
#endif
