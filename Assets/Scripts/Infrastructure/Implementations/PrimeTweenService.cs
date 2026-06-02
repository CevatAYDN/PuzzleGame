#if PRIME_TWEEN_INSTALLED
using System;
using System.Collections.Generic;
using PrimeTween;
using UnityEngine;
using PuzzleGame.Domain.Interfaces;

namespace PuzzleGame.Infrastructure.Implementations
{
    /// <summary>
    /// PrimeTween-backed ITweenService implementation.
    /// Zero-allocation tweens, GPU-friendly, no coroutine overhead.
    /// Automatically selected when PrimeTween package is installed.
    /// </summary>
    public class PrimeTweenService : ITweenService
    {
        public ITweenHandle TweenPosition(Transform t, Vector3 target, float duration, EaseType ease)
            => Start(PrimeTween.Tween.Position(t, target, duration, ToPrimeEase(ease)));

        public ITweenHandle TweenLocalPosition(Transform t, Vector3 target, float duration, EaseType ease)
            => Start(PrimeTween.Tween.LocalPosition(t, target, duration, ToPrimeEase(ease)));

        public ITweenHandle TweenScale(Transform t, Vector3 target, float duration, EaseType ease)
            => Start(PrimeTween.Tween.Scale(t, target, duration, ToPrimeEase(ease)));

        public ITweenHandle TweenRotation(Transform t, Vector3 target, float duration, EaseType ease)
            => Start(PrimeTween.Tween.LocalEulerAngles(t, t.localEulerAngles, target, duration, ToPrimeEase(ease)));

        public ITweenHandle TweenShakeRotation(Transform t, float duration, Vector3 strength, int vibrato)
        {
            // PrimeTween doesn't have shake — implement via custom callback
            var startRot = t.rotation;
            var handle = new PrimeTweenShakeHandle(
                PrimeTween.Tween.Custom(t, 0f, 1f, duration, (tr, progress) =>
                {
                    var trans = (Transform)tr;
                    if (trans == null) return;
                    float decay = 1f - progress;
                    float angle = Mathf.Sin(progress * Mathf.PI * vibrato) * strength.z * decay;
                    trans.rotation = startRot * Quaternion.Euler(0f, 0f, angle);
                }, ToPrimeEase(EaseType.Linear)),
                t, startRot
            );
            return handle;
        }

        public ITweenHandle TweenCustom(object target, float from, float to, float duration, Action<object, float> onUpdate)
            => Start(PrimeTween.Tween.Custom(target, from, to, duration, (obj, val) => onUpdate(obj, val), ToPrimeEase(EaseType.Linear)));

        public ITweenHandle SequenceCreate() => StartSequence(PrimeTween.Sequence.Create());

        public ITweenHandle Delay(float duration)
            => Start(PrimeTween.Tween.Delay(duration));

        private static Ease ToPrimeEase(EaseType ease) => ease switch
        {
            EaseType.Linear => Ease.Linear,
            EaseType.InOutSine => Ease.InOutSine,
            EaseType.OutBack => Ease.OutBack,
            EaseType.OutBounce => Ease.OutBounce,
            EaseType.InOutQuad => Ease.InOutQuad,
            _ => Ease.Linear,
        };

        private static ITweenHandle Start(Tween t) => new PrimeTweenHandle(t);
        private static ITweenHandle StartSequence(Sequence s) => new PrimeTweenSequenceHandle(s);
    }

    internal class PrimeTweenHandle : ITweenHandle
    {
        internal readonly Tween _tween;
        private readonly List<Action> _onCompleteCallbacks = new List<Action>();

        public PrimeTweenHandle(Tween tween) => _tween = tween;

        public void Chain(ITweenHandle other) { }
        public void Group(ITweenHandle other) { }

        public void OnComplete(Action callback)
        {
            _onCompleteCallbacks.Add(callback);
            _tween.OnComplete(() =>
            {
                for (int i = 0; i < _onCompleteCallbacks.Count; i++)
                    _onCompleteCallbacks[i]?.Invoke();
            });
        }

        public void SetCycles(int loops, LoopMode mode)
        {
            _tween.SetRemainingCycles(loops);
        }

        public void Kill() => _tween.Stop();
        public void Start() { } // Already started by factory
    }

    internal class PrimeTweenShakeHandle : ITweenHandle
    {
        private readonly Tween _tween;
        private readonly Transform _target;
        private readonly Quaternion _startRot;
        private Action _onComplete;

        public PrimeTweenShakeHandle(Tween tween, Transform target, Quaternion startRot)
        {
            _tween = tween;
            _target = target;
            _startRot = startRot;
        }

        public void Chain(ITweenHandle other) { }
        public void Group(ITweenHandle other) { }

        public void OnComplete(Action callback) => _onComplete += callback;

        public void SetCycles(int loops, LoopMode mode) { }

        public void Kill() => _tween.Stop();
        public void Start()
        {
            _tween.OnComplete(() =>
            {
                if (_target != null) _target.rotation = _startRot;
                _onComplete?.Invoke();
            });
        }
    }

    internal class PrimeTweenSequenceHandle : ITweenHandle
    {
        internal Sequence _sequence;
        private bool _started;
        private Action _onComplete;

        public PrimeTweenSequenceHandle(Sequence sequence)
        {
            _sequence = sequence;
        }

        public void Chain(ITweenHandle other)
        {
            if (other is PrimeTweenHandle h) _sequence.Chain(h._tween);
            else if (other is PrimeTweenSequenceHandle s) _sequence.Chain(s._sequence);
        }

        public void Group(ITweenHandle other)
        {
            if (other is PrimeTweenHandle h) _sequence.Group(h._tween);
            else if (other is PrimeTweenSequenceHandle s) _sequence.Group(s._sequence);
        }

        public void OnComplete(Action callback) => _onComplete += callback;

        public void SetCycles(int loops, LoopMode mode)
        {
            _sequence.SetRemainingCycles(loops);
        }

        public void Kill() => _sequence.Stop();

        public void Start()
        {
            if (_started) return;
            _started = true;
            if (_onComplete != null)
                _sequence.OnComplete(_onComplete);
        }
    }
}
#endif
