using System;
using System.Collections.Generic;
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
    /// <remarks>
    /// Raycast / hit-test ordering (MoldInputRouter + IMoldLookupCache):
    /// <list type="number">
    /// <item><see cref="Camera"/> ray cast hits the <c>Collider</c> of every <c>IMoldView</c>.</item>
    /// <item>The collider is resolved to its owning mold via
    ///   <see cref="IMoldLookupCache.FindByCollider"/>, which uses the O(1)
    ///   EntityId dictionary built in <see cref="IMoldLookupCache.SetMolds"/>.</item>
    /// <item>If two colliders overlap, the raycast returns the closest one
    ///   (Unity's <see cref="Physics.Raycast"/> default). Z-order tie-break
    ///   is therefore "nearest hit first"; do not rely on array order in
    ///   <see cref="IActiveMoldsProvider.Molds"/>.</item>
    /// </list>
    /// When <see cref="IMoldLookupCache.FindByCollider"/> misses (test setup
    /// without proper EntityId, etc.) the router falls back to a linear scan
    /// over <see cref="IActiveMoldsProvider.Molds"/> in array order.
    /// </remarks>
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
        private readonly IActiveMoldsProvider _moldsProvider;
        private readonly IHapticFeedbackService _hapticService;
        private readonly IAnalyticsService _analytics;
        private readonly IMultiPourService _multiPourService;

        private LevelData _currentLevelData;
        private Vector3 _selectedOriginalPos;
        // Fix #24: Frame guard to prevent processing multiple inputs per frame
        private int _lastProcessedFrame = -1;

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
            IInputHandlerDefaults defaults,
            IActiveMoldsProvider moldsProvider,
            IHapticFeedbackService hapticService,
            IAnalyticsService analytics,
            IMultiPourService multiPourService)
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
            _moldsProvider = moldsProvider ?? throw new ArgumentNullException(nameof(moldsProvider));
            _hapticService = hapticService ?? throw new ArgumentNullException(nameof(hapticService));
            _analytics = analytics ?? throw new ArgumentNullException(nameof(analytics));
            _multiPourService = multiPourService ?? throw new ArgumentNullException(nameof(multiPourService));
        }

        /// <summary>
        /// Test-only seam: resets the per-frame guard so a unit test can drive
        /// ProcessInput multiple times within a single frame. Marked public so
        /// the test assembly can invoke it without an InternalsVisibleTo attribute.
        /// </summary>
        public void ResetFrameGuardForTests() => _lastProcessedFrame = -1;

        public void ProcessInput()
        {
            // Fix #24: Frame-based guard — ensure ProcessInput is called only once per frame.
            // Tests should call ResetFrameGuardForTests() between inputs to allow multiple
            // calls within a single test frame.
            int currentFrame = Time.frameCount;
            if (currentFrame == _lastProcessedFrame) return;
            _lastProcessedFrame = currentFrame;

            if (_stateMachine == null)
            {
                MoldLogger.LogWarning("[MoldInputRouter] ProcessInput skipped: _stateMachine is null.");
                return;
            }
            if (!_stateMachine.IsInState(GameState.Playing) && !_stateMachine.IsInState(GameState.OptionalCasting))
            {
                // Comment out to avoid spamming the log, but keep track of it if needed
                // MoldLogger.LogDebug($"[MoldInputRouter] ProcessInput skipped: current state is {_stateMachine.Current}");
                return;
            }
            if (_animationService == null)
            {
                MoldLogger.LogWarning("[MoldInputRouter] ProcessInput skipped: _animationService is null.");
                return;
            }
            if (_animationService.IsAnimating)
            {
                MoldLogger.LogWarning("[MoldInputRouter] ProcessInput skipped: _animationService.IsAnimating is true.");
                return;
            }
            if (_inputHandler == null)
            {
                MoldLogger.LogWarning("[MoldInputRouter] ProcessInput skipped: _inputHandler is null.");
                return;
            }

            if (_inputHandler.GetPointerDown(out Vector2 screenPos))
            {
                MoldLogger.LogInfo($"[MoldInputRouter] Pointer down detected at {screenPos}. Invoking HandleInput.");
                HandleInput(screenPos);
            }
        }

        public void SetLevelData(LevelData levelData)
        {
            _currentLevelData = levelData;
        }

        public void DisposeDefaults()
        {
            _defaults.Dispose();
        }

        private static bool IsStateInSelection(IReadOnlyList<MoldState> selectedMolds, MoldState state)
        {
            for (int i = 0; i < selectedMolds.Count; i++)
            {
                if (selectedMolds[i] == state) return true;
            }
            return false;
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
            bool isMultiPour = _currentLevelData != null && _currentLevelData.enableMultiPour;

            if (selectedState == null)
            {
                TrySelectMold(clicked, isMultiPour);
            }
            else if (isMultiPour && _selectionService.SelectedMolds.Count >= 1)
            {
                HandleMultiPourInput(clicked);
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

        private void TrySelectMold(IMoldView mold, bool isMultiPour = false)
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

            if (isMultiPour)
                _selectionService.SetMultiSelect(true);

            MoldLogger.LogInfo("Selected Mold.");
            _selectedOriginalPos = mold.Transform.position;
            _selectionService.Select(mold.State);
            mold.SetSelectionHighlight(true);
            _hapticService.Trigger(HapticIntensity.Selection);
            _animationService.AnimateMoldLift(
                mold.Transform,
                _animConfig.liftHeight, _animConfig.liftDuration,
                keepHovering: () => _selectionService.IsMultiSelect
                    ? IsStateInSelection(_selectionService.SelectedMolds, mold.State)
                    : _selectionService.SelectedMold == mold.State);
        }

        private void HandleMultiPourInput(IMoldView clicked)
        {
            var selectedMolds = _selectionService.SelectedMolds;

            // Toggle clicked mold in/out of selection if it's a valid source
            if (!clicked.IsEmpty && !clicked.IsCapped)
            {
                if (IsStateInSelection(_selectionService.SelectedMolds, clicked.State))
                {
                    // Deselect this specific mold
                    clicked.SetSelectionHighlight(false);
                    _selectionService.Deselect(clicked.State);
                    MoldLogger.LogInfo("Mold deselected from multi-pour selection.");
                    return;
                }

                // Add to multi-pour selection
                _selectionService.Select(clicked.State);
                clicked.SetSelectionHighlight(true);
                _hapticService.Trigger(HapticIntensity.Selection);
                MoldLogger.LogInfo("Mold added to multi-pour selection.");
                return;
            }

            // If clicked mold is empty or capped, treat as target and attempt multi-cast
            if (selectedMolds.Count >= 1)
            {
                TryMultiCast(selectedMolds, clicked);
            }
        }

        private void TryMultiCast(IReadOnlyList<MoldState> sourceStates, IMoldView target)
        {
            _multiPourService.TryMultiPour(sourceStates, target, _selectedOriginalPos, _moldsProvider.Molds);
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

            if (_castService.TryCast(source, target, activeLevelData, _moldsProvider.Molds))
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
                _analytics.Track(AnalyticsEvent.ErrorShown, new Dictionary<string, object>
                {
                    { "errorCode", "validator_rejected" },
                    { "sourceIndex", source?.MoldIndex ?? -1 },
                    { "targetIndex", target?.MoldIndex ?? -1 }
                });
            }
        }

        private void LowerSelectedMold()
        {
            var selected = _lookup.FindByState(_selectionService.SelectedMold);
            if (selected != null)
            {
                selected.SetSelectionHighlight(false);
                _hapticService.Trigger(HapticIntensity.Selection);
                _animationService.AnimateMoldLower(
                    selected.Transform,
                    _selectedOriginalPos, _animConfig.liftDuration);
            }
        }
    }
}
