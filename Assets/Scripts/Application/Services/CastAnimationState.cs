using System;
using UnityEngine;
using UnityEngine.VFX;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Configuration;
using PuzzleGame.Domain.Interfaces;
using PuzzleGame.Domain.Models;
// IGameObjectPool now in PuzzleGame.Application.Interfaces

namespace PuzzleGame.Application.Services
{
    /// <summary>
    /// Per-Cast animation state object. Eliminates heap-allocated lambda closures
    /// by storing all captured state in a reusable object. Pooled by AnimationService.
    /// </summary>
    internal sealed class CastAnimationState
    {
        // ── Source/Target ──────────────────────────────────────────────────────
        public IMoldView Source;
        public IMoldView Target;
        public Transform SourceT;
        public Transform TargetT;

        // ── Transform state ────────────────────────────────────────────────────
        public Vector3 StartPos;
        public Quaternion StartRot;
        public Quaternion TiltedRot;
        public Vector3 LocalSourceMouth;
        public Vector3 StartMouth;
        public Vector3 TargetMouth;

        // ── Visual snapshots ───────────────────────────────────────────────────
        public LayerSnapshot SourceStart;
        public LayerSnapshot TargetStart;
        public OreLayer CastedLayer;

        // ── Duration configuration ─────────────────────────────────────────────
        public float TiltDuration;
        public float FlowDuration;
        public float ReturnDuration;

        // ── Stream / Particles ─────────────────────────────────────────────────
        public VisualEffect Effect;
        public IStreamRenderer StreamRenderer;
        public IStreamTrailController TrailController;
        public Color StreamColor;
        public ParticleSystem SplashPS;
        public ParticleSystem BubblePS;

        // ── Services ───────────────────────────────────────────────────────────
        public AnimationConfig Config;
        public ITweenService TweenService;
        public IAudioService AudioService;
        public IGameObjectPool<ParticleSystem> SplashPool;
        public IGameObjectPool<ParticleSystem> BubblePool;
        public AnimationService Owner;

        // ── Completion ─────────────────────────────────────────────────────────
        public Action OnComplete;

        // ── Cached Action Delegates (Allocated once per instance to avoid GC) ──
        public readonly Action OnTiltCompleteCached;
        public readonly Action OnFlowCompleteCached;
        public readonly Action OnReturnCompleteCached;

        public CastAnimationState()
        {
            OnTiltCompleteCached = OnTiltComplete;
            OnFlowCompleteCached = OnFlowComplete;
            OnReturnCompleteCached = OnReturnComplete;
        }

        private void OnTiltComplete()
        {
            // Phase 2: Flow
            if (Effect != null) Effect.Play();

            AudioService?.PlaySfx(AudioClipId.CastLoop);

            // Begin trail from source mouth
            Vector3 sourceMouth = SourceT.TransformPoint(LocalSourceMouth);
            TrailController?.BeginTrail(sourceMouth, StreamColor);

            SplashPS = SplashPool.Rent(TargetT);
            BubblePS = BubblePool.Rent(TargetT);
            if (SplashPS != null)
            {
                var splashMain = SplashPS.main;
                splashMain.startColor = StreamColor;
                SplashPS.Play();
            }
            if (BubblePS != null) BubblePS.Play();

            var flowCustom = TweenService.TweenCustom(this, 0f, 1f, FlowDuration, FlowUpdate);
            Owner.RegisterTween(flowCustom);
            flowCustom.OnComplete(OnFlowCompleteCached);
        }

        private void OnFlowComplete()
        {
            // Phase 3: Return
            if (Effect != null) Effect.Stop();
            if (TrailController != null)
            {
                TrailController.EndTrail();
                float maxAlpha = TrailController.TrailAlpha;
                float fadeDuration = TrailController.TrailFadeDuration;
                var fadeTween = TweenService.TweenCustom(TrailController, maxAlpha, 0f, fadeDuration, 
                    (trail, val) => ((IStreamTrailController)trail).SetAlpha(val));
                Owner.RegisterTween(fadeTween);
            }
            if (SplashPS != null) { SplashPS.Stop(); Owner.DelayReturnToPool(SplashPS, SplashPool, 1.0f); }
            if (BubblePS != null) { BubblePS.Stop(); Owner.DelayReturnToPool(BubblePS, BubblePool, 1.5f); }

            AudioService?.PlaySfx(AudioClipId.CastEnd);

            var returnCustom = TweenService.TweenCustom(this, 0f, 1f, ReturnDuration, ReturnUpdate);
            Owner.RegisterTween(returnCustom);
            returnCustom.OnComplete(OnReturnCompleteCached);
        }

        private void OnReturnComplete()
        {
            Source.UpdateVisualsFromState();
            Target.UpdateVisualsFromState();
            Target.PlaySettleBounce();
            Owner.DecrCount();

            var cb = OnComplete;
            Owner.ReturnCastState(this);
            cb?.Invoke();
        }

        // ── Static Update Delegates (cached to avoid lambda allocations) ────────

        public static readonly Action<object, float> TiltUpdate = OnTiltUpdate;
        public static readonly Action<object, float> FlowUpdate = OnFlowUpdate;
        public static readonly Action<object, float> ReturnUpdate = OnReturnUpdate;

        private static void OnTiltUpdate(object target, float val)
        {
            var s = (CastAnimationState)target;
            float easedVal = Mathf.SmoothStep(0f, 1f, val);
            Quaternion currentRot = Quaternion.Slerp(s.StartRot, s.TiltedRot, easedVal);
            Vector3 currentMouth = Vector3.Lerp(s.StartMouth, s.TargetMouth, easedVal);
            s.SourceT.rotation = currentRot;
            s.SourceT.position = currentMouth - currentRot * s.LocalSourceMouth;
        }

        private static void OnFlowUpdate(object target, float val)
        {
            var s = (CastAnimationState)target;
            s.Owner.UpdateVisualCastProgress(s.Source, s.Target,
                s.SourceStart, s.TargetStart, s.CastedLayer, val);
            s.StreamRenderer.Update(s.Effect, s.Source, s.Target,
                s.SourceT, s.TargetT, val, s.Config);

            // Update trail — trails source mouth position during flow
            Vector3 sourceMouthWS = s.SourceT.TransformPoint(s.LocalSourceMouth);
            s.TrailController?.UpdateTrail(sourceMouthWS);

            float currentFill = s.Target.VisualTotalFill;
            if (s.SplashPS != null)
                s.SplashPS.transform.position = s.TargetT.position + Vector3.up * (s.Target.Height * currentFill);

            if (s.BubblePS != null)
            {
                float OreHeight = s.Target.Height * currentFill;
                var shape = s.BubblePS.shape;
                shape.position = new Vector3(0f, OreHeight * 0.5f, 0f);
                shape.length = Mathf.Max(OreHeight, 0.1f);
            }
        }

        private static void OnReturnUpdate(object target, float val)
        {
            var s = (CastAnimationState)target;
            float easedVal = Mathf.SmoothStep(0f, 1f, val);
            Quaternion currentRot = Quaternion.Slerp(s.TiltedRot, s.StartRot, easedVal);
            Vector3 currentMouth = Vector3.Lerp(s.TargetMouth, s.StartMouth, easedVal);
            s.SourceT.rotation = currentRot;
            s.SourceT.position = currentMouth - currentRot * s.LocalSourceMouth;
        }

        public void Clear()
        {
            Source = null;
            Target = null;
            SourceT = null;
            TargetT = null;
            Effect = null;
            StreamRenderer = null;
            TrailController?.Cleanup();
            TrailController = null;
            SplashPS = null;
            BubblePS = null;
            TweenService = null;
            AudioService = null;
            SplashPool = null;
            BubblePool = null;
            Owner = null;
            OnComplete = null;
            Config = null;
        }
    }
}
