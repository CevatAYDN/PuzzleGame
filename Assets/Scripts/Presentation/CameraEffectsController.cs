using UnityEngine;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Configuration;
using PuzzleGame.Application.Events;

namespace PuzzleGame.Presentation
{
    /// <summary>
    /// Camera shake and visual feedback controller.
    /// Listens to game events and applies PrimeTween-based camera effects.
    /// Attached to the Main Camera GameObject by GameManager.
    /// No Cinemachine dependency — pure Transform tweens.
    /// </summary>
    public class CameraEffectsController : MonoBehaviour
    {
        private IEventAggregator _eventAggregator;
        private AnimationConfig _config;
        private ITweenService _tweenService;

        private Vector3 _restPosition;
        private bool _hasSubscribed;

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        private void Log(string msg) => Debug.Log($"[CameraEffects] {msg}");

        public void Initialize(IEventAggregator eventAggregator, AnimationConfig config, ITweenService tweenService)
        {
            _eventAggregator = eventAggregator ?? throw new System.ArgumentNullException(nameof(eventAggregator));
            _config = config;
            _tweenService = tweenService ?? throw new System.ArgumentNullException(nameof(tweenService));
            _restPosition = transform.localPosition;

            if (!_hasSubscribed)
            {
                _eventAggregator.Subscribe<CastCompletedEvent>(OnCastCompleted);
                _eventAggregator.Subscribe<CastRejectedEvent>(OnCastRejected);
                _eventAggregator.Subscribe<LevelCompletedEvent>(OnLevelCompleted);
                _hasSubscribed = true;
            }
        }

        private void OnDestroy()
        {
            if (_eventAggregator != null)
            {
                _eventAggregator.Unsubscribe<CastCompletedEvent>(OnCastCompleted);
                _eventAggregator.Unsubscribe<CastRejectedEvent>(OnCastRejected);
                _eventAggregator.Unsubscribe<LevelCompletedEvent>(OnLevelCompleted);
            }
        }

        private void OnCastCompleted(CastCompletedEvent evt)
        {
            if (_config == null) return;
            ShakeCamera(
                _config.successShakeAmplitude,
                _config.successShakeDuration,
                _config.cameraShakeFrequency);
        }

        private void OnCastRejected(CastRejectedEvent evt)
        {
            if (_config == null) return;
            ShakeCamera(
                _config.errorShakeAmplitude,
                _config.errorShakeDuration,
                _config.cameraShakeFrequency * 1.5f);
        }

        private void OnLevelCompleted(LevelCompletedEvent evt)
        {
            // Subtle celebratory pulse — a small upward nudge and settle
            if (_tweenService == null) return;

            Vector3 target = _restPosition + Vector3.up * 0.1f;
            var up = _tweenService.TweenPosition(transform, target, 0.3f, EaseType.OutBack);
            up.OnComplete(() =>
            {
                _tweenService.TweenPosition(transform, _restPosition, 0.6f, EaseType.InOutSine);
            });
        }

        /// <summary>
        /// Shakes the camera with a decaying sine wave along X and Y axes.
        /// </summary>
        private void ShakeCamera(float amplitude, float duration, float frequency)
        {
            if (amplitude <= 0f || duration <= 0f) return;
            if (_tweenService == null) return;

            // Custom shake via ITweenService.ShakeRotation on a dummy or...
            // Since ITweenService exposes ShakeRotation but not ShakePosition,
            // we roll a manual position shake using TweenCustom or here a simple
            // ITweenService.Custom call.
            //
            // Simpler approach: use ITweenService.Delay to schedule reset
            // and manual coroutine-like update via PrimeTween.
            // However to stay within the existing ITweenService boundary:
            // We use rotation shake (already available) + subtle position bounce.

            // Position bounce: nudge in a random direction, then settle back
            float angle = Random.Range(0f, Mathf.PI * 2f);
            Vector3 offset = new Vector3(
                Mathf.Cos(angle) * amplitude,
                Mathf.Sin(angle) * amplitude * 0.5f,
                0f);

            var bounce = _tweenService.TweenPosition(transform, _restPosition + offset, duration * 0.3f, EaseType.OutBack);
            bounce.OnComplete(() =>
            {
                _tweenService.TweenPosition(transform, _restPosition, duration * 0.7f, EaseType.InOutSine);
            });

            // Rotation shake: available via ITweenService
            _tweenService.TweenShakeRotation(transform, duration, new Vector3(amplitude * 3f, amplitude * 3f, 0f), Mathf.RoundToInt(frequency));
        }
    }
}
