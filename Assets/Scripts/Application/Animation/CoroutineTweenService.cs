using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PuzzleGame.Domain.Interfaces;
using PuzzleGame.Logging;

namespace PuzzleGame.Application.Animation
{
    /// <summary>
    /// MonoBehaviour host that runs coroutine-based tweens.
    /// Used when no third-party tween library is available.
    /// Drop-in compatible with PrimeTween API surface.
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public class CoroutineTweenHost : MonoBehaviour
    {
        public static CoroutineTweenHost Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    /// <summary>
    /// Coroutine-based tween service. Unity coroutines use Time.deltaTime;
    /// not allocation-free, but works without third-party deps.
    /// </summary>
    public class CoroutineTweenService : ITweenService
    {
        private readonly MonoBehaviour _host;

        public CoroutineTweenService()
        {
            _host = EnsureHost();
        }

        private static MonoBehaviour EnsureHost()
        {
            if (CoroutineTweenHost.Instance != null) return CoroutineTweenHost.Instance;
            var go = new GameObject("[CoroutineTweenHost]");
            return go.AddComponent<CoroutineTweenHost>();
        }

        public ITweenHandle TweenPosition(Transform t, Vector3 target, float duration, EaseType ease)
            => Start(new TransformPositionTween(_host, t, target, duration, ease, false));

        public ITweenHandle TweenLocalPosition(Transform t, Vector3 target, float duration, EaseType ease)
            => Start(new TransformPositionTween(_host, t, target, duration, ease, true));

        public ITweenHandle TweenScale(Transform t, Vector3 target, float duration, EaseType ease)
            => Start(new ScaleTween(_host, t, target, duration, ease));

        public ITweenHandle TweenRotation(Transform t, Vector3 target, float duration, EaseType ease)
            => Start(new RotationTween(_host, t, target, duration, ease));

        public ITweenHandle TweenShakeRotation(Transform t, float duration, Vector3 strength, int vibrato)
            => Start(new ShakeRotationTween(_host, t, duration, strength, vibrato));

        public ITweenHandle TweenCustom(object target, float from, float to, float duration, Action<object, float> onUpdate)
            => Start(new CustomTween(_host, target, from, to, duration, onUpdate));

        public ITweenHandle SequenceCreate() => new SequenceHandle();

        public ITweenHandle Delay(float duration) => new DelayHandle(_host, duration);

        private ITweenHandle Start(BaseTween t)
        {
            t.Start();
            return t;
        }
    }

    internal abstract class BaseTween : ITweenHandle
    {
        protected Action _onComplete;
        public virtual void Chain(ITweenHandle other) { }
        public virtual void Group(ITweenHandle other) { }
        public virtual ITweenHandle OnComplete(Action callback) { _onComplete += callback; return this; }
        public virtual void SetCycles(int loops, LoopMode mode) { }
        public virtual void Kill() { _onComplete = null; }
        public virtual void Start() { }
    }

    internal class TransformPositionTween : BaseTween
    {
        private readonly MonoBehaviour _host;
        private readonly Transform _t;
        private readonly Vector3 _from, _to;
        private readonly float _duration;
        private readonly EaseType _ease;
        private readonly bool _local;
        private Coroutine _coroutine;

        public TransformPositionTween(MonoBehaviour host, Transform t, Vector3 target, float duration, EaseType ease, bool local)
        {
            _host = host; _t = t; _from = local ? t.localPosition : t.position;
            _to = target; _duration = Mathf.Max(0.001f, duration); _ease = ease; _local = local;
        }

        public override void Start() => _coroutine = _host.StartCoroutine(Run());

        private IEnumerator Run()
        {
            float elapsed = 0f;
            while (elapsed < _duration)
            {
                if (_t == null) yield break;
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / _duration);
                float eased = Easing.Apply(t, _ease);
                Vector3 val = Vector3.LerpUnclamped(_from, _to, eased);
                if (_local) _t.localPosition = val; else _t.position = val;
                yield return null;
            }
            if (_t != null)
            {
                if (_local) _t.localPosition = _to;
                else _t.position = _to;
            }
            _onComplete?.Invoke();
        }

        public override void Kill()
        {
            if (_coroutine != null) _host.StopCoroutine(_coroutine);
            base.Kill();
        }
    }

    internal class ScaleTween : BaseTween
    {
        private readonly MonoBehaviour _host;
        private readonly Transform _t;
        private readonly Vector3 _from, _to;
        private readonly float _duration;
        private readonly EaseType _ease;
        private Coroutine _coroutine;

        public ScaleTween(MonoBehaviour host, Transform t, Vector3 target, float duration, EaseType ease)
        {
            _host = host; _t = t; _from = t.localScale; _to = target;
            _duration = Mathf.Max(0.001f, duration); _ease = ease;
        }

        public override void Start() => _coroutine = _host.StartCoroutine(Run());

        private IEnumerator Run()
        {
            float elapsed = 0f;
            while (elapsed < _duration)
            {
                if (_t == null) yield break;
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / _duration);
                float eased = Easing.Apply(t, _ease);
                _t.localScale = Vector3.LerpUnclamped(_from, _to, eased);
                yield return null;
            }
            if (_t != null) _t.localScale = _to;
            _onComplete?.Invoke();
        }

        public override void Kill()
        {
            if (_coroutine != null) _host.StopCoroutine(_coroutine);
            base.Kill();
        }
    }

    internal class RotationTween : BaseTween
    {
        private readonly MonoBehaviour _host;
        private readonly Transform _t;
        private readonly Vector3 _from, _to;
        private readonly float _duration;
        private readonly EaseType _ease;
        private Coroutine _coroutine;

        public RotationTween(MonoBehaviour host, Transform t, Vector3 target, float duration, EaseType ease)
        {
            _host = host; _t = t; _from = t.rotation.eulerAngles; _to = target;
            _duration = Mathf.Max(0.001f, duration); _ease = ease;
        }

        public override void Start() => _coroutine = _host.StartCoroutine(Run());

        private IEnumerator Run()
        {
            float elapsed = 0f;
            while (elapsed < _duration)
            {
                if (_t == null) yield break;
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / _duration);
                float eased = Easing.Apply(t, _ease);
                _t.rotation = Quaternion.Slerp(Quaternion.Euler(_from), Quaternion.Euler(_to), eased);
                yield return null;
            }
            if (_t != null) _t.rotation = Quaternion.Euler(_to);
            _onComplete?.Invoke();
        }

        public override void Kill()
        {
            if (_coroutine != null) _host.StopCoroutine(_coroutine);
            base.Kill();
        }
    }

    internal class ShakeRotationTween : BaseTween
    {
        private readonly MonoBehaviour _host;
        private readonly Transform _t;
        private readonly float _duration;
        private readonly Vector3 _strength;
        private readonly int _vibrato;
        private Coroutine _coroutine;

        public ShakeRotationTween(MonoBehaviour host, Transform t, float duration, Vector3 strength, int vibrato)
        {
            _host = host; _t = t; _duration = Mathf.Max(0.001f, duration); _strength = strength; _vibrato = vibrato;
        }

        public override void Start() => _coroutine = _host.StartCoroutine(Run());

        private IEnumerator Run()
        {
            Quaternion start = _t.rotation;
            float elapsed = 0f;
            while (elapsed < _duration)
            {
                if (_t == null) yield break;
                elapsed += Time.deltaTime;
                float progress = elapsed / _duration;
                float decay = 1f - progress;
                float angle = Mathf.Sin(progress * Mathf.PI * _vibrato) * _strength.z * decay;
                _t.rotation = start * Quaternion.Euler(0f, 0f, angle);
                yield return null;
            }
            if (_t != null) _t.rotation = start;
            _onComplete?.Invoke();
        }

        public override void Kill()
        {
            if (_coroutine != null) _host.StopCoroutine(_coroutine);
            base.Kill();
        }
    }

    internal class CustomTween : BaseTween
    {
        private readonly MonoBehaviour _host;
        private readonly float _from, _to;
        private readonly float _duration;
        private readonly Action<object, float> _onUpdate;
        private readonly object _target;
        private Coroutine _coroutine;

        public CustomTween(MonoBehaviour host, object target, float from, float to, float duration, Action<object, float> onUpdate)
        {
            _host = host; _target = target; _from = from; _to = to;
            _duration = Mathf.Max(0.001f, duration); _onUpdate = onUpdate;
        }

        public override void Start() => _coroutine = _host.StartCoroutine(Run());

        private IEnumerator Run()
        {
            float elapsed = 0f;
            while (elapsed < _duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / _duration);
                float val = Mathf.LerpUnclamped(_from, _to, t);
                _onUpdate?.Invoke(_target, val);
                yield return null;
            }
            _onUpdate?.Invoke(_target, _to);
            _onComplete?.Invoke();
        }

        public override void Kill()
        {
            if (_coroutine != null) _host.StopCoroutine(_coroutine);
            base.Kill();
        }
    }

    internal class SequenceHandle : BaseTween
    {
        private readonly List<ITweenHandle> _chained = new List<ITweenHandle>();
        private readonly List<ITweenHandle> _grouped = new List<ITweenHandle>();
        private bool _started;

        public override void Chain(ITweenHandle other) { _chained.Add(other); }
        public override void Group(ITweenHandle other) { _grouped.Add(other); }

        public override ITweenHandle OnComplete(Action callback)
        {
            if (_chained.Count == 0 && _grouped.Count == 0)
            {
                _onComplete = callback;
                return this;
            }
            var last = _chained.Count > 0 ? _chained[_chained.Count - 1] : _grouped[_grouped.Count - 1];
            last.OnComplete(callback);
            return this;
        }

        public override void Start()
        {
            if (_started) return;
            _started = true;
            // Grouped tweens all start at t=0 (begin)
            // Chained tweens start sequentially via callback chain
            foreach (var g in _grouped) g.Start();
            for (int i = 0; i < _chained.Count; i++)
            {
                if (i == 0)
                {
                    // First chained starts after the last grouped completes
                    if (_grouped.Count > 0)
                    {
                        var last = _grouped[_grouped.Count - 1];
                        var next = _chained[0];
                        last.OnComplete(() => next.Start());
                    }
                    else
                    {
                        _chained[0].Start();
                    }
                }
                else
                {
                    var prev = _chained[i - 1];
                    var curr = _chained[i];
                    prev.OnComplete(() => curr.Start());
                }
            }
        }

        public override void Kill()
        {
            foreach (var h in _grouped) h.Kill();
            foreach (var h in _chained) h.Kill();
            base.Kill();
        }
    }

    internal class DelayHandle : BaseTween
    {
        private readonly MonoBehaviour _host;
        private readonly float _duration;
        private Coroutine _coroutine;

        public DelayHandle(MonoBehaviour host, float duration)
        {
            _host = host;
            _duration = Mathf.Max(0.001f, duration);
        }

        public override void Start() => _coroutine = _host.StartCoroutine(Run());

        private IEnumerator Run()
        {
            yield return new WaitForSeconds(_duration);
            _onComplete?.Invoke();
        }

        public override void Kill()
        {
            if (_coroutine != null) _host.StopCoroutine(_coroutine);
            base.Kill();
        }
    }

    internal static class Easing
    {
        public static float InOutSine(float t) => -(Mathf.Cos(Mathf.PI * t) - 1) / 2f;
        public static float OutBack(float t)
        {
            const float c1 = 1.3f, c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }
        public static float OutBounce(float t)
        {
            const float n1 = 7.5625f, d1 = 2.75f;
            if (t < 1f / d1) return n1 * t * t;
            if (t < 2f / d1) return n1 * (t -= 1.5f / d1) * t + 0.75f;
            if (t < 2.5f / d1) return n1 * (t -= 2.25f / d1) * t + 0.9375f;
            return n1 * (t -= 2.625f / d1) * t + 0.984375f;
        }
        public static float InOutQuad(float t) => t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;

        public static float Apply(float t, EaseType ease) => ease switch
        {
            EaseType.InOutSine => InOutSine(t),
            EaseType.OutBack => OutBack(t),
            EaseType.OutBounce => OutBounce(t),
            EaseType.InOutQuad => InOutQuad(t),
            _ => t
        };
    }
}
