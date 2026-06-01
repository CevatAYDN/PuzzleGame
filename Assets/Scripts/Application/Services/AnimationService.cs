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

        private static Texture2D _solidCircleTex;
        private static Texture2D _bubbleTex;

        private static Texture2D GetSolidCircleTex()
        {
            if (_solidCircleTex == null)
                _solidCircleTex = CreateCircleTexture(true);
            return _solidCircleTex;
        }

        private static Texture2D GetBubbleTex()
        {
            if (_bubbleTex == null)
                _bubbleTex = CreateCircleTexture(false);
            return _bubbleTex;
        }

        private static Texture2D CreateCircleTexture(bool solid)
        {
            int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            float center = size / 2f;
            float radius = size * 0.45f;
            float innerRadius = size * 0.33f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    if (solid)
                    {
                        float alpha = Mathf.Clamp01((radius - dist) / 1.5f);
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                    }
                    else
                    {
                        float edgeAlpha = Mathf.Clamp01((radius - dist) / 1.5f);
                        float innerAlpha = Mathf.Clamp01((dist - innerRadius) / 1.5f);
                        float alpha = Mathf.Min(edgeAlpha, innerAlpha);

                        float hDx = x - (center - radius * 0.3f);
                        float hDy = y - (center + radius * 0.3f);
                        float hDist = Mathf.Sqrt(hDx * hDx + hDy * hDy);
                        float highlight = Mathf.Clamp01((radius * 0.15f - hDist) / 1.0f) * 0.8f;

                        float inside = (dist < innerRadius) ? 0.05f * (dist / innerRadius) : 0f;

                        float finalAlpha = Mathf.Max(alpha * 0.8f, highlight, inside);
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, finalAlpha));
                    }
                }
            }
            tex.Apply();
            return tex;
        }

        private ParticleSystem CreateSplashParticles(Transform parent, Color color, Texture2D tex)
        {
            GameObject go = new GameObject("PourSplashParticles");
            go.transform.SetParent(parent, false);

            ParticleSystem ps = go.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.duration = 1f;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.45f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
            main.gravityModifier = new ParticleSystem.MinMaxCurve(2.0f);
            main.startColor = color;
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

            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Sprites/Default");
            Material mat = new Material(shader);
            if (tex != null)
            {
                mat.mainTexture = tex;
                if (mat.HasProperty("_BaseMap"))
                    mat.SetTexture("_BaseMap", tex);
            }
            mat.color = Color.white;
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", Color.white);
            renderer.sharedMaterial = mat;

            return ps;
        }

        private ParticleSystem CreateBubbleParticles(Transform parent, Texture2D tex, float bottleHeight)
        {
            GameObject go = new GameObject("PourBubbleParticles");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;

            ParticleSystem ps = go.AddComponent<ParticleSystem>();

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

            var velocityOverLifetime = ps.velocityOverLifetime;
            velocityOverLifetime.enabled = true;
            velocityOverLifetime.space = ParticleSystemSimulationSpace.Local;

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

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(1f, 0.8f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(grad);

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;

            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Sprites/Default");
            Material mat = new Material(shader);
            if (tex != null)
            {
                mat.mainTexture = tex;
                if (mat.HasProperty("_BaseMap"))
                    mat.SetTexture("_BaseMap", tex);
            }
            mat.color = Color.white;
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", Color.white);
            renderer.sharedMaterial = mat;

            return ps;
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

            float tiltPortion   = _config != null ? _config.tiltPhasePortion : 0.25f;
            float flowPortion   = _config != null ? _config.flowPhasePortion : 0.50f;
            float returnPortion = _config != null ? _config.returnPhasePortion : 0.25f;

            float tiltDuration   = duration * tiltPortion;
            float flowDuration   = duration * flowPortion;
            float returnDuration = duration * returnPortion;
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

            lr.enabled = false; // Hidden during travel

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
            lr.enabled = true;
            elapsed = 0f;

            Texture2D solidTex = GetSolidCircleTex();
            Texture2D bubbleTex = GetBubbleTex();

            ParticleSystem splashPS = CreateSplashParticles(target.transform, streamColor, solidTex);
            ParticleSystem bubblePS = CreateBubbleParticles(target.transform, bubbleTex, target.Height);

            splashPS.Play();
            bubblePS.Play();

            while (elapsed < flowDuration)
            {
                elapsed += Time.deltaTime;
                float tValue = Mathf.Clamp01(elapsed / flowDuration);

                UpdateVisualPourProgress(source, target, sourceStart, targetStart, pouredLayer, tValue);
                UpdateStream(lr, source, target, sourceT, targetT, tValue);

                float currentFill = target.VisualTotalFill;
                Vector3 landingPoint = targetT.position + Vector3.up * (target.Height * currentFill);
                if (splashPS != null)
                {
                    splashPS.transform.position = landingPoint;
                }

                if (bubblePS != null)
                {
                    var shape = bubblePS.shape;
                    float liquidHeight = target.Height * currentFill;
                    shape.position = new Vector3(0f, liquidHeight * 0.5f, 0f);
                    shape.length = Mathf.Max(liquidHeight, 0.1f);
                }

                yield return null;
            }
            if (lr != null) lr.enabled = false;

            if (splashPS != null)
            {
                splashPS.Stop();
                GameObject.Destroy(splashPS.gameObject, 1.0f);
            }
            if (bubblePS != null)
            {
                bubblePS.Stop();
                GameObject.Destroy(bubblePS.gameObject, 1.5f);
            }

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
            float scaleFactor = Mathf.SmoothStep(0f, 1f, t < 0.1f ? t / 0.1f : (t > 0.9f ? (1f - t) / 0.1f : 1f));
            float baseWidth = _config != null ? _config.streamWidth * scaleFactor : 0.08f * scaleFactor;

            // Source mouth offset rotated in world space
            Vector3 sourceMouth = sourceT.TransformPoint(new Vector3(0f, source.Height, 0f));
            // Target mouth straight offset in world space
            Vector3 targetMouth = targetT.position + Vector3.up * target.Height;
            // Landing point at the liquid surface in the target bottle
            Vector3 landingPoint = targetT.position + Vector3.up * (target.Height * target.VisualTotalFill);

            // We divide the LineRenderer into two parts:
            // 1. Curve from sourceMouth to targetMouth (external flow)
            // 2. Straight line from targetMouth to landingPoint (internal flow)
            int externalSegments = 12;
            int internalSegments = 6;
            int totalPoints = externalSegments + internalSegments;
            lr.positionCount = totalPoints;

            // Set up a flat width curve for clean vector look
            var widthCurve = new AnimationCurve();
            widthCurve.AddKey(0f, baseWidth);
            widthCurve.AddKey(1f, baseWidth);
            lr.widthCurve = widthCurve;

            // 1. External curve
            for (int i = 0; i < externalSegments; i++)
            {
                float segT = (float)i / (externalSegments - 1);
                
                // Parabolic gravity drop (curves downward in world space)
                float distH = Vector3.Distance(new Vector3(sourceMouth.x, 0f, sourceMouth.z), new Vector3(targetMouth.x, 0f, targetMouth.z));
                float gravityArc = Mathf.Sin(segT * Mathf.PI) * (0.05f + distH * 0.12f);
                
                Vector3 pos = Vector3.Lerp(sourceMouth, targetMouth, segT);
                pos.y -= gravityArc;
                lr.SetPosition(i, pos);
            }

            // 2. Internal straight vertical line
            for (int i = 0; i < internalSegments; i++)
            {
                float segT = (float)i / (internalSegments - 1);
                Vector3 pos = Vector3.Lerp(targetMouth, landingPoint, segT);
                lr.SetPosition(externalSegments + i, pos);
            }
        }

        private static float EaseOutBack(float x)
        {
            const float c1 = 1.3f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
        }
    }
}