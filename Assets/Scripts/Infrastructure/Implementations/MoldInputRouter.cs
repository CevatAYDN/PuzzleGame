using System;
using PuzzleGame.Application.Configuration;
using PuzzleGame.Application.Events;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Logging;
using PuzzleGame.Application.Services;
using PuzzleGame.Domain.Interfaces;
using PuzzleGame.Domain.Models;
using UnityEngine;

namespace PuzzleGame.Infrastructure.Implementations
{
    /// <summary>
    /// Input → action router. Owns the select/cast/deselect flow. Delegates
    /// mold lookup to <see cref="IMoldLookupCache"/> and LevelData fallback
    /// to <see cref="IInputHandlerDefaults"/>. This split keeps the router
    /// focused on orchestration (one responsibility) while data concerns
    /// live in their own services.
    /// </summary>
    public sealed class MoldInputRouter : IMoldInputRouter
    {
        private readonly IInputHandler _inputHandler;
        private readonly Camera _camera;
        private readonly IGameStateMachine _stateMachine;
        private readonly IAnimationService _animationService;
        private readonly IMoldSelectionService _selectionService;
        private readonly IMoldValidator _validator;
        private readonly GameConfig _gameConfig;
        private readonly AnimationConfig _animConfig;
        private readonly IAudioService _audioService;
        private readonly IGameHistoryManager _historyManager;
        private readonly ICastService _castService;
        private readonly IMoldLookupCache _lookup;
        private readonly IInputHandlerDefaults _defaults;

        private LevelData _currentLevelData;
        private Vector3 _selectedOriginalPos;

        public MoldInputRouter(
            IInputHandler inputHandler,
            Camera camera,
            IGameStateMachine stateMachine,
            IAnimationService animationService,
            IMoldSelectionService selectionService,
            IMoldValidator validator,
            GameConfig gameConfig,
            AnimationConfig animConfig,
            IAudioService audioService,
            IGameHistoryManager historyManager,
            ICastService castService,
            IMoldLookupCache lookup,
            IInputHandlerDefaults defaults)
        {
            _inputHandler = inputHandler ?? throw new ArgumentNullException(nameof(inputHandler));
            _camera = camera;
            _stateMachine = stateMachine;
            _animationService = animationService;
            _selectionService = selectionService;
            _validator = validator;
            _gameConfig = gameConfig;
            _animConfig = animConfig;
            _audioService = audioService;
            _historyManager = historyManager;
            _castService = castService;
            _lookup = lookup ?? throw new ArgumentNullException(nameof(lookup));
            _defaults = defaults ?? throw new ArgumentNullException(nameof(defaults));
        }

        public void ProcessInput()
        {
            if (_stateMachine == null || (!_stateMachine.IsInState(GameState.Playing) && !_stateMachine.IsInState(GameState.OptionalCasting))) return;
            if (_animationService == null || _animationService.IsAnimating) return;
            if (_inputHandler == null) return;

            if (_inputHandler.GetPointerDown(out Vector2 screenPos))
                HandleInput(screenPos);
        }

        public void SetLevelData(LevelData levelData)
        {
            _currentLevelData = levelData;
        }

        public void DisposeDefaults()
        {
            _defaults.Dispose();
        }

        private void HandleInput(Vector2 screenPos)
        {
            if (!_inputHandler.Raycast(screenPos, _gameConfig.MoldLayerMask, out RaycastHit hit, out Collider hitCollider))
            {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                if (_inputHandler.Raycast(screenPos, ~0, out RaycastHit debugHit, out _))
                {
                    int layer = debugHit.collider != null ? debugHit.collider.gameObject.layer : 0;
                    MoldLogger.LogWarning($"Input raycast missed Mold because it hit '{debugHit.collider?.name}' " +
                        $"(Layer: {LayerMask.LayerToName(layer)}) instead of the Mold layer.");
                }
                else
                {
                    MoldLogger.LogWarning($"Input raycast missed everything at screen position {screenPos}.");
                }
#endif
                if (_selectionService.SelectedMold != null)
                {
                    LowerSelectedMold();
                    _selectionService.Deselect();
                }
                return;
            }

            // O(1) lookup via collider EntityId cache.
            IMoldView clicked = _lookup.FindByCollider(hitCollider);

            if (clicked == null)
            {
                MoldLogger.LogDebug("Could not resolve clicked Mold.");
                return;
            }

            var selectedState = _selectionService.SelectedMold;

            if (selectedState == null)
            {
                TrySelectMold(clicked);
            }
            else if (clicked.State == selectedState)
            {
                LowerSelectedMold();
                _selectionService.Deselect();
            }
            else
            {
                TryCast(_lookup.FindByState(selectedState), clicked);
            }
        }

        private void TrySelectMold(IMoldView mold)
        {
            if (mold.IsCapped)
            {
                MoldLogger.LogDebug("Cannot select completed/capped Mold.");
                return;
            }

            if (mold.IsEmpty)
            {
                MoldLogger.LogDebug("Cannot select empty Mold.");
                return;
            }

            MoldLogger.LogInfo("Selected Mold.");
            _selectedOriginalPos = mold.Transform.position;
            _selectionService.Select(mold.State);
            mold.SetSelectionHighlight(true);
            _animationService.AnimateMoldLift(
                mold.Transform,
                _animConfig.liftHeight, _animConfig.liftDuration,
                keepHovering: () => _selectionService.SelectedMold == mold.State);
        }

        private void TryCast(IMoldView source, IMoldView target)
        {
            if (source == null)
            {
                MoldLogger.LogWarning("TryCast: source Mold not found.");
                _selectionService.Deselect();
                return;
            }

            if (_castService == null)
            {
                MoldLogger.LogError("TryCast: CastService is null — DI may have failed.");
                _selectionService.Deselect();
                return;
            }

            MoldLogger.LogInfo("Attempting Cast.");

            var activeLevelData = _defaults.GetActiveLevelData(_currentLevelData);
            int castCount = _castService.GetCastLayerCount(source, target, activeLevelData);

            if (_castService.TryCast(source, target, activeLevelData, null))
            {
                if (MoldLogger.IsInfoEnabled)
                    MoldLogger.LogInfo($"Cast succeeded ({castCount} layers).");

                _animationService.AnimateCast(
                    source,
                    target,
                    _animConfig.CastDuration,
                    onComplete: () =>
                    {
                        source.SetSelectionHighlight(false);
                        _animationService.AnimateMoldLower(
                            source.Transform,
                            _selectedOriginalPos, _animConfig.liftDuration);
                    });

                _selectionService.Deselect();
            }
            else
            {
                MoldLogger.LogDebug("Cast rejected.");
                _audioService?.PlaySfx(AudioClipId.Error);
                _animationService?.AnimateErrorShake(source.Transform, onComplete: () =>
                {
                    LowerSelectedMold();
                    _selectionService.Deselect();
                });
            }
        }

        private void LowerSelectedMold()
        {
            var selected = _lookup.FindByState(_selectionService.SelectedMold);
            if (selected != null)
            {
                selected.SetSelectionHighlight(false);
                _animationService.AnimateMoldLower(
                    selected.Transform,
                    _selectedOriginalPos, _animConfig.liftDuration);
            }
        }
    }
}
