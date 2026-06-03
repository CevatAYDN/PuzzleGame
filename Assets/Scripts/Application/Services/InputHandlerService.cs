using System;
using UnityEngine;
using PuzzleGame.Domain.Interfaces;
using PuzzleGame.Domain.Models;
using PuzzleGame.Application.Configuration;
using PuzzleGame.Infrastructure.Interfaces;
using PuzzleGame.Application.Events;
using PuzzleGame.Application.Logging;
using PuzzleGame.Application.Interfaces;


namespace PuzzleGame.Application.Services
{
    /// <summary>
    /// Handles all input-related logic for the game.
    /// Artık BottleController (MonoBehaviour) yerine IBottleView abstraction kullanır —
    /// Domain katmanı izole kalır, unit testler yazılabilir.
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
            // Perform raycast against the configured bottle layer mask.
            if (!_inputHandler.Raycast(screenPos, _gameConfig.bottleLayerMask, out RaycastHit hit))
            {
                if (BottleLogger.IsWarningEnabled)
                {
                    if (_inputHandler.Raycast(screenPos, ~0, out RaycastHit debugHit))
                    {
                        BottleLogger.LogWarning($"Input raycast missed bottle because it hit '{debugHit.collider.name}' (Layer: {LayerMask.LayerToName(debugHit.collider.gameObject.layer)}) instead of the bottle layer.");
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

            var clicked = hit.collider.GetComponent<IBottleView>();
            if (clicked == null)
            {
                BottleLogger.LogDebug("Hit collider has no IBottleView component.");
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

            if (_pourService.TryPour(source, target, _currentLevelData))
            {
                int pourCount = _pourService.GetPourLayerCount(source, target, _currentLevelData);
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
    }
}
