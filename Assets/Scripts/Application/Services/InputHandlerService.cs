using System;
using UnityEngine;
using PuzzleGame.Domain.Interfaces;
using PuzzleGame.Domain.Models;
using PuzzleGame.Infrastructure.Interfaces;
using PuzzleGame.Events;
using PuzzleGame.Logging;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Configuration;

namespace PuzzleGame.Application.Services
{
    /// <summary>
    /// Handles all input-related logic for the game
    /// </summary>
    public class InputHandlerService
    {
        private readonly IInputHandler _inputHandler;
        private readonly Camera _camera;
        private readonly IGameStateMachine _stateMachine;
        private readonly IAnimationService _animationService;
        private readonly IBottleSelectionService _selectionService;
        private readonly IBottleValidator _validator;
        private readonly GameConfig _gameConfig;
        private readonly AnimationConfig _animConfig;

        private BottleController[] _bottles;
        private Vector3 _selectedOriginalPos;
        private Action _onPourSucceeded;
        private Action _onRecordUndoSnapshot;

        public InputHandlerService(
            IInputHandler inputHandler,
            Camera camera,
            IGameStateMachine stateMachine,
            IAnimationService animationService,
            IBottleSelectionService selectionService,
            IBottleValidator validator,
            GameConfig gameConfig,
            AnimationConfig animConfig,
            Action onPourSucceeded = null,
            Action onRecordUndoSnapshot = null)
        {
            _inputHandler = inputHandler;
            _camera = camera;
            _stateMachine = stateMachine;
            _animationService = animationService;
            _selectionService = selectionService;
            _validator = validator;
            _gameConfig = gameConfig;
            _animConfig = animConfig;
            _onPourSucceeded = onPourSucceeded;
            _onRecordUndoSnapshot = onRecordUndoSnapshot;
        }

        public void SetBottles(BottleController[] bottles)
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
            if (!_inputHandler.Raycast(screenPos, _gameConfig.bottleLayerMask, out RaycastHit hit))
            {
                if (_selectionService.SelectedBottle != null)
                {
                    LowerSelectedBottle();
                    _selectionService.Deselect();
                }
                return;
            }

            var clicked = hit.collider.GetComponent<BottleController>();
            if (clicked == null)
            {
                BottleLogger.LogDebug("Hit collider has no BottleController.");
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

        private void TrySelectBottle(BottleController bottle)
        {
            if (bottle.IsCapped)
            {
                BottleLogger.LogDebug($"Cannot select completed/capped bottle '{bottle.name}'.");
                return;
            }

            if (bottle.IsEmpty())
            {
                BottleLogger.LogDebug($"Cannot select empty bottle '{bottle.name}'.");
                return;
            }

            BottleLogger.LogInfo($"Selected '{bottle.name}'.");
            _selectedOriginalPos = bottle.transform.position;
            _selectionService.Select(bottle.State);
            bottle.SetSelectionHighlight(true);
            _animationService.AnimateBottleLift(
                bottle.transform,
                _animConfig.liftHeight, _animConfig.liftDuration,
                keepHovering: () => _selectionService.SelectedBottle == bottle.State);
        }

        private void TryPour(BottleController source, BottleController target)
        {
            if (source == null)
            {
                BottleLogger.LogWarning("TryPour: source bottle not found in scene.");
                _selectionService.Deselect();
                return;
            }

            BottleLogger.LogInfo($"Attempting pour: '{source.name}' → '{target.name}'.");

            if (_validator.CanPour(source.State, target.State))
            {
                // Hamle başarılı olacağı için ÖNCE undo state'i kaydet
                _onRecordUndoSnapshot?.Invoke();
                
                if (source.TryPourTo(target))
                {
                    BottleLogger.LogInfo($"Pour succeeded.");
                    _onPourSucceeded?.Invoke();
                    _animationService.AnimatePour(
                        source, target,
                        _animConfig.pourDuration,
                        onComplete: () =>
                        {
                            source.SetSelectionHighlight(false);
                            _animationService.AnimateBottleLower(
                                source.transform,
                                _selectedOriginalPos, _animConfig.liftDuration);
                        });

                    _selectionService.Deselect();
                    EventAggregator.Publish(new PourCompletedEvent(source.State, target.State));
                }
            }
            else
            {
                BottleLogger.LogDebug($"Pour rejected: '{source.name}' → '{target.name}'.");
                _animationService?.AnimateErrorShake(source.transform, onComplete: () =>
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
                    selected.transform,
                    _selectedOriginalPos, _animConfig.liftDuration);
            }
        }

        private BottleController FindBottleByState(BottleState state)
        {
            if (state == null || _bottles == null) return null;
            foreach (var b in _bottles)
                if (b != null && b.State == state) return b;
            return null;
        }
    }
}