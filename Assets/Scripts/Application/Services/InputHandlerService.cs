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
    /// Uses IBottleView abstraction instead of BottleController (MonoBehaviour) —
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
        private readonly IBottleSelectionService _selectionService;
        private readonly IBottleValidator _validator;
        private readonly GameConfig _gameConfig;
        private readonly AnimationConfig _animConfig;
        private readonly IAudioService _audioService;
        private readonly IGameHistoryManager _historyManager;
        private readonly IPourService _pourService;
        private LevelData _currentLevelData;

        private IBottleView[] _bottles;
        private Vector3 _selectedOriginalPos;


        public InputHandlerService(
            IInputHandler inputHandler,
            Camera camera,
            IGameStateMachine stateMachine,
            IAnimationService animationService,
            IBottleSelectionService selectionService,
            IBottleValidator validator,
            GameConfig gameConfig,
            AnimationConfig animConfig,
            IAudioService audioService,
            IGameHistoryManager historyManager,
            IPourService pourService)
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
            _pourService = pourService;
        }

        public void SetLevelData(LevelData levelData)
        {
            _currentLevelData = levelData;
        }

        public void SetBottles(IBottleView[] bottles)
        {
            _bottles = bottles;
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
            if (!_inputHandler.Raycast(screenPos, _gameConfig.bottleLayerMask, out RaycastHit hit, out Collider hitCollider))
            {
                if (BottleLogger.IsWarningEnabled)
                {
                    if (_inputHandler.Raycast(screenPos, ~0, out RaycastHit debugHit, out _))
                    {
                        BottleLogger.LogWarning($"Input raycast missed bottle because it hit '{debugHit.collider?.name}' " +
                            $"(Layer: {LayerMask.LayerToName(debugHit.collider?.gameObject.layer ?? 0)}) instead of the bottle layer.");
                    }
                    else
                    {
                        BottleLogger.LogWarning($"Input raycast missed everything at screen position {screenPos}.");
                    }
                }
                if (_selectionService.SelectedBottle != null)
                {
                    LowerSelectedBottle();
                    _selectionService.Deselect();
                }
                return;
            }

            // Resolve clicked bottle from the Collider returned by the new Raycast overload.
            IBottleView clicked = hitCollider != null ? hitCollider.GetComponent<IBottleView>() : null;

            // Fallback: search bottle list by collider instance ID (edge case: pooled objects)
            if (clicked == null && hitCollider != null && _bottles != null)
            {
                var colliderId = hitCollider.GetEntityId();
                foreach (var b in _bottles)
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
                BottleLogger.LogDebug("Could not resolve clicked bottle.");
                return;
            }

            var selectedState = _selectionService.SelectedBottle;

            if (selectedState == null)
            {
                TrySelectBottle(clicked);
            }
            else if (clicked.State == selectedState)
            {
                LowerSelectedBottle();
                _selectionService.Deselect();
            }
            else
            {
                TryPour(FindBottleByState(selectedState), clicked);
            }
        }

        private void TrySelectBottle(IBottleView bottle)
        {
            if (bottle.IsCapped)
            {
                BottleLogger.LogDebug("Cannot select completed/capped bottle.");
                return;
            }

            if (bottle.IsEmpty)
            {
                BottleLogger.LogDebug("Cannot select empty bottle.");
                return;
            }

            BottleLogger.LogInfo("Selected bottle.");
            _selectedOriginalPos = bottle.Transform.position;
            _selectionService.Select(bottle.State);
            bottle.SetSelectionHighlight(true);
            _animationService.AnimateBottleLift(
                bottle.Transform,
                _animConfig.liftHeight, _animConfig.liftDuration,
                keepHovering: () => _selectionService.SelectedBottle == bottle.State);
        }

        private void TryPour(IBottleView source, IBottleView target)
        {
            if (source == null)
            {
                BottleLogger.LogWarning("TryPour: source bottle not found.");
                _selectionService.Deselect();
                return;
            }

            if (_pourService == null)
            {
                BottleLogger.LogError("TryPour: PourService is null — DI may have failed.");
                _selectionService.Deselect();
                return;
            }

            BottleLogger.LogInfo("Attempting pour.");

            var activeLevelData = GetActiveLevelData();
            int pourCount = _pourService.GetPourLayerCount(source, target, activeLevelData);

            if (_pourService.TryPour(source, target, activeLevelData, _bottles))
            {
                if (BottleLogger.IsInfoEnabled)
                    BottleLogger.LogInfo($"Pour succeeded ({pourCount} layers).");

                _animationService.AnimatePour(
                    source,
                    target,
                    _animConfig.pourDuration,
                    onComplete: () =>
                    {
                        source.SetSelectionHighlight(false);
                        _animationService.AnimateBottleLower(
                            source.Transform,
                            _selectedOriginalPos, _animConfig.liftDuration);
                    });

                _selectionService.Deselect();
            }
            else
            {
                BottleLogger.LogDebug("Pour rejected.");
                _audioService?.PlaySfx(AudioClipId.Error);
                _animationService?.AnimateErrorShake(source.Transform, onComplete: () =>
                {
                    LowerSelectedBottle();
                    _selectionService.Deselect();
                });
            }
        }

        private void LowerSelectedBottle()
        {
            var selected = FindBottleByState(_selectionService.SelectedBottle);
            if (selected != null)
            {
                selected.SetSelectionHighlight(false);
                _animationService.AnimateBottleLower(
                    selected.Transform,
                    _selectedOriginalPos, _animConfig.liftDuration);
            }
        }

        private IBottleView FindBottleByState(BottleState state)
        {
            if (state == null || _bottles == null) return null;
            foreach (var b in _bottles)
                if (b != null && b.State == state) return b;
            return null;
        }

        /// <summary>
        /// Fix #4: Returns existing level data or a pre-built default LevelData asset.
        /// No longer creates ScriptableObject instances at runtime.
        /// </summary>
        private LevelData GetActiveLevelData()
        {
            if (_currentLevelData != null) return _currentLevelData;

            // Play-test fallback: return a minimal inline-configured LevelData.
            // This is only reached when playing directly from the scene editor
            // without going through the level selection flow.
            BottleLogger.LogDebug("GetActiveLevelData: no level set, using play-test defaults.");
            return _playTestDefaults;
        }

        /// <summary>
        /// Cached play-test defaults — pure POCO, no ScriptableObject dependency.
        /// Used when no level data is set (editor direct play mode).
        /// </summary>
        private static readonly LevelData _playTestDefaults = new LevelData
        {
            autoGenerate = false,
            enableMultiLayerPour = false,
            enableReactionSystem = false
        };
    }
}
