using UnityEngine;
using System;

namespace PuzzleGame.Application.Interfaces
{
    /// <summary>
    /// Drives all bottle animations using PrimeTween.
    /// Plain C# class — no scene dependency, no MonoBehaviour context needed.
    /// </summary>
    public interface IAnimationService
    {
        /// <summary>True while any tween is running.</summary>
        bool IsAnimating { get; }

        /// <summary>Lifts a bottle upward by <paramref name="height"/> units.</summary>
        void AnimateBottleLift(Transform bottle,
                               float height, float duration,
                               Func<bool> keepHovering = null,
                               Action onComplete = null);

        /// <summary>Returns a bottle to its original position.</summary>
        void AnimateBottleLower(Transform bottle,
                                Vector3 originalPos, float duration,
                                Action onComplete = null);

        /// <summary>
        /// Plays the pour sequence: tilt source toward target, wait, then restore.
        /// Calls <paramref name="onComplete"/> after the tilt-back finishes.
        /// </summary>
        void AnimatePour(IBottleView source, IBottleView target,
                         float duration, Action onComplete = null);

        /// <summary>
        /// Plays a rapid error shake (left/right wiggle) to signify an invalid pour.
        /// </summary>
        void AnimateErrorShake(Transform bottle, Action onComplete = null);

        /// <summary>
        /// Plays a corked-bottle drop animation: cork descends + scales up with bounce.
        /// </summary>
        void AnimateCorkDrop(Transform cork, float bottleHeight, Action onComplete = null);

        /// <summary>
        /// Plays a rim-light flash on the liquid material (slot 1).
        /// </summary>
        void AnimateLiquidFlash(Renderer renderer, int materialSlot,
                                float peakIntensity, float duration,
                                Action onComplete = null);

        /// <summary>
        /// Plays a settle bounce on the visual fill of a bottle.
        /// </summary>
        void AnimateSettleBounce(IBottleView bottle, float duration,
                                 Action onComplete = null);
    }
}
