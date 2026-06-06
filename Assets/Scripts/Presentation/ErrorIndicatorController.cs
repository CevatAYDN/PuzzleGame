using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Configuration;
using VContainer;

namespace PuzzleGame.Presentation
{
    /// <summary>
    /// Visual error indicator — red ring flash on mold + temporal X icon.
    /// Uses MaterialPropertyBlock to set rim color directly, avoiding per-mold overhead.
    /// Attached to a shared indicator pool GameObject by GameManager.
    /// Migrated from coroutine to UniTask (Sprint #18) for cancellation-on-destroy safety.
    /// </summary>
    public class ErrorIndicatorController : MonoBehaviour, IErrorIndicatorService
    {
        [SerializeField] private GameObject _errorPrefab;
        [SerializeField] private float _indicatorDuration = 0.6f;

        private AnimationConfig _animConfig;
        private IMoldView[] _moldViews;
        private readonly Dictionary<int, GameObject> _activeIndicators = new Dictionary<int, GameObject>();
        private readonly Queue<GameObject> _indicatorPool = new Queue<GameObject>();
        private CancellationToken _lifetimeToken;

        private static readonly int RimColorID = Shader.PropertyToID("_RimColor");
        private static readonly int RimIntensityID = Shader.PropertyToID("_RimIntensity");
        private static readonly Color ErrorRed = new Color(1f, 0.15f, 0.1f, 1f);

        [Inject]
        public void Construct(AnimationConfig animConfig)
        {
            _animConfig = animConfig;
        }

        private void Awake()
        {
            _lifetimeToken = this.GetCancellationTokenOnDestroy();
        }

        public void Initialize(IMoldView[] moldViews)
        {
            _moldViews = moldViews;
        }

        public void ShowErrorOnMold(int moldIndex, string reason)
        {
            if (_moldViews == null || moldIndex < 0 || moldIndex >= _moldViews.Length)
            {
                Debug.LogWarning($"[ErrorIndicator] Invalid mold index: {moldIndex}, reason: {reason}");
                return;
            }

            var mold = _moldViews[moldIndex];
            if (mold == null) return;

            // Flash red rim on mold renderer
            var renderer = mold.GameObject?.GetComponent<Renderer>();
            if (renderer != null)
            {
                var mpb = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(mpb, 0);
                mpb.SetColor(RimColorID, ErrorRed);
                mpb.SetFloat(RimIntensityID, 3f);
                renderer.SetPropertyBlock(mpb, 0);

                // Schedule clear after duration (fire-and-forget, token cancels on destroy)
                ClearRimAfterDelayAsync(renderer).Forget();
            }

            // Spawn X indicator at mold position
            if (_errorPrefab != null && mold.GameObject != null)
            {
                GameObject indicator = GetOrCreateIndicator();
                indicator.transform.position = mold.GameObject.transform.position + Vector3.up * 0.5f;
                indicator.SetActive(true);
                _activeIndicators[moldIndex] = indicator;

                ReturnIndicatorAfterDelayAsync(moldIndex).Forget();
            }
        }

        public void ClearAllIndicators()
        {
            foreach (var kvp in _activeIndicators)
            {
                ReturnToPool(kvp.Value);
            }
            _activeIndicators.Clear();
        }

        private GameObject GetOrCreateIndicator()
        {
            if (_indicatorPool.Count > 0) return _indicatorPool.Dequeue();
            return Instantiate(_errorPrefab, transform);
        }

        private void ReturnToPool(GameObject indicator)
        {
            if (indicator == null) return;
            indicator.SetActive(false);
            indicator.transform.SetParent(transform);
            _indicatorPool.Enqueue(indicator);
        }

        private async UniTaskVoid ClearRimAfterDelayAsync(Renderer renderer)
        {
            await UniTask.Delay(System.TimeSpan.FromSeconds(_indicatorDuration), cancellationToken: _lifetimeToken);
            if (renderer != null)
            {
                var mpb = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(mpb, 0);
                mpb.SetFloat(RimIntensityID, 0f);
                renderer.SetPropertyBlock(mpb, 0);
            }
        }

        private async UniTaskVoid ReturnIndicatorAfterDelayAsync(int moldIndex)
        {
            await UniTask.Delay(System.TimeSpan.FromSeconds(_indicatorDuration), cancellationToken: _lifetimeToken);
            if (_activeIndicators.TryGetValue(moldIndex, out var indicator))
            {
                ReturnToPool(indicator);
                _activeIndicators.Remove(moldIndex);
            }
        }
    }
}
