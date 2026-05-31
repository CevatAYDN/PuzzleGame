using BottleShaders.AppServices.Interfaces;
using UnityEngine;
using System;
using System.Collections;

namespace BottleShaders.AppServices
{
    /// <summary>
    /// Plain C# animation service — delegates coroutine execution to a MonoBehaviour context.
    /// Tracks a running-coroutine count so IsAnimating is race-safe even when
    /// multiple animations overlap (e.g. lift + lower at the same time).
    /// </summary>
    public class AnimationService : IAnimationService
    {
        private int _runningCount;

        /// <inheritdoc/>
        public bool IsAnimating => _runningCount > 0;

        // ── IAnimationService ────────────────────────────────────────────────

        /// <inheritdoc/>
        public void AnimateBottleLift(MonoBehaviour context, Transform bottle,
                                      float height, float duration, Action onComplete = null)
        {
            var target = bottle.position + Vector3.up * height;
            context.StartCoroutine(MoveRoutine(bottle, bottle.position, target, duration, onComplete));
        }

        /// <inheritdoc/>
        public void AnimateBottleLower(MonoBehaviour context, Transform bottle,
                                       Vector3 originalPos, float duration, Action onComplete = null)
        {
            context.StartCoroutine(MoveRoutine(bottle, bottle.position, originalPos, duration, onComplete));
        }

        /// <inheritdoc/>
        public void AnimatePour(MonoBehaviour context, Transform source, Transform target,
                                float duration, Action onComplete = null)
        {
            context.StartCoroutine(PourRoutine(source, target, duration, onComplete));
        }

        // ── Coroutines ───────────────────────────────────────────────────────

        private IEnumerator MoveRoutine(Transform t, Vector3 from, Vector3 to,
                                        float duration, Action onComplete)
        {
            _runningCount++;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                t.position = Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, elapsed / duration));
                yield return null;
            }

            t.position = to;
            _runningCount--;
            onComplete?.Invoke();
        }

        private IEnumerator PourRoutine(Transform source, Transform target,
                                        float duration, Action onComplete)
        {
            _runningCount++;

            // Determine tilt direction: lean toward the target bottle
            Vector3 toTarget   = (target.position - source.position).normalized;
            float   tiltAngle  = 45f;
            Vector3 tiltAxis   = Vector3.Cross(Vector3.up, toTarget).normalized;
            if (tiltAxis == Vector3.zero) tiltAxis = Vector3.forward;

            Quaternion startRot  = source.rotation;
            Quaternion tiltedRot = Quaternion.AngleAxis(tiltAngle, tiltAxis) * startRot;

            float half = duration * 0.5f;

            // Tilt forward
            float elapsed = 0f;
            while (elapsed < half)
            {
                elapsed += Time.deltaTime;
                source.rotation = Quaternion.Slerp(startRot, tiltedRot,
                                                   Mathf.SmoothStep(0f, 1f, elapsed / half));
                yield return null;
            }
            source.rotation = tiltedRot;

            // Tilt back
            elapsed = 0f;
            while (elapsed < half)
            {
                elapsed += Time.deltaTime;
                source.rotation = Quaternion.Slerp(tiltedRot, startRot,
                                                   Mathf.SmoothStep(0f, 1f, elapsed / half));
                yield return null;
            }
            source.rotation = startRot;

            _runningCount--;
            onComplete?.Invoke();
        }
    }
}
