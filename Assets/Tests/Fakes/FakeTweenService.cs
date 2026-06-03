using System;
using UnityEngine;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Domain.Interfaces;

namespace PuzzleGame.Tests.Fakes
{
    /// <summary>
    /// No-op fake for ITweenService. All tweens complete immediately.
    /// </summary>
    public class FakeTweenService : ITweenService
    {
        public int TweenPositionCallCount { get; private set; }
        public int TweenCustomCallCount { get; private set; }
        public int DelayCallCount { get; private set; }

        public ITweenHandle TweenPosition(Transform target, Vector3 endValue, float duration, EaseType ease)
        {
            TweenPositionCallCount++;
            var handle = new FakeTweenHandle();
            handle.InvokeComplete();
            return handle;
        }

        public ITweenHandle TweenScale(Transform target, Vector3 endValue, float duration, EaseType ease)
        {
            var handle = new FakeTweenHandle();
            handle.InvokeComplete();
            return handle;
        }

        public ITweenHandle TweenRotation(Transform target, Vector3 endValue, float duration, EaseType ease)
        {
            var handle = new FakeTweenHandle();
            handle.InvokeComplete();
            return handle;
        }

        public ITweenHandle TweenLocalPosition(Transform target, Vector3 endValue, float duration, EaseType ease)
        {
            var handle = new FakeTweenHandle();
            handle.InvokeComplete();
            return handle;
        }

        public ITweenHandle TweenShakeRotation(Transform target, float duration, Vector3 strength, int vibrato)
        {
            var handle = new FakeTweenHandle();
            handle.InvokeComplete();
            return handle;
        }

        public ITweenHandle TweenCustom(object target, float from, float to, float duration, Action<object, float> onUpdate)
        {
            TweenCustomCallCount++;
            var handle = new FakeTweenHandle();
            onUpdate?.Invoke(target, to);
            handle.InvokeComplete();
            return handle;
        }

        public ITweenHandle SequenceCreate()
        {
            var handle = new FakeTweenHandle();
            handle.InvokeComplete();
            return handle;
        }

        public ITweenHandle Delay(float duration)
        {
            DelayCallCount++;
            var handle = new FakeTweenHandle();
            handle.InvokeComplete();
            return handle;
        }
    }

    /// <summary>
    /// Fake tween handle that records callbacks.
    /// </summary>
    public class FakeTweenHandle : ITweenHandle
    {
        private Action _onComplete;
        private bool _isCompleted;

        public void Chain(ITweenHandle other)
        {
        }

        public void Group(ITweenHandle other)
        {
        }

        public ITweenHandle OnComplete(Action callback)
        {
            _onComplete = callback;
            if (_isCompleted)
            {
                _onComplete?.Invoke();
            }
            return this;
        }

        public void SetCycles(int loops, LoopMode mode)
        {
        }

        public void Kill()
        {
            _onComplete = null;
        }

        public void Start()
        {
        }

        public void InvokeComplete()
        {
            _isCompleted = true;
            _onComplete?.Invoke();
        }
    }
}
