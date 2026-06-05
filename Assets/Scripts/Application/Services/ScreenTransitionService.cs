using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace PuzzleGame.Application.Services
{
    /// <summary>
    /// Simple screen-fade transition service. Attach a full-screen black CanvasGroup
    /// to your scene; call FadeOut then LoadScene then FadeIn.
    /// </summary>
    public sealed class ScreenTransitionService
    {
        private readonly MonoBehaviour _coroutineHost;
        private readonly CanvasGroup _overlay;
        private readonly float _defaultDuration;

        public event Action OnFadeOutComplete;
        public event Action OnFadeInComplete;

        public ScreenTransitionService(MonoBehaviour coroutineHost, CanvasGroup overlay, float defaultDuration = 0.35f)
        {
            _coroutineHost = coroutineHost;
            _overlay = overlay;
            _defaultDuration = defaultDuration;
        }

        public Coroutine FadeOut(float? duration = null, Action onComplete = null)
        {
            if (_coroutineHost == null || _overlay == null) return null;
            return _coroutineHost.StartCoroutine(FadeRoutine(0f, 1f, duration ?? _defaultDuration, () =>
            {
                onComplete?.Invoke();
                OnFadeOutComplete?.Invoke();
            }));
        }

        public Coroutine FadeIn(float? duration = null, Action onComplete = null)
        {
            if (_coroutineHost == null || _overlay == null) return null;
            return _coroutineHost.StartCoroutine(FadeRoutine(1f, 0f, duration ?? _defaultDuration, () =>
            {
                onComplete?.Invoke();
                OnFadeInComplete?.Invoke();
            }));
        }

        private IEnumerator FadeRoutine(float from, float to, float duration, Action onComplete)
        {
            if (_overlay == null) yield break;
            _overlay.gameObject.SetActive(true);
            _overlay.blocksRaycasts = true;
            float t = 0f;
            _overlay.alpha = from;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                _overlay.alpha = Mathf.Lerp(from, to, t / duration);
                yield return null;
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
