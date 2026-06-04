using System;
using System.Collections.Generic;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Configuration;
using PuzzleGame.Domain.Interfaces;
using PuzzleGame.Domain.Models;
// Infrastructure dependencies now through interfaces (IColorAdapter, IPoolManager)
using UnityEngine;
using PuzzleGame.Application.Logging;

namespace PuzzleGame.Application.Services
{
    /// <summary>
    /// Animation service. Tween logic delegated to ITweenService.
    /// No MonoBehaviour dependency, no coroutines in this layer.
    /// Particle pools use generic GameObjectPool&lt;T&gt;.
    /// Particle prefab creation delegated to ParticlePrefabFactory.
    /// </summary>
    public class AnimationService : IAnimationService, System.IDisposable
    {
        private readonly AnimationConfig _config;
        private readonly ITweenService _tween;
        private readonly IAudioService _audioService;
        private int _activeTweenCount;

        private const int MaxPoolSize = 32;

        private readonly IGameObjectPool<ParticleSystem> _splashPool;
        private readonly IGameObjectPool<ParticleSystem> _bubblePool;

        private static readonly int RimIntensityID = Shader.PropertyToID("_RimIntensity");

        private readonly IPoolManager _poolManager;
        private readonly IColorAdapter _colorAdapter;
        private readonly IParticleFactory _particleFactory;
        private readonly IStreamRenderer _streamRenderer;
        private readonly IStreamTrailController _trailController;

        private readonly ParticleSystem _splashPrefab;
        private readonly ParticleSystem _bubblePrefab;
        // Fix #12: HashSet instead of List — Remove() is O(1) instead of O(n).
        private readonly HashSet<ITweenHandle> _activeTweens = new HashSet<ITweenHandle>();
        private readonly Stack<CastAnimationState> _CastStatePool = new Stack<CastAnimationState>();

        public bool IsAnimating => _activeTweenCount > 0;

        public AnimationService(AnimationConfig config, ITweenService tween, IAudioService audioService, IPoolManager poolManager, IColorAdapter colorAdapter, IParticleFactory particleFactory, IStreamRenderer streamRenderer, IStreamTrailController trailController)
        {
            _config = config;
            _tween = tween ?? throw new ArgumentNullException(nameof(tween));
            _audioService = audioService;
            _poolManager = poolManager;
            _colorAdapter = colorAdapter;
            _particleFactory = particleFactory ?? throw new ArgumentNullException(nameof(particleFactory));
            _streamRenderer = streamRenderer ?? throw new ArgumentNullException(nameof(streamRenderer));
            _trailController = trailController;

            _splashPrefab = _particleFactory.CreateSplash();
            _bubblePrefab = _particleFactory.CreateBubble();

            _splashPool = _poolManager.RegisterPool<ParticleSystem>("SplashPool", _splashPrefab, MaxPoolSize);
            _bubblePool = _poolManager.RegisterPool<ParticleSystem>("BubblePool", _bubblePrefab, MaxPoolSize);
        }

        internal void RegisterTween(ITweenHandle handle)
        {
            if (handle == null) return;
            _activeTweens.Add(handle); // HashSet.Add is O(1)
            handle.OnComplete(() => _activeTweens.Remove(handle)); // HashSet.Remove is O(1)
        }

        internal CastAnimationState RentCastState()
        {
            if (_CastStatePool.Count > 0)
            {
                return _CastStatePool.Pop();
            }
            return new CastAnimationState();
        }

        internal void ReturnCastState(CastAnimationState state)
        {
            state.Clear();
            _CastStatePool.Push(state);
        }

        public void Dispose()
        {
            foreach (var tween in _activeTweens)
            {
                try { tween?.Kill(); }
                catch (Exception ex) { MoldLogger.LogDebug($"Error killing tween on dispose: {ex.Message}"); }
            }
            _activeTweens.Clear();

            if (_splashPrefab != null)
            {
                UnityEngine.Object.Destroy(_splashPrefab.gameObject);
            }
            if (_bubblePrefab != null)
            {
                UnityEngine.Object.Destroy(_bubblePrefab.gameObject);
            }

            _poolManager.RemovePool<ParticleSystem>("SplashPool");
            _poolManager.RemovePool<ParticleSystem>("BubblePool");

        }

        // ──────────────────────────────────────────────
        //  Public API
        // ──────────────────────────────────────────────

        public void AnimateMoldLift(Transform Mold,
                                       float height, float duration,
                                       Func<bool> keepHovering = null,
                                       Action onComplete = null)
        {
            Vector3 target = Mold.position + Vector3.up * height;
            IncrCount();
            var t = _tween.TweenPosition(Mold, target, duration, EaseType.OutBack)
                .OnComplete(() =>
                {
                    DecrCount();
                    onComplete?.Invoke();
                });
            RegisterTween(t);

            if (keepHovering != null && _config != null)
            {
                // Hover: re-call whenever selection state still matches
                Vector3 rest = target;
                // We don't start a separate tween — caller will trigger lower on deselect.
            }
        }

        public void AnimateMoldLower(Transform Mold,
                                       Vector3 originalPos, float duration,
                                       Action onComplete = null)
        {
            IncrCount();
            var t = _tween.TweenPosition(Mold, originalPos, duration, EaseType.InOutSine)
                .OnComplete(() =>
                {
                    DecrCount();
                    onComplete?.Invoke();
                });
            RegisterTween(t);
        }

        public void AnimateCast(IMoldView source, IMoldView target,
                                float duration, Action onComplete = null)
        {
            if (duration <= 0f)
            {
                MoldLogger.LogWarning("AnimateCast called with duration <= 0f. Falling back to 0.001f.");
                duration = 0.001f;
            }

            CastAnimationState state = RentCastState();
            state.Source = source;
            state.Target = target;
            state.SourceT = source.Transform;
            state.TargetT = target.Transform;

            Vector3 toTarget = (state.TargetT.position - state.SourceT.position).normalized;
            float tiltAngle = 85f;
            Vector3 tiltAxis = Vector3.Cross(Vector3.up, toTarget).normalized;
            if (tiltAxis.sqrMagnitude < 0.0001f) tiltAxis = Vector3.forward;

            state.StartPos = state.SourceT.position;
            state.StartRot = state.SourceT.rotation;
            state.TiltedRot = Quaternion.AngleAxis(tiltAngle, tiltAxis) * state.StartRot;

            state.TargetMouth = state.TargetT.position + Vector3.up * (target.Height + 0.15f);
            state.LocalSourceMouth = new Vector3(0f, source.Height, 0f);
            state.StartMouth = state.StartPos + state.StartRot * state.LocalSourceMouth;

            float tiltPortion = _config != null ? _config.tiltPhasePortion : 0.25f;
            float flowPortion = _config != null ? _config.flowPhasePortion : 0.50f;
            float returnPortion = _config != null ? _config.returnPhasePortion : 0.25f;

            state.TiltDuration = duration * tiltPortion;
            state.FlowDuration = duration * flowPortion;
            state.ReturnDuration = duration * returnPortion;

            state.SourceStart = new LayerSnapshot(source.VisualLayers);
            state.TargetStart = new LayerSnapshot(target.VisualLayers);
            state.CastedLayer = target.State.TopLayer ?? new OreLayer(new DomainColor(0, 0, 0, 0), 0f);

            state.Effect = _streamRenderer.EnsureEffect(source.GameObject);
            state.StreamColor = _colorAdapter.ToUnity(state.CastedLayer.Color);
            _streamRenderer.SetColor(state.Effect, state.StreamColor);
            if (state.Effect != null) state.Effect.Stop();

            state.Config = _config;
            state.TweenService = _tween;
            state.AudioService = _audioService;
            state.StreamRenderer = _streamRenderer;
            state.TrailController = _trailController;
            state.SplashPool = _splashPool;
            state.BubblePool = _bubblePool;
            state.Owner = this;
            state.OnComplete = onComplete;

            IncrCount();

            var tilt = _tween.TweenCustom(state, 0f, 1f, state.TiltDuration, CastAnimationState.TiltUpdate);
            RegisterTween(tilt);
            tilt.OnComplete(state.OnTiltCompleteCached);
        }

        public void AnimateErrorShake(Transform Mold, Action onComplete = null)
        {
            float duration = _config != null ? _config.shakeDuration : 0.25f;
            float angle = _config != null ? _config.shakeAngle : 8f;
            Vector3 startRot = Mold.rotation.eulerAngles;

            IncrCount();
            var t = _tween.TweenShakeRotation(Mold, duration, Vector3.forward * angle, 4)
                .OnComplete(() =>
                {
                    Mold.rotation = Quaternion.Euler(startRot);
                    DecrCount();
                    onComplete?.Invoke();
                });
            RegisterTween(t);
        }

        public void AnimateCorkDrop(Transform cork, float MoldHeight, Action onComplete = null)
        {
            Vector3 startPos = new Vector3(0f, MoldHeight + 0.3f, 0f);
            Vector3 endPos = new Vector3(0f, MoldHeight - 0.05f, 0f);
            const float duration = 0.5f;

            cork.localPosition = startPos;
            cork.localScale = Vector3.zero;

            _audioService?.PlaySfx(AudioClipId.CorkPop, cork.position);

            IncrCount();
            var t1 = _tween.TweenLocalPosition(cork, endPos, duration, EaseType.OutBounce);
            var t2 = _tween.TweenScale(cork, Vector3.one, duration, EaseType.OutBounce);
            RegisterTween(t1);
            RegisterTween(t2);

            t1.OnComplete(() =>
            {
                cork.localPosition = endPos;
                cork.localScale = Vector3.one;
                DecrCount();
                onComplete?.Invoke();
            });
        }

        public void AnimateOreFlash(Renderer renderer, int materialSlot,
                                       float peakIntensity, float duration,
                                       Action onComplete = null)
        {
            if (renderer == null) { onComplete?.Invoke(); return; }
            var propBlock = new MaterialPropertyBlock();
            float flashDuration = duration;

            IncrCount();
            var t1 = _tween.TweenCustom(renderer, 0.5f, peakIntensity, flashDuration * 0.2f,
                (t, val) => SetRimIntensity(renderer, materialSlot, val, propBlock));
            RegisterTween(t1);

            t1.OnComplete(() =>
            {
                var t2 = _tween.TweenCustom(renderer, peakIntensity, 0.5f, flashDuration * 0.8f,
                    (t, val) => SetRimIntensity(renderer, materialSlot, val, propBlock));
                RegisterTween(t2);

                t2.OnComplete(() =>
                {
                    SetRimIntensity(renderer, materialSlot, 0.5f, propBlock);
                    DecrCount();
                    onComplete?.Invoke();
                });
            });
        }

        public void AnimateSettleBounce(IMoldView Mold, float duration,
                                        Action onComplete = null)
        {
            float originalFill = Mold.VisualTotalFill;
            IncrCount();
            var t = _tween.TweenCustom(Mold, 0f, 1f, duration, (tweenable, progress) =>
            {
                float wave = Mathf.Cos(progress * Mathf.PI * 3f) * 0.04f * (1f - progress);
                Mold.SetVisualState(Mold.State.Layers, originalFill + wave);
            })
            .OnComplete(() =>
            {
                Mold.SetVisualState(Mold.State.Layers, originalFill);
                DecrCount();
                onComplete?.Invoke();
            });
            RegisterTween(t);
        }

        // ──────────────────────────────────────────────
        //  Private helpers
        // ──────────────────────────────────────────────

        internal void IncrCount() => _activeTweenCount++;
        internal void DecrCount() => _activeTweenCount = Mathf.Max(0, _activeTweenCount - 1);

        private static void SetRimIntensity(Renderer renderer, int materialSlot, float val, MaterialPropertyBlock propBlock)
        {
            if (renderer == null || propBlock == null) return;
            if (renderer.sharedMaterials.Length <= materialSlot) return;
            renderer.GetPropertyBlock(propBlock, materialSlot);
            propBlock.SetFloat(RimIntensityID, val);
            renderer.SetPropertyBlock(propBlock, materialSlot);
        }

        internal void UpdateVisualCastProgress(IMoldView source, IMoldView target,
                                               LayerSnapshot sourceStart, LayerSnapshot targetStart,
                                               OreLayer CastedLayer, float t)
        {
            t = Mathf.Clamp01(t);
            source.SetVisualCastProgress(sourceStart, t, true, CastedLayer);
            target.SetVisualCastProgress(targetStart, t, false, CastedLayer);
        }

        internal void DelayReturnToPool(ParticleSystem ps, IGameObjectPool<ParticleSystem> pool, float delay)
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
    }
}
