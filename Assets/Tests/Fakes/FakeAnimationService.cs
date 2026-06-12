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
        public int AnimateMoldLiftCallCount { get; private set; }
        public int AnimateMoldLowerCallCount { get; private set; }
        public int AnimateCastCallCount { get; private set; }
        public int AnimateErrorShakeCallCount { get; private set; }
        public int AnimateCorkDropCallCount { get; private set; }
        public int AnimateOreFlashCallCount { get; private set; }
        public int AnimateSettleBounceCallCount { get; private set; }

        public IMoldView LastCastSource { get; private set; }
        public IMoldView LastCastTarget { get; private set; }
        public Transform LastShakeTransform { get; private set; }

        public void AnimateMoldLift(Transform Mold, float height, float duration,
            Func<bool> keepHovering = null, Action onComplete = null)
        {
            AnimateMoldLiftCallCount++;
            onComplete?.Invoke();
        }

        public void AnimateMoldLower(Transform Mold, Vector3 originalPos, float duration,
            Action onComplete = null)
        {
            AnimateMoldLowerCallCount++;
            onComplete?.Invoke();
        }

        public void AnimateCast(IMoldView source, IMoldView target, float duration,
            Action onComplete = null)
        {
            AnimateCastCallCount++;
            LastCastSource = source;
            LastCastTarget = target;
            onComplete?.Invoke();
        }

        public void AnimateErrorShake(Transform Mold, Action onComplete = null)
        {
            AnimateErrorShakeCallCount++;
            LastShakeTransform = Mold;
            onComplete?.Invoke();
        }

        public void AnimateCorkDrop(Transform cork, float MoldHeight, Action onComplete = null)
        {
            AnimateCorkDropCallCount++;
            onComplete?.Invoke();
        }

        public void AnimateOreFlash(Renderer renderer, int materialSlot,
            float peakIntensity, float duration, Action onComplete = null)
        {
            AnimateOreFlashCallCount++;
            onComplete?.Invoke();
        }

        public void AnimateSettleBounce(IMoldView Mold, float duration,
            Action onComplete = null)
        {
            AnimateSettleBounceCallCount++;
            onComplete?.Invoke();
        }

        public int ForceUnlockCallCount { get; private set; }
        public void ForceUnlock()
        {
            ForceUnlockCallCount++;
            IsAnimating = false;
        }
    }
}
