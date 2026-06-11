using System;
using System.Collections.Generic;
using PuzzleGame.Application.Configuration;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Logging;
using PuzzleGame.Domain.Models;
using UnityEngine;

namespace PuzzleGame.Application.Services
{
    public sealed class MultiPourService : IMultiPourService
    {
        private const string LogTag = "[MultiPourService]";

        private readonly ICastService _castService;
        private readonly IAnimationService _animationService;
        private readonly IMoldSelectionService _selectionService;
        private readonly IAudioService _audioService;
        private readonly IMoldLookupCache _lookup;
        private readonly IInputHandlerDefaults _defaults;
        private readonly IActiveMoldsProvider _moldsProvider;
        private readonly AnimationConfig _animConfig;
        private LevelData _currentLevelData;

        public MultiPourService(
            ICastService castService,
            IAnimationService animationService,
            IMoldSelectionService selectionService,
            IAudioService audioService,
            IMoldLookupCache lookup,
            IInputHandlerDefaults defaults,
            IActiveMoldsProvider moldsProvider,
            AnimationConfig animConfig)
        {
            _castService = castService ?? throw new ArgumentNullException(nameof(castService));
            _animationService = animationService;
            _selectionService = selectionService;
            _audioService = audioService;
            _lookup = lookup;
            _defaults = defaults;
            _moldsProvider = moldsProvider;
            _animConfig = animConfig;
        }

        public void SetLevelData(LevelData levelData)
        {
            _currentLevelData = levelData;
        }

        public bool TryMultiPour(IReadOnlyList<MoldState> sourceStates, IMoldView target, Vector3 selectedOriginalPos, IMoldView[] activeMolds)
        {
            if (sourceStates == null || sourceStates.Count == 0) return false;
            if (target == null) return false;

            var activeLevelData = _defaults.GetActiveLevelData(_currentLevelData);
            var sourceViews = new IMoldView[sourceStates.Count];

            for (int i = 0; i < sourceStates.Count; i++)
                sourceViews[i] = _lookup.FindByState(sourceStates[i]);

            if (_castService.TryMultiCast(sourceViews, target, activeLevelData, activeMolds))
            {
                MoldLogger.LogInfo($"{LogTag} Multi-cast succeeded ({sourceStates.Count} sources).");

                for (int i = 0; i < sourceViews.Length; i++)
                {
                    var idx = i;
                    _animationService.AnimateCast(
                        sourceViews[idx],
                        target,
                        _animConfig.CastDuration,
                        onComplete: () =>
                        {
                            sourceViews[idx].SetSelectionHighlight(false);
                            _animationService.AnimateMoldLower(
                                sourceViews[idx].Transform,
                                selectedOriginalPos, _animConfig.liftDuration);
                        });
                }

                _selectionService.Deselect();
                return true;
            }
            else
            {
                MoldLogger.LogDebug($"{LogTag} Multi-cast rejected.");
                _audioService?.PlaySfx(AudioClipId.Error);
                _animationService?.AnimateErrorShake(target.Transform, onComplete: () =>
                {
                    for (int i = 0; i < sourceViews.Length; i++)
                    {
                        sourceViews[i].SetSelectionHighlight(false);
                        _animationService.AnimateMoldLower(
                            sourceViews[i].Transform,
                            selectedOriginalPos, _animConfig.liftDuration);
                    }
                    _selectionService.Deselect();
                });
                return false;
            }
        }
    }
}