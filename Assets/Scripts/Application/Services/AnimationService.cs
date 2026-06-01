using System;
using System.Collections;
using System.Collections.Generic;
using BottleShaders.Application.Interfaces;
using BottleShaders.Domain.Models;
using UnityEngine;

namespace BottleShaders.Application.Services
{
    public class AnimationService : IAnimationService
    {
        private readonly Configuration.AnimationConfig _config;
        private int _runningCount;

        private static readonly Vector3 _up = Vector3.up;

        public bool IsAnimating => _runningCount > 0;

        public AnimationService(Configuration.AnimationConfig config)
        {
            _config = config;
        }

        public void AnimateBottleLift(MonoBehaviour context, Transform bottle,
                                      float height, float duration, Func<bool> keepHovering = null, Action onComplete = null)
        {
            Vector3 target = bottle.position + _up * height;
            context.StartCoroutine(MoveRoutine(bottle, bottle.position, target, duration, keepHovering, onComplete));
        }

        public void AnimateBottleLower(MonoBehaviour context, Transform bottle,
                                       Vector3 originalPos, float duration, Action onComplete = null)
        {
            context.StartCoroutine(MoveRoutine(bottle, bottle.position, originalPos, duration, null, onComplete));
        }

        public void AnimatePour(MonoBehaviour context, BottleController source, BottleController target,
                                float duration, Action onComplete = null)
        {
            context.StartCoroutine(PourRoutine(source, target, duration, onComplete));
        }

        public void AnimateErrorShake(MonoBehaviour context, Transform bottle, Action onComplete = null)
        {
            context.StartCoroutine(ErrorShakeRoutine(bottle, onComplete));
        }

        private IEnumerator MoveRoutine(Transform t, Vector3 from, Vector3 to,
                                        float duration, Func<bool> keepHovering, Action onComplete)
        {
            _runningCount++;
            float elapsed = 0f;
            float invDuration = 1f / duration;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float tValue = Mathf.Clamp01(elapsed * invDuration);
                
                float easedT = keepHovering != null ? EaseOutBack(tValue) : Mathf.SmoothStep(0f, 1f, tValue);
                
                t.position = from + (to - from) * easedT;
                yield return null;
            }

            t.position = to;
            _runningCount--;
            onComplete?.Invoke();

            if (keepHovering != null && _config != null)
            {
                float hoverTime = 0f;
                float amplitude = _config.hoverAmplitude;
                float frequency = _config.hoverFrequency;

                while (keepHovering())
                {
                    hoverTime += Time.deltaTime;
                    float offset = Mathf.Sin(hoverTime * frequency) * amplitude;
                    t.position = to + Vector3.up * offset;
                    yield return null;
                }
            }
        }

        private IEnumerator ErrorShakeRoutine(Transform t, Action onComplete)
        {
            _runningCount++;
            float elapsed = 0f;
            float duration = _config != null ? _config.shakeDuration : 0.25f;
            float shakeAngle = _config != null ? _config.shakeAngle : 8f;
            Quaternion startRot = t.rotation;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                
                // Snappy decaying sine wave wobble
                float angle = Mathf.Sin(progress * Mathf.PI * 4f) * shakeAngle * (1f - progress);
                t.rotation = startRot * Quaternion.Euler(0f, 0f, angle);
                yield return null;
            }

            t.rotation = startRot;
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

            Vector3 startPos     = sourceT.position;
            Quaternion startRot  = sourceT.rotation;
            Quaternion tiltedRot  = Quaternion.AngleAxis(tiltAngle, tiltAxis) * startRot;

            // Compute exact pour position so source mouth aligns with target mouth
            Vector3 targetMouth      = targetT.position + Vector3.up * (target.Height + 0.15f);
            Vector3 localSourceMouth = new Vector3(0f, source.Height, 0f);
            Vector3 pourPos          = targetMouth - tiltedRot * localSourceMouth;

            float halfDuration = duration * 0.5f;
            float invHalf      = 1f / halfDuration;
            float elapsed;

            var sourceStart = new LayerSnapshot(source.VisualLayers);
            var targetStart = new LayerSnapshot(target.VisualLayers);
            
            LiquidLayer pouredLayer = target.State.TopLayer ?? new LiquidLayer(Color.clear, 0f);

            var lr = source.GetComponent<LineRenderer>();
            if (lr == null)
            {
                lr = source.gameObject.AddComponent<LineRenderer>();
                lr.useWorldSpace = true;
                lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                lr.receiveShadows = false;
                
                var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
                if (shader != null)
                {
                    lr.material = new Material(shader);
                }
            }

            Color streamColor = pouredLayer.Color.ToUnityColor();
            lr.startColor = streamColor;
            lr.endColor = streamColor;
            if (lr.material != null)
            {
                lr.material.color = streamColor;
                lr.material.SetColor("_BaseColor", streamColor);
            }

            lr.enabled = true;
            float elapsedTotal = 0f;

            elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                elapsedTotal += Time.deltaTime;
                float tValue = Mathf.SmoothStep(0f, 1f, elapsed * invHalf);
                
                sourceT.position = Vector3.Lerp(startPos, pourPos, tValue);
                sourceT.rotation = Quaternion.Slerp(startRot, tiltedRot, tValue);

                UpdateVisualPourProgress(source, target, sourceStart, targetStart, pouredLayer, elapsedTotal / duration);
                UpdateStream(lr, source, target, sourceT, targetT, elapsedTotal / duration);

                yield return null;
            }
            sourceT.position = pourPos;
            sourceT.rotation = tiltedRot;

            elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                elapsedTotal += Time.deltaTime;
                float tValue = Mathf.SmoothStep(0f, 1f, elapsed * invHalf);
                
                sourceT.position = Vector3.Lerp(pourPos, startPos, tValue);
                sourceT.rotation = Quaternion.Slerp(tiltedRot, startRot, tValue);

                UpdateVisualPourProgress(source, target, sourceStart, targetStart, pouredLayer, elapsedTotal / duration);
                UpdateStream(lr, source, target, sourceT, targetT, elapsedTotal / duration);

                yield return null;
            }
            sourceT.position = startPos;
            sourceT.rotation = startRot;

            if (lr != null)
            {
                lr.enabled = false;
            }

            source.UpdateVisualsFromState();
            target.UpdateVisualsFromState();
            target.PlaySettleBounce(); // Dynamic liquid settle slosh

            _runningCount--;
            onComplete?.Invoke();
        }

        private void UpdateVisualPourProgress(BottleController source, BottleController target,
                                               LayerSnapshot sourceStart, LayerSnapshot targetStart,
                                               LiquidLayer pouredLayer, float t)
        {
            t = Mathf.Clamp01(t);
            source.SetVisualPourProgress(sourceStart, t, true, pouredLayer);
            target.SetVisualPourProgress(targetStart, t, false, pouredLayer);
        }

        private void UpdateStream(LineRenderer lr, BottleController source, BottleController target,
                                  Transform sourceT, Transform targetT, float t)
        {
            if (lr == null) return;

            // Bell curve stream width: starts thin, peaks at mid-pour, goes thin
            float widthFactor = Mathf.Clamp01(Mathf.Sin(t * Mathf.PI) * 1.5f);
            float baseWidth = _config != null ? _config.streamWidth * widthFactor : 0.08f * widthFactor;

            // Source mouth offset rotated in world space
            Vector3 sourceMouth = sourceT.TransformPoint(new Vector3(0f, source.Height, 0f));
            // Target mouth straight offset in world space
            Vector3 targetMouth = targetT.position + Vector3.up * target.Height;

            int segmentCount = 15;
            lr.positionCount = segmentCount;

            var widthCurve = new AnimationCurve();
            float rippleSpeed = 22f;
            float rippleFreq = 8f;

            for (int i = 0; i < segmentCount; i++)
            {
                float segmentT = (float)i / (segmentCount - 1);
                
                // Parabolic gravity drop (curves downward in world space)
                float distH = Vector3.Distance(new Vector3(sourceMouth.x, 0f, sourceMouth.z), new Vector3(targetMouth.x, 0f, targetMouth.z));
                float gravityArc = Mathf.Sin(segmentT * Mathf.PI) * (0.08f + distH * 0.15f);
                
                Vector3 pos = Vector3.Lerp(sourceMouth, targetMouth, segmentT);
                pos.y -= gravityArc;
                lr.SetPosition(i, pos);

                // Wave ripple calculation: simulates flowing water ripples moving down the stream
                float wave = Mathf.Sin(segmentT * rippleFreq - Time.time * rippleSpeed) * 0.2f;
                float segWidth = baseWidth * (1f + wave);
                
                widthCurve.AddKey(segmentT, segWidth);
            }

            lr.widthCurve = widthCurve;
        }

        private static float EaseOutBack(float x)
        {
            const float c1 = 1.3f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
        }
    }
}