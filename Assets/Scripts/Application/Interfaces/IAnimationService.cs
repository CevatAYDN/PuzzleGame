using UnityEngine;
using System;

namespace PuzzleGame.Application.Interfaces
{
    /// <summary>
    /// Drives all Mold animations using PrimeTween.
    /// Plain C# class — no scene dependency, no MonoBehaviour context needed.
    /// </summary>
    public interface IAnimationService
    {
        /// <summary>True while any tween is running.</summary>
        bool IsAnimating { get; }

        /// <summary>Lifts a Mold upward by <paramref name="height"/> units.</summary>
        void AnimateMoldLift(Transform Mold,
                               float height, float duration,
                               Func<bool> keepHovering = null,
                               Action onComplete = null);

        /// <summary>Returns a Mold to its original position.</summary>
        void AnimateMoldLower(Transform Mold,
                                Vector3 originalPos, float duration,
                                Action onComplete = null);

        /// <summary>
        /// Plays the Cast sequence: tilt source toward target, wait, then restore.
        /// Calls <paramref name="onComplete"/> after the tilt-back finishes.
        /// </summary>
        void AnimateCast(IMoldView source, IMoldView target,
                         float duration, Action onComplete = null);

        /// <summary>
        /// Plays a rapid error shake (left/right wiggle) to signify an invalid Cast.
        /// </summary>
        void AnimateErrorShake(Transform Mold, Action onComplete = null);

        /// <summary>
        /// Plays a corked-Mold drop animation: cork descends + scales up with bounce.
        /// </summary>
        void AnimateCorkDrop(Transform cork, float MoldHeight, Action onComplete = null);

        /// <summary>
        /// Plays a rim-light flash on the Ore material (slot 1).
        /// </summary>
        void AnimateOreFlash(Renderer renderer, int materialSlot,
                                float peakIntensity, float duration,
                                Action onComplete = null);

        /// <summary>
        /// Plays a settle bounce on the visual fill of a Mold.
        /// </summary>
        void AnimateSettleBounce(IMoldView Mold, float duration,
                                 Action onComplete = null);
    }
}
