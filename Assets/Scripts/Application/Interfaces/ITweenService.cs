using UnityEngine;
using System;

namespace PuzzleGame.Application.Interfaces
{
    /// <summary>
    /// Lightweight tween abstraction. AnimationService uses this via DI.
    /// Swap implementation to switch between PrimeTween / DOTween / coroutine fallback.
    /// </summary>
    public interface ITweenService
    {
        ITweenHandle TweenPosition(Transform t, Vector3 target, float duration, EaseType ease);
        ITweenHandle TweenLocalPosition(Transform t, Vector3 target, float duration, EaseType ease);
        ITweenHandle TweenScale(Transform t, Vector3 target, float duration, EaseType ease);
        ITweenHandle TweenRotation(Transform t, Vector3 target, float duration, EaseType ease);
        ITweenHandle TweenShakeRotation(Transform t, float duration, Vector3 strength, int vibrato);
        ITweenHandle TweenCustom(object target, float from, float to, float duration, Action<object, float> onUpdate);
        ITweenHandle SequenceCreate();
        ITweenHandle Delay(float duration);
    }

    public interface ITweenHandle
    {
        void Chain(ITweenHandle other);
        void Group(ITweenHandle other);
        ITweenHandle OnComplete(Action callback);
        void SetCycles(int loops, LoopMode mode);
        void Kill();
        void Start();
    }

    public enum EaseType
    {
        Linear,
        InOutSine,
        OutBack,
        OutBounce,
        InOutQuad
    }

    public enum LoopMode
    {
        Restart, Yoyo, Incremental
    }
}
