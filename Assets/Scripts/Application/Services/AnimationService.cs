using System;
using System.Collections;
using System.Collections.Generic;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Domain.Models;
using PuzzleGame.Infrastructure;
using UnityEngine;

namespace PuzzleGame.Application.Services
{
    public class AnimationService : IAnimationService
    {
        private readonly Configuration.AnimationConfig _config;
        private int _runningCount;

        private const int MaxPoolSize = 16; // Memory leak guard

        private readonly List<ParticleSystem> _splashPool = new List<ParticleSystem>();
        private readonly List<ParticleSystem> _bubblePool = new List<ParticleSystem>();

        public bool IsAnimating => _runningCount > 0;

        public AnimationService(Configuration.AnimationConfig config)
        {
            _config = config;
        }

        public void AnimateBottleLift(MonoBehaviour context, Transform bottle,
                                      float height, float duration, Func<bool> keepHovering = null, Action onComplete = null)
        {
            Vector3 target = bottle.position + Vector3.up * height;
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
            context.StartCoroutine(PourRoutine(context, source, target, duration, onComplete));
        }

        public void AnimateErrorShake(MonoBehaviour context, Transform bottle, Action onComplete = null)
        {
            context.StartCoroutine(ErrorShakeRoutine(bottle, onComplete));
        }

        private IEnumerator MoveRoutine(Transform t, Vector3 from, Vector3 to,
                                        float duration, Func<bool> keepHovering, Action onComplete)
        {
            if (duration <= 0f) duration = 0.001f;
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
            if (duration <= 0f) duration = 0.001f;
            float shakeAngle = _config != null ? _config.shakeAngle : 8f;
            Quaternion startRot = t.rotation;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                float angle = Mathf.Sin(progress * Mathf.PI * 4f) * shakeAngle * (1f - progress);
                t.rotation = startRot * Quaternion.Euler(0f, 0f, angle);
                yield return null;
            }

            t.rotation = startRot;
            _runningCount--;
            onComplete?.Invoke();
        }

        private ParticleSystem CreateSplashParticles(Texture2D tex)
        {
            GameObject go = new GameObject("PourSplashParticles");
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.duration = 1f;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.45f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
            main.gravityModifier = new ParticleSystem.MinMaxCurve(2.0f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false;

            var emission = ps.emission;
            emission.rateOverTime = new ParticleSystem.MinMaxCurve(45f);

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 25f;
            shape.radius = 0.04f;
            shape.rotation = new Vector3(-90f, 0f, 0f);

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve curve = new AnimationCurve();
            curve.AddKey(0f, 1f);
            curve.AddKey(0.7f, 0.8f);
            curve.AddKey(1f, 0f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sharedMaterial = ParticleMaterialFactory.GetSplashMaterial(tex);

            return ps;
        }

        private ParticleSystem CreateBubbleParticles(Texture2D tex)
        {
            GameObject go = new GameObject("PourBubbleParticles");
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.duration = 1f;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.10f);
            main.gravityModifier = new ParticleSystem.MinMaxCurve(-0.06f);
            main.startColor = new Color(1f, 1f, 1f, 0.45f);
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.playOnAwake = false;

            var emission = ps.emission;
            emission.rateOverTime = new ParticleSystem.MinMaxCurve(15f);

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 0f;
            shape.radius = 0.15f;
            shape.length = 0.1f;
            shape.rotation = new Vector3(-90f, 0f, 0f);

            var noise = ps.noise;
            noise.enabled = true;
            noise.frequency = 2.0f;
            noise.strength = new ParticleSystem.MinMaxCurve(0.15f);
            noise.octaveCount = 1;

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve curve = new AnimationCurve();
            curve.AddKey(0f, 0.2f);
            curve.AddKey(0.2f, 1.0f);
            curve.AddKey(0.8f, 1.0f);
            curve.AddKey(1f, 0f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sharedMaterial = ParticleMaterialFactory.GetBubbleMaterial(tex);

            return ps;
        }

        private ParticleSystem GetSplashParticles(Transform parent, Color color, Texture2D tex, MonoBehaviour context)
        {
            ParticleSystem ps = TakeFromPool(_splashPool);
            if (ps == null)
            {
                if (_splashPool.Count >= MaxPoolSize)
                {
                    var oldest = _splashPool[0];
                    _splashPool.RemoveAt(0);
                    if (oldest != null) UnityEngine.Object.Destroy(oldest.gameObject);
                }
                ps = CreateSplashParticles(tex);
            }

            ps.transform.SetParent(parent, false);
            ps.transform.localPosition = Vector3.zero;
            ps.transform.localRotation = Quaternion.identity;
            ps.gameObject.SetActive(true);
            var main = ps.main;
            main.startColor = color;
            return ps;
        }

        private ParticleSystem GetBubbleParticles(Transform parent, Texture2D tex, float bottleHeight, MonoBehaviour context)
        {
            ParticleSystem ps = TakeFromPool(_bubblePool);
            if (ps == null)
            {
                if (_bubblePool.Count >= MaxPoolSize)
                {
                    var oldest = _bubblePool[0];
                    _bubblePool.RemoveAt(0);
                    if (oldest != null) UnityEngine.Object.Destroy(oldest.gameObject);
                }
                ps = CreateBubbleParticles(tex);
            }

            ps.transform.SetParent(parent, false);
            ps.transform.localPosition = Vector3.zero;
            ps.transform.localRotation = Quaternion.identity;
            ps.gameObject.SetActive(true);
            var shape = ps.shape;
            shape.length = Mathf.Max(bottleHeight, 0.1f);
            return ps;
        }

        private static ParticleSystem TakeFromPool(List<ParticleSystem> pool)
        {
            for (int i = pool.Count - 1; i >= 0; i--)
            {
                if (pool[i] == null)
                {
                    pool.RemoveAt(i);
                    continue;
                }
                if (!pool[i].gameObject.activeSelf)
                {
                    var ps = pool[i];
                    pool.RemoveAt(i);
                    return ps;
                }
            }
            return null;
        }

        private void ReturnSplashToPool(ParticleSystem ps, MonoBehaviour context, float delay)
        {
            if (ps == null) return;
            context.StartCoroutine(ReturnToPoolRoutine(_splashPool, ps, delay));
        }

        private void ReturnBubbleToPool(ParticleSystem ps, MonoBehaviour context, float delay)
        {
            if (ps == null) return;
            context.StartCoroutine(ReturnToPoolRoutine(_bubblePool, ps, delay));
        }

        private IEnumerator ReturnToPoolRoutine(List<ParticleSystem> pool, ParticleSystem ps, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (ps != null)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.gameObject.SetActive(false);
                ps.transform.SetParent(null);
                pool.Add(ps);
            }
        }

        private IEnumerator PourRoutine(MonoBehaviour context, BottleController source, BottleController target,
                                        float duration, Action onComplete)
        {
            if (duration <= 0f) duration = 0.001f;
            _runningCount++;

            Transform sourceT = source.transform;
            Transform targetT = target.transform;

            Vector3 toTarget  = (targetT.position - sourceT.position).normalized;
            float tiltAngle = 45f;
            Vector3 tiltAxis = Vector3.Cross(Vector3.up, toTarget);
            if (tiltAxis.sqrMagnitude < 0.0001f) tiltAxis = Vector3.forward;

            Vector3 startPos = sourceT.position;
            Quaternion startRot = sourceT.rotation;
            Quaternion tiltedRot = Quaternion.AngleAxis(tiltAngle, tiltAxis) * startRot;

            Vector3 targetMouth = targetT.position + Vector3.up * (target.Height + 0.15f);
            Vector3 localSourceMouth = new Vector3(0f, source.Height, 0f);
            Vector3 pourPos = targetMouth - tiltedRot * localSourceMouth;

            float tiltPortion   = _config != null ? _config.tiltPhasePortion : 0.25f;
            float flowPortion   = _config != null ? _config.flowPhasePortion : 0.50f;
            float returnPortion = _config != null ? _config.returnPhasePortion : 0.25f;

            float tiltDuration   = duration * tiltPortion;
            float flowDuration   = duration * flowPortion;
            float returnDuration = duration * returnPortion;
            float elapsed;

            var sourceStart = new LayerSnapshot(source.VisualLayers);
            var targetStart = new LayerSnapshot(target.VisualLayers);
            LiquidLayer pouredLayer = target.State.TopLayer ?? new LiquidLayer(new DomainColor(0, 0, 0, 0), 0f);

            LineRenderer lr = StreamRenderer.EnsureLineRenderer(source.gameObject);
            Color streamColor = ColorAdapter.ToUnity(pouredLayer.Color);
            StreamRenderer.SetColor(lr, streamColor);

            lr.enabled = false;

            // Phase 1: Tilt & Travel
            elapsed = 0f;
            while (elapsed < tiltDuration)
            {
                elapsed += Time.deltaTime;
                float tValue = Mathf.Clamp01(elapsed / tiltDuration);
                float easedT = Mathf.SmoothStep(0f, 1f, tValue);
                sourceT.position = Vector3.Lerp(startPos, pourPos, easedT);
                sourceT.rotation = Quaternion.Slerp(startRot, tiltedRot, easedT);
                UpdateVisualPourProgress(source, target, sourceStart, targetStart, pouredLayer, 0f);
                yield return null;
            }
            sourceT.position = pourPos;
            sourceT.rotation = tiltedRot;

            // Phase 2: Hold & Flow
            lr.positionCount = StreamRenderer.TotalSegments;
            lr.enabled = true;
            elapsed = 0f;

            Texture2D solidTex = TextureGenerator.GetSolidCircleTex();
            Texture2D bubbleTex = TextureGenerator.GetBubbleTex();
            ParticleSystem splashPS = GetSplashParticles(target.transform, streamColor, solidTex, context);
            ParticleSystem bubblePS = GetBubbleParticles(target.transform, bubbleTex, target.Height, context);
            splashPS.Play();
            bubblePS.Play();

            while (elapsed < flowDuration)
            {
                elapsed += Time.deltaTime;
                float tValue = Mathf.Clamp01(elapsed / flowDuration);
                UpdateVisualPourProgress(source, target, sourceStart, targetStart, pouredLayer, tValue);
                StreamRenderer.Update(lr, source, target, sourceT, targetT, tValue, _config);

                float currentFill = target.VisualTotalFill;
                if (splashPS != null)
                {
                    splashPS.transform.position = targetT.position + Vector3.up * (target.Height * currentFill);
                }

                if (bubblePS != null)
                {
                    float liquidHeight = target.Height * currentFill;
                    var shape = bubblePS.shape;
                    shape.position = new Vector3(0f, liquidHeight * 0.5f, 0f);
                    shape.length = Mathf.Max(liquidHeight, 0.1f);
                }
                yield return null;
            }
            if (lr != null) lr.enabled = false;
            if (splashPS != null) { splashPS.Stop(); ReturnSplashToPool(splashPS, context, 1.0f); }
            if (bubblePS != null) { bubblePS.Stop(); ReturnBubbleToPool(bubblePS, context, 1.5f); }

            // Phase 3: Return & Settle
            elapsed = 0f;
            while (elapsed < returnDuration)
            {
                elapsed += Time.deltaTime;
                float tValue = Mathf.Clamp01(elapsed / returnDuration);
                float easedT = Mathf.SmoothStep(0f, 1f, tValue);
                sourceT.position = Vector3.Lerp(pourPos, startPos, easedT);
                sourceT.rotation = Quaternion.Slerp(tiltedRot, startRot, easedT);
                UpdateVisualPourProgress(source, target, sourceStart, targetStart, pouredLayer, 1f);
                yield return null;
            }
            sourceT.position = startPos;
            sourceT.rotation = startRot;

            source.UpdateVisualsFromState();
            target.UpdateVisualsFromState();
            target.PlaySettleBounce();

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

        private static float EaseOutBack(float x)
        {
            const float c1 = 1.3f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
        }
    }
}
