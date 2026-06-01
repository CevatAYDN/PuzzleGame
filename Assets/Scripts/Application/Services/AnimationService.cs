using BottleShaders.AppServices.Interfaces;
using UnityEngine;
using System;
using System.Collections;

namespace BottleShaders.AppServices
{
    public class AnimationService : IAnimationService
    {
        private int _runningCount;

        private static readonly Vector3 _up = Vector3.up;

        public bool IsAnimating => _runningCount > 0;

        public void AnimateBottleLift(MonoBehaviour context, Transform bottle,
                                      float height, float duration, Action onComplete = null)
        {
            Vector3 target = bottle.position + _up * height;
            context.StartCoroutine(MoveRoutine(bottle, bottle.position, target, duration, onComplete));
        }

        public void AnimateBottleLower(MonoBehaviour context, Transform bottle,
                                       Vector3 originalPos, float duration, Action onComplete = null)
        {
            context.StartCoroutine(MoveRoutine(bottle, bottle.position, originalPos, duration, onComplete));
        }

        public void AnimatePour(MonoBehaviour context, Transform source, Transform target,
                                float duration, Action onComplete = null)
        {
            context.StartCoroutine(PourRoutine(source, target, duration, onComplete));
        }

        private IEnumerator MoveRoutine(Transform t, Vector3 from, Vector3 to,
                                        float duration, Action onComplete)
        {
            _runningCount++;
            float elapsed = 0f;
            float invDuration = 1f / duration;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float tValue = Mathf.SmoothStep(0f, 1f, elapsed * invDuration);
                t.position = from + (to - from) * tValue;
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

            Vector3 toTarget  = (target.position - source.position).normalized;
            float   tiltAngle = 45f;
            Vector3 tiltAxis  = Vector3.Cross(_up, toTarget);

            if (tiltAxis.sqrMagnitude < 0.0001f)
                tiltAxis = Vector3.forward;

            Quaternion startRot  = source.rotation;
            Quaternion tiltedRot  = Quaternion.AngleAxis(tiltAngle, tiltAxis) * startRot;
            Quaternion currentRot = Quaternion.identity;

            float halfDuration = duration * 0.5f;
            float invHalf      = 1f / halfDuration;
            float elapsed;

            elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float tValue = Mathf.SmoothStep(0f, 1f, elapsed * invHalf);
                currentRot = Quaternion.Slerp(startRot, tiltedRot, tValue);
                source.rotation = currentRot;
                yield return null;
            }
            source.rotation = tiltedRot;

            elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float tValue = Mathf.SmoothStep(0f, 1f, elapsed * invHalf);
                currentRot = Quaternion.Slerp(tiltedRot, startRot, tValue);
                source.rotation = currentRot;
                yield return null;
            }
            source.rotation = startRot;

            _runningCount--;
            onComplete?.Invoke();
        }
    }
}