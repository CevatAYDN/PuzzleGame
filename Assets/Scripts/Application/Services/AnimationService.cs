using System;
using System.Collections;
using System.Collections.Generic;
using BottleShaders.AppServices.Interfaces;
using BottleShaders.Domain.Models;
using UnityEngine;

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

        public void AnimatePour(MonoBehaviour context, BottleController source, BottleController target,
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

        private IEnumerator PourRoutine(BottleController source, BottleController target,
                                        float duration, Action onComplete)
        {
            _runningCount++;

            Transform sourceT = source.transform;
            Transform targetT = target.transform;

            Vector3 toTarget  = (targetT.position - sourceT.position).normalized;
            float   tiltAngle = 45f;
            Vector3 tiltAxis  = Vector3.Cross(_up, toTarget);

            if (tiltAxis.sqrMagnitude < 0.0001f)
                tiltAxis = Vector3.forward;

            Quaternion startRot  = sourceT.rotation;
            Quaternion tiltedRot  = Quaternion.AngleAxis(tiltAngle, tiltAxis) * startRot;
            Quaternion currentRot = Quaternion.identity;

            float halfDuration = duration * 0.5f;
            float invHalf      = 1f / halfDuration;
            float elapsed;

            var sourceStartLayers = new List<LiquidLayer>(source.VisualLayers);
            var targetStartLayers = new List<LiquidLayer>(target.VisualLayers);
            
            LiquidLayer pouredLayer = target.State.TopLayer ?? new LiquidLayer(Color.clear, 0f);

            float elapsedTotal = 0f;

            elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                elapsedTotal += Time.deltaTime;
                float tValue = Mathf.SmoothStep(0f, 1f, elapsed * invHalf);
                currentRot = Quaternion.Slerp(startRot, tiltedRot, tValue);
                sourceT.rotation = currentRot;

                UpdateVisualPourProgress(source, target, sourceStartLayers, targetStartLayers, pouredLayer, elapsedTotal / duration);

                yield return null;
            }
            sourceT.rotation = tiltedRot;

            elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                elapsedTotal += Time.deltaTime;
                float tValue = Mathf.SmoothStep(0f, 1f, elapsed * invHalf);
                currentRot = Quaternion.Slerp(tiltedRot, startRot, tValue);
                sourceT.rotation = currentRot;

                UpdateVisualPourProgress(source, target, sourceStartLayers, targetStartLayers, pouredLayer, elapsedTotal / duration);

                yield return null;
            }
            sourceT.rotation = startRot;

            source.UpdateVisualsFromState();
            target.UpdateVisualsFromState();

            _runningCount--;
            onComplete?.Invoke();
        }

        private void UpdateVisualPourProgress(BottleController source, BottleController target,
                                               List<LiquidLayer> sourceStart, List<LiquidLayer> targetStart,
                                               LiquidLayer pouredLayer, float t)
        {
            t = Mathf.Clamp01(t);

            var sourceCurrent = new List<LiquidLayer>(sourceStart);
            if (sourceCurrent.Count > 0)
            {
                int topIdx = sourceCurrent.Count - 1;
                var top = sourceCurrent[topIdx];
                sourceCurrent[topIdx] = top.WithAmount(top.Amount * (1f - t));
            }
            float sourceTotal = 0f;
            foreach (var l in sourceCurrent) sourceTotal += l.Amount;
            source.SetVisualState(sourceCurrent, sourceTotal);

            var targetCurrent = new List<LiquidLayer>(targetStart);
            targetCurrent.Add(pouredLayer.WithAmount(pouredLayer.Amount * t));
            float targetTotal = 0f;
            foreach (var l in targetCurrent) targetTotal += l.Amount;
            target.SetVisualState(targetCurrent, targetTotal);
        }
    }
}