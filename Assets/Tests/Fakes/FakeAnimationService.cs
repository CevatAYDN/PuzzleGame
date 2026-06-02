using System;
using UnityEngine;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Domain.Interfaces;

namespace PuzzleGame.Tests.Fakes
{
    /// <summary>
    /// Controllable fake for IAnimationService. Records all calls.
    /// </summary>
    public class FakeAnimationService : IAnimationService
    {
        public bool IsAnimating { get; set; }

        // Call records
        public int AnimateBottleLiftCallCount { get; private set; }
        public int AnimateBottleLowerCallCount { get; private set; }
        public int AnimatePourCallCount { get; private set; }
        public int AnimateErrorShakeCallCount { get; private set; }
        public int AnimateCorkDropCallCount { get; private set; }
        public int AnimateLiquidFlashCallCount { get; private set; }
        public int AnimateSettleBounceCallCount { get; private set; }

        public IBottleView LastPourSource { get; private set; }
        public IBottleView LastPourTarget { get; private set; }
        public Transform LastShakeTransform { get; private set; }

        public void AnimateBottleLift(Transform bottle, float height, float duration,
            Func<bool> keepHovering = null, Action onComplete = null)
        {
            AnimateBottleLiftCallCount++;
            onComplete?.Invoke();
        }

        public void AnimateBottleLower(Transform bottle, Vector3 originalPos, float duration,
            Action onComplete = null)
        {
            AnimateBottleLowerCallCount++;
            onComplete?.Invoke();
        }

        public void AnimatePour(IBottleView source, IBottleView target, float duration,
            Action onComplete = null)
        {
            AnimatePourCallCount++;
            LastPourSource = source;
            LastPourTarget = target;
            onComplete?.Invoke();
        }

        public void AnimateErrorShake(Transform bottle, Action onComplete = null)
        {
            AnimateErrorShakeCallCount++;
            LastShakeTransform = bottle;
            onComplete?.Invoke();
        }

        public void AnimateCorkDrop(Transform cork, float bottleHeight, Action onComplete = null)
        {
            AnimateCorkDropCallCount++;
            onComplete?.Invoke();
        }

        public void AnimateLiquidFlash(Renderer renderer, int materialSlot,
            float peakIntensity, float duration, Action onComplete = null)
        {
            AnimateLiquidFlashCallCount++;
            onComplete?.Invoke();
        }

        public void AnimateSettleBounce(IBottleView bottle, float duration,
            Action onComplete = null)
        {
            AnimateSettleBounceCallCount++;
            onComplete?.Invoke();
        }
    }
}
