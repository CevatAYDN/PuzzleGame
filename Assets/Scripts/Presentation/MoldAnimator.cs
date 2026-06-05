using PuzzleGame.Application.Interfaces;
using UnityEngine;

namespace PuzzleGame
{
    /// <summary>
    /// Animation trigger facade for a Mold. Owns completion flash, settle bounce, wobble impulse.
    /// Extracted from MoldController for SRP (single responsibility = animation triggers).
    /// Cork drop is delegated to MoldCorkController (still owned by the controller for now).
    /// </summary>
    public sealed class MoldAnimator
    {
        private readonly Renderer _renderer;
        private readonly IAnimationService _animationService;
        private readonly PuzzleGame.Wobble _wobble;
        private readonly PuzzleGame.Application.Configuration.MoldVisualConfig _visualConfig;
        private readonly PuzzleGame.MoldCorkController _corkController;

        public MoldAnimator(
            Renderer renderer,
            IAnimationService animationService,
            PuzzleGame.Wobble wobble,
            PuzzleGame.Application.Configuration.MoldVisualConfig visualConfig,
            PuzzleGame.MoldCorkController corkController)
        {
            _renderer = renderer;
            _animationService = animationService;
            _wobble = wobble;
            _visualConfig = visualConfig;
            _corkController = corkController;
        }

        public void AnimateCompletion()
        {
            if (_corkController != null && _corkController.IsCapped) return;
            _corkController?.AnimateDrop();

            int oreIndex = _visualConfig != null ? _visualConfig.oreMaterialIndex : 1;
            if (_renderer != null && _renderer.sharedMaterials.Length > oreIndex && _animationService != null)
            {
                float intensity = _visualConfig != null ? _visualConfig.completionFlashIntensity : 4.0f;
                float duration = _visualConfig != null ? _visualConfig.completionFlashDuration : 0.6f;
                _animationService.AnimateOreFlash(
                    _renderer, oreIndex,
                    intensity,
                    duration,
                    onComplete: null);
            }
        }

        public void PlaySettleBounce(IMoldView host, float? overrideDuration = null)
        {
            if (_animationService == null) return;
            float duration = overrideDuration
                ?? (_visualConfig != null ? _visualConfig.settleBounceDuration : 0.6f);
            _animationService.AnimateSettleBounce(host, duration, onComplete: null);
        }

        public void AddWobbleImpulse(Vector3 direction, float strength)
        {
            _wobble?.AddImpulse(direction, strength);
        }
    }
}
