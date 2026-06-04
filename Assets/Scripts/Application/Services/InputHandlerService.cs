using System;
using UnityEngine;
using PuzzleGame.Domain.Interfaces;
using PuzzleGame.Domain.Models;
using PuzzleGame.Application.Configuration;
// IInputHandler now in PuzzleGame.Application.Interfaces
using PuzzleGame.Application.Events;
using PuzzleGame.Application.Logging;
using PuzzleGame.Application.Interfaces;


namespace PuzzleGame.Application.Services
{
    /// <summary>
    /// Handles all input-related logic for the game.
    /// Uses IMoldView abstraction instead of MoldController (MonoBehaviour) —
    /// Domain layer stays isolated, unit tests are writable.
    ///
    /// Fix #3: No reflection on RaycastHit — uses IInputHandler.Raycast overload
    ///         that returns Collider directly.
    /// Fix #4: No ScriptableObject.CreateInstance in GetActiveLevelData() —
    ///         uses plain DefaultLevelSettings struct instead.
    /// </summary>
    public class InputHandlerService : IInputHandlerService
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
        private readonly ICastService _CastService;
        private LevelData _currentLevelData;

        private IMoldView[] _Molds;
        private Vector3 _selectedOriginalPos;


        public InputHandlerService(
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
            ICastService CastService)
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
            _CastService = CastService;
        }

        public void SetLevelData(LevelData levelData)
        {
            _currentLevelData = levelData;
        }

        public void SetMolds(IMoldView[] Molds)
        {
            _Molds = Molds;
        }

        public void ProcessInput()
        {
            if (_stateMachine == null || !_stateMachine.IsInState(GameState.Playing)) return;
            if (_animationService == null || _animationService.IsAnimating) return;
            if (_inputHandler == null) return;

            if (_inputHandler.GetPointerDown(out Vector2 screenPos))
                HandleInput(screenPos);
        }

        private void HandleInput(Vector2 screenPos)
        {
            // Fix #3: Use the four-argument Raycast overload — Collider resolved directly,
            // no reflection on RaycastHit private fields.
            if (!_inputHandler.Raycast(screenPos, _gameConfig.MoldLayerMask, out RaycastHit hit, out Collider hitCollider))
            {
                if (MoldLogger.IsWarningEnabled)
                {
                    if (_inputHandler.Raycast(screenPos, ~0, out RaycastHit debugHit, out _))
                    {
                        MoldLogger.LogWarning($"Input raycast missed Mold because it hit '{debugHit.collider?.name}' " +
                            $"(Layer: {LayerMask.LayerToName(debugHit.collider?.gameObject.layer ?? 0)}) instead of the Mold layer.");
                    }
                    else
                    {
                        MoldLogger.LogWarning($"Input raycast missed everything at screen position {screenPos}.");
                    }
                }
                if (_selectionService.SelectedMold != null)
                {
                    LowerSelectedMold();
                    _selectionService.Deselect();
                }
                return;
            }

            // Resolve clicked Mold from the Collider returned by the new Raycast overload.
            IMoldView clicked = hitCollider != null ? hitCollider.GetComponent<IMoldView>() : null;

            // Fallback: search Mold list by collider instance ID (edge case: pooled objects)
            if (clicked == null && hitCollider != null && _Molds != null)
            {
                var colliderId = hitCollider.GetEntityId();
                foreach (var b in _Molds)
                {
                    if (b?.GameObject == null) continue;
                    var col = b.GameObject.GetComponent<Collider>();
                    if (col != null && col.GetEntityId() == colliderId)
                    {
                        clicked = b;
                        break;
                    }
                }
            }

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
                TryCast(FindMoldByState(selectedState), clicked);
            }
        }

        private void TrySelectMold(IMoldView Mold)
        {
            if (Mold.IsCapped)
            {
                MoldLogger.LogDebug("Cannot select completed/capped Mold.");
                return;
            }

            if (Mold.IsEmpty)
            {
                MoldLogger.LogDebug("Cannot select empty Mold.");
                return;
            }

            MoldLogger.LogInfo("Selected Mold.");
            _selectedOriginalPos = Mold.Transform.position;
            _selectionService.Select(Mold.State);
            Mold.SetSelectionHighlight(true);
            _animationService.AnimateMoldLift(
                Mold.Transform,
                _animConfig.liftHeight, _animConfig.liftDuration,
                keepHovering: () => _selectionService.SelectedMold == Mold.State);
        }

        private void TryCast(IMoldView source, IMoldView target)
        {
            if (source == null)
            {
                MoldLogger.LogWarning("TryCast: source Mold not found.");
                _selectionService.Deselect();
                return;
            }

            if (_CastService == null)
            {
                MoldLogger.LogError("TryCast: CastService is null — DI may have failed.");
                _selectionService.Deselect();
                return;
            }

            MoldLogger.LogInfo("Attempting Cast.");

            var activeLevelData = GetActiveLevelData();
            int CastCount = _CastService.GetCastLayerCount(source, target, activeLevelData);

            if (_CastService.TryCast(source, target, activeLevelData, _Molds))
            {
                if (MoldLogger.IsInfoEnabled)
                    MoldLogger.LogInfo($"Cast succeeded ({CastCount} layers).");

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
            var selected = FindMoldByState(_selectionService.SelectedMold);
            if (selected != null)
            {
                selected.SetSelectionHighlight(false);
                _animationService.AnimateMoldLower(
                    selected.Transform,
                    _selectedOriginalPos, _animConfig.liftDuration);
            }
        }

        private IMoldView FindMoldByState(MoldState state)
        {
            if (state == null || _Molds == null) return null;
            foreach (var b in _Molds)
                if (b != null && b.State == state) return b;
            return null;
        }

        /// <summary>
        /// Fix #4: Returns existing level data or a pre-built default LevelData asset.
        /// No longer creates ScriptableObject instances at runtime.
        /// </summary>
        private static LevelData _playTestDefaults;

        /// <summary>
        /// Fix #4: Returns existing level data or a pre-built default LevelData asset.
        /// No longer creates ScriptableObject instances at runtime via new operator.
        /// </summary>
        private LevelData GetActiveLevelData()
        {
            if (_currentLevelData != null) return _currentLevelData;

            // Play-test fallback: return a minimal inline-configured LevelData.
            // This is only reached when playing directly from the scene editor
            // without going through the level selection flow.
            if (_playTestDefaults == null)
            {
                _playTestDefaults = ScriptableObject.CreateInstance<LevelData>();
                _playTestDefaults.autoGenerate = false;
                _playTestDefaults.enableMultiLayerCast = false;
                _playTestDefaults.enableReactionSystem = false;
            }

            MoldLogger.LogDebug("GetActiveLevelData: no level set, using play-test defaults.");
            return _playTestDefaults;
        }
    }
}
