using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace PuzzleGame.Application.Services
{
    /// <summary>
    /// Simple screen-fade transition service. Attach a full-screen black CanvasGroup
    /// to your scene; call FadeOut then LoadScene then FadeIn.
    /// Migrated from coroutine to UniTask (Sprint #18) for cancellation support
    /// and to align with the async/await pattern used by LocalizationBootstrap.
    /// </summary>
    public sealed class ScreenTransitionService
    {
        private readonly MonoBehaviour _coroutineHost;
        private readonly CanvasGroup _overlay;
        private readonly float _defaultDuration;
        private readonly CancellationToken _lifetimeToken;

        public event Action OnFadeOutComplete;
        public event Action OnFadeInComplete;

        public ScreenTransitionService(MonoBehaviour coroutineHost, CanvasGroup overlay, float defaultDuration = 0.35f)
        {
            _coroutineHost = coroutineHost;
            _overlay = overlay;
            _defaultDuration = defaultDuration;
            _lifetimeToken = coroutineHost != null
                ? coroutineHost.GetCancellationTokenOnDestroy()
                : CancellationToken.None;
        }

        public UniTask FadeOut(float? duration = null, Action onComplete = null)
        {
            if (_coroutineHost == null || _overlay == null) return UniTask.CompletedTask;
            return FadeRoutineAsync(0f, 1f, duration ?? _defaultDuration, () =>
            {
                onComplete?.Invoke();
                OnFadeOutComplete?.Invoke();
            });
        }

        public UniTask FadeIn(float? duration = null, Action onComplete = null)
        {
            if (_coroutineHost == null || _overlay == null) return UniTask.CompletedTask;
            return FadeRoutineAsync(1f, 0f, duration ?? _defaultDuration, () =>
            {
                onComplete?.Invoke();
                OnFadeInComplete?.Invoke();
            });
        }

        private async UniTask FadeRoutineAsync(float from, float to, float duration, Action onComplete)
        {
            if (_overlay == null) return;
            _overlay.gameObject.SetActive(true);
            _overlay.blocksRaycasts = true;
            float t = 0f;
            _overlay.alpha = from;
            while (t < duration)
            {
                await UniTask.Yield(PlayerLoopTiming.Update, _lifetimeToken);
                t += Time.unscaledDeltaTime;
                _overlay.alpha = Mathf.Lerp(from, to, t / duration);
            }
            _overlay.alpha = to;
            if (to <= 0f)
            {
                _overlay.blocksRaycasts = false;
                _overlay.gameObject.SetActive(false);
            }
            onComplete?.Invoke();
        }
    }
}
