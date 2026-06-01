using System;
using UnityEngine;

namespace PuzzleGame.Infrastructure.Providers
{
    /// <summary>
    /// Contract for async asset loading. Decouples Addressables from consumers.
    /// Always available — implementation is conditionally compiled.
    /// </summary>
    public interface IAssetProvider
    {
        void Initialize();
        void LoadAssetAsync<T>(string address, Action<T> onComplete) where T : UnityEngine.Object;
        void Release<T>(T asset) where T : UnityEngine.Object;
        void InstantiateAsync(string address, Transform parent, Action<GameObject> onComplete);
        void ReleaseInstance(GameObject instance);
    }
}
