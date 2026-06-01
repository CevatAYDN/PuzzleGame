using UnityEngine;
using System;

namespace BottleShaders.Application.Interfaces
{
    /// <summary>
    /// Drives all bottle animations. Runs coroutines via a MonoBehaviour context
    /// so the service itself stays a plain C# class (no scene dependency).
    /// </summary>
    public interface IAnimationService
    {
        /// <summary>True while any animation coroutine is running.</summary>
        bool IsAnimating { get; }

        /// <summary>Lifts a bottle upward by <paramref name="height"/> units.</summary>
        void AnimateBottleLift(MonoBehaviour context, Transform bottle,
                               float height, float duration, Func<bool> keepHovering = null, Action onComplete = null);

        /// <summary>Returns a bottle to its original position.</summary>
        void AnimateBottleLower(MonoBehaviour context, Transform bottle,
                                Vector3 originalPos, float duration, Action onComplete = null);

        /// <summary>
        /// Plays the pour sequence: tilt source toward target, wait, then restore.
        /// Calls <paramref name="onComplete"/> after the tilt-back finishes.
        /// </summary>
        void AnimatePour(MonoBehaviour context, BottleController source, BottleController target,
                         float duration, Action onComplete = null);
    }
}
