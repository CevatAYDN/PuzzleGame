using System;
using System.Collections.Generic;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Configuration;
using PuzzleGame.Domain.Interfaces;
using PuzzleGame.Domain.Models;
using PuzzleGame.Infrastructure;
using PuzzleGame.Infrastructure.Pool;
using UnityEngine;

namespace PuzzleGame.Application.Services
{
    /// <summary>
    /// Animation service. Tween logic delegated to ITweenService.
    /// No MonoBehaviour dependency, no coroutines in this layer.
    /// Particle pools use generic GameObjectPool<T>.
    /// </summary>
    public class AnimationService : IAnimationService
    {
        private readonly AnimationConfig _config;
        private readonly ITweenService _tween;
        private readonly IAudioService _audioService;
        private int _activeTweenCount;

        private const int MaxPoolSize = 16;

        private readonly IGameObjectPool<ParticleSystem> _splashPool;
        private readonly IGameObjectPool<ParticleSystem> _bubblePool;

        private static readonly int RimIntensityID = Shader.PropertyToID("_RimIntensity");

        public bool IsAnimating => _activeTweenCount > 0;

        public AnimationService(AnimationConfig config, ITweenService tween, IAudioService audioService)
        {
            _config = config;
            _tween = tween ?? throw new ArgumentNullException(nameof(tween));
            _audioService = audioService;

            var splashPrefab = CreateSplashParticlePrefab();
            var bubblePrefab = CreateBubbleParticlePrefab();

            _splashPool = PoolManager.Instance.RegisterPool<ParticleSystem>("SplashPool", splashPrefab, MaxPoolSize);
            _bubblePool = PoolManager.Instance.RegisterPool<ParticleSystem>("BubblePool", bubblePrefab, MaxPoolSize);
        }

        // ──────────────────────────────────────────────
        //  Public API
        // ──────────────────────────────────────────────

        public void AnimateBottleLift(Transform bottle,
                                      float height, float duration,
                                      Func<bool> keepHovering = null,
                                      Action onComplete = null)
        {
            Vector3 target = bottle.position + Vector3.up * height;
            IncrCount();
            _tween.TweenPosition(bottle, target, duration, EaseType.OutBack)
                .OnComplete(() =>
                {
                    DecrCount();
                    onComplete?.Invoke();
                });

            if (keepHovering != null && _config != null)
            {
                // Hover: re-call whenever selection state still matches
                Vector3 rest = target;
                // We don't start a separate tween — caller will trigger lower on deselect.
            }
        }

        public void AnimateBottleLower(Transform bottle,
                                       Vector3 originalPos, float duration,
                                       Action onComplete = null)
        {
            IncrCount();
            _tween.TweenPosition(bottle, originalPos, duration, EaseType.InOutSine)
                .OnComplete(() =>
                {
                    DecrCount();
                    onComplete?.Invoke();
                });
        }

        public void AnimatePour(IBottleView source, IBottleView target,
                                float duration, Action onComplete = null)
        {
            if (duration <= 0f) duration = 0.001f;

            Transform sourceT = source.Transform;
            Transform targetT = target.Transform;

            Vector3 toTarget = (targetT.position - sourceT.position).normalized;
            // 85 degrees is ideal for a natural liquid pour
            float tiltAngle = 85f;
            Vector3 tiltAxis = Vector3.Cross(Vector3.up, toTarget).normalized;
            if (tiltAxis.sqrMagnitude < 0.0001f) tiltAxis = Vector3.forward;

            Vector3 startPos = sourceT.position;
            Quaternion startRot = sourceT.rotation;
            Quaternion tiltedRot = Quaternion.AngleAxis(tiltAngle, tiltAxis) * startRot;

            Vector3 targetMouth = targetT.position + Vector3.up * (target.Height + 0.15f);
            Vector3 localSourceMouth = new Vector3(0f, source.Height, 0f);
            Vector3 startMouth = startPos + startRot * localSourceMouth;

            float tiltPortion = _config != null ? _config.tiltPhasePortion : 0.25f;
            float flowPortion = _config != null ? _config.flowPhasePortion : 0.50f;
            float returnPortion = _config != null ? _config.returnPhasePortion : 0.25f;

            float tiltDuration = duration * tiltPortion;
            float flowDuration = duration * flowPortion;
            float returnDuration = duration * returnPortion;

            var sourceStart = new LayerSnapshot(source.VisualLayers);
            var targetStart = new LayerSnapshot(target.VisualLayers);
            LiquidLayer pouredLayer = target.State.TopLayer ?? new LiquidLayer(new DomainColor(0, 0, 0, 0), 0f);

            LineRenderer lr = StreamRenderer.EnsureLineRenderer(source.GameObject);
            Color streamColor = ColorAdapter.ToUnity(pouredLayer.Color);
            StreamRenderer.SetColor(lr, streamColor);
            lr.enabled = false;

            // Capture for closure
            ParticleSystem splashPS = null;
            ParticleSystem bubblePS = null;

            IncrCount();

            // Phase 1: Tilt around the mouth pivot
            var tilt = _tween.TweenCustom(sourceT, 0f, 1f, tiltDuration, (tweenable, val) =>
            {
                float easedVal = Mathf.SmoothStep(0f, 1f, val);
                Quaternion currentRot = Quaternion.Slerp(startRot, tiltedRot, easedVal);
                Vector3 currentMouth = Vector3.Lerp(startMouth, targetMouth, easedVal);
                sourceT.rotation = currentRot;
                sourceT.position = currentMouth - currentRot * localSourceMouth;
            });

            tilt.OnComplete(() =>
            {
                // Phase 2: Flow (Custom Tween updating visuals, stream, and particles)
                lr.positionCount = StreamRenderer.TotalSegments;
                lr.enabled = true;

                _audioService?.PlaySfx(AudioClipId.PourLoop);

                splashPS = _splashPool.Rent(target.Transform);
                bubblePS = _bubblePool.Rent(target.Transform);
                if (splashPS != null)
                {
                    var splashMain = splashPS.main;
                    splashMain.startColor = streamColor;
                    splashPS.Play();
                }
                if (bubblePS != null) bubblePS.Play();

                var flowCustom = _tween.TweenCustom(sourceT, 0f, 1f, flowDuration, (tweenable, val) =>
                {
                    UpdateVisualPourProgress(source, target, sourceStart, targetStart, pouredLayer, val);
                    StreamRenderer.Update(lr, source, target, sourceT, targetT, val, _config);

                    float currentFill = target.VisualTotalFill;
                    if (splashPS != null)
                        splashPS.transform.position = targetT.position + Vector3.up * (target.Height * currentFill);

                    if (bubblePS != null)
                    {
                        float liquidHeight = target.Height * currentFill;
                        var shape = bubblePS.shape;
                        shape.position = new Vector3(0f, liquidHeight * 0.5f, 0f);
                        shape.length = Mathf.Max(liquidHeight, 0.1f);
                    }
                });

                flowCustom.OnComplete(() =>
                {
                    // Phase 3: Return to original position/rotation (around the mouth pivot)
                    lr.enabled = false;
                    if (splashPS != null) { splashPS.Stop(); DelayReturnToPool(splashPS, _splashPool, 1.0f); }
                    if (bubblePS != null) { bubblePS.Stop(); DelayReturnToPool(bubblePS, _bubblePool, 1.5f); }

                    _audioService?.PlaySfx(AudioClipId.PourEnd);

                    var returnCustom = _tween.TweenCustom(sourceT, 0f, 1f, returnDuration, (tweenable, val) =>
                    {
                        float easedVal = Mathf.SmoothStep(0f, 1f, val);
                        Quaternion currentRot = Quaternion.Slerp(tiltedRot, startRot, easedVal);
                        Vector3 currentMouth = Vector3.Lerp(targetMouth, startMouth, easedVal);
                        sourceT.rotation = currentRot;
                        sourceT.position = currentMouth - currentRot * localSourceMouth;
                    });

                    returnCustom.OnComplete(() =>
                    {
                        source.UpdateVisualsFromState();
                        target.UpdateVisualsFromState();
                        target.PlaySettleBounce();
                        DecrCount();
                        onComplete?.Invoke();
                    });
                });
            });
        }

        public void AnimateErrorShake(Transform bottle, Action onComplete = null)
        {
            float duration = _config != null ? _config.shakeDuration : 0.25f;
            float angle = _config != null ? _config.shakeAngle : 8f;
            Vector3 startRot = bottle.rotation.eulerAngles;

            IncrCount();
            _tween.TweenShakeRotation(bottle, duration, Vector3.forward * angle, 4)
                .OnComplete(() =>
                {
                    bottle.rotation = Quaternion.Euler(startRot);
                    DecrCount();
                    onComplete?.Invoke();
                });
        }

        public void AnimateCorkDrop(Transform cork, float bottleHeight, Action onComplete = null)
        {
            Vector3 startPos = new Vector3(0f, bottleHeight + 0.3f, 0f);
            Vector3 endPos = new Vector3(0f, bottleHeight - 0.05f, 0f);
            const float duration = 0.5f;

            cork.localPosition = startPos;
            cork.localScale = Vector3.zero;

            _audioService?.PlaySfx(AudioClipId.CorkPop, cork.position);

            IncrCount();
            var t1 = _tween.TweenLocalPosition(cork, endPos, duration, EaseType.OutBounce);
            var t2 = _tween.TweenScale(cork, Vector3.one, duration, EaseType.OutBounce);
            
            t1.OnComplete(() =>
            {
                cork.localPosition = endPos;
                cork.localScale = Vector3.one;
                DecrCount();
                onComplete?.Invoke();
            });
        }

        public void AnimateLiquidFlash(Renderer renderer, int materialSlot,
                                       float peakIntensity, float duration,
                                       Action onComplete = null)
        {
            if (renderer == null) { onComplete?.Invoke(); return; }
            var propBlock = new MaterialPropertyBlock();
            float flashDuration = duration;

            IncrCount();
            var t1 = _tween.TweenCustom(renderer, 0.5f, peakIntensity, flashDuration * 0.2f,
                (t, val) => SetRimIntensity(renderer, materialSlot, val, propBlock));
            
            t1.OnComplete(() =>
            {
                var t2 = _tween.TweenCustom(renderer, peakIntensity, 0.5f, flashDuration * 0.8f,
                    (t, val) => SetRimIntensity(renderer, materialSlot, val, propBlock));
                
                t2.OnComplete(() =>
                {
                    SetRimIntensity(renderer, materialSlot, 0.5f, propBlock);
                    DecrCount();
                    onComplete?.Invoke();
                });
            });
        }

        public void AnimateSettleBounce(IBottleView bottle, float duration,
                                        Action onComplete = null)
        {
            float originalFill = bottle.VisualTotalFill;
            IncrCount();
            _tween.TweenCustom(bottle, 0f, 1f, duration, (tweenable, progress) =>
            {
                float wave = Mathf.Cos(progress * Mathf.PI * 3f) * 0.04f * (1f - progress);
                bottle.SetVisualState(new List<LiquidLayer>(bottle.State.Layers), originalFill + wave);
            })
            .OnComplete(() =>
            {
                bottle.SetVisualState(new List<LiquidLayer>(bottle.State.Layers), originalFill);
                DecrCount();
                onComplete?.Invoke();
            });
        }

        // ──────────────────────────────────────────────
        //  Private helpers
        // ──────────────────────────────────────────────

        private void IncrCount() => _activeTweenCount++;
        private void DecrCount() => _activeTweenCount = Mathf.Max(0, _activeTweenCount - 1);

        private static void SetRimIntensity(Renderer renderer, int materialSlot, float val, MaterialPropertyBlock propBlock)
        {
            if (renderer == null || propBlock == null) return;
            if (renderer.sharedMaterials.Length <= materialSlot) return;
            renderer.GetPropertyBlock(propBlock, materialSlot);
            propBlock.SetFloat(RimIntensityID, val);
            renderer.SetPropertyBlock(propBlock, materialSlot);
        }

        private void UpdateVisualPourProgress(IBottleView source, IBottleView target,
                                               LayerSnapshot sourceStart, LayerSnapshot targetStart,
                                               LiquidLayer pouredLayer, float t)
        {
            t = Mathf.Clamp01(t);
            source.SetVisualPourProgress(sourceStart, t, true, pouredLayer);
            target.SetVisualPourProgress(targetStart, t, false, pouredLayer);
        }

        private void DelayReturnToPool(ParticleSystem ps, IGameObjectPool<ParticleSystem> pool, float delay)
        {
            _tween.Delay(delay)
                .OnComplete(() =>
                {
                    if (ps != null)
                    {
                        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                        pool.Return(ps);
                    }
                });
        }

        private static ParticleSystem CreateSplashParticlePrefab()
        {
            var go = new GameObject("SplashParticle_Prefab", typeof(ParticleSystem));
            go.SetActive(false);
            var ps = go.GetComponent<ParticleSystem>();
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
            var curve = new AnimationCurve();
            curve.AddKey(0f, 1f); curve.AddKey(0.7f, 0.8f); curve.AddKey(1f, 0f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sharedMaterial = ParticleMaterialFactory.GetSplashMaterial(TextureGenerator.GetSolidCircleTex());

            return ps;
        }

        private static ParticleSystem CreateBubbleParticlePrefab()
        {
            var go = new GameObject("BubbleParticle_Prefab", typeof(ParticleSystem));
            go.SetActive(false);
            var ps = go.GetComponent<ParticleSystem>();
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
            var curve = new AnimationCurve();
            curve.AddKey(0f, 0.2f); curve.AddKey(0.2f, 1.0f);
            curve.AddKey(0.8f, 1.0f); curve.AddKey(1f, 0f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sharedMaterial = ParticleMaterialFactory.GetBubbleMaterial(TextureGenerator.GetBubbleTex());

            return ps;
        }
    }
}
