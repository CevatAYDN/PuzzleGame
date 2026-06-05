using System;
using System.Collections.Generic;
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
        // EntityId is Unity 6's stable per-object identifier (replaces the deprecated
        // int GetInstanceID()). It is a value type with proper IEquatable<EntityId>/
        // GetHashCode, so it's safe as a Dictionary key.
        private readonly Dictionary<EntityId, IMoldView> _MoldByColliderId = new Dictionary<EntityId, IMoldView>();
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
            _MoldByColliderId.Clear();
            if (Molds == null) return;
            for (int i = 0; i < Molds.Length; i++)
            {
                var view = Molds[i];
                if (view?.GameObject == null) continue;
                var col = view.GameObject.GetComponent<Collider>();
                if (col != null)
                    _MoldByColliderId[col.GetEntityId()] = view;
            }
        }

        public void ProcessInput()
        {
            if (_stateMachine == null || (!_stateMachine.IsInState(GameState.Playing) && !_stateMachine.IsInState(GameState.OptionalCasting))) return;
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

            // O(1) lookup via collider instance ID cache (built in SetMolds).
            IMoldView clicked = null;
            if (hitCollider != null)
                _MoldByColliderId.TryGetValue(hitCollider.GetEntityId(), out clicked);

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
        /// Returns existing level data or a transient default for play-test mode.
        /// Uses a lightweight struct instead of ScriptableObject to avoid asset leaks.
        /// </summary>
        private LevelData _playTestDefaults;

        private LevelData GetActiveLevelData()
        {
            if (_currentLevelData != null) return _currentLevelData;

            if (_playTestDefaults == null)
            {
                _playTestDefaults = CreatePlayTestLevelDefaults();
            }

            MoldLogger.LogDebug("GetActiveLevelData: no level set, using play-test defaults.");
            return _playTestDefaults;
        }

        private LevelData CreatePlayTestLevelDefaults()
        {
            var data = ScriptableObject.CreateInstance<LevelData>();
            data.autoGenerate = false;
            data.enableMultiLayerCast = false;
            data.enableReactionSystem = false;
            data.hideFlags = HideFlags.HideAndDontSave;
            data.name = "InputHandlerService_PlayTestDefaults";
            return data;
        }

        public void DisposeDefaults()
        {
            if (_playTestDefaults != null)
            {
                UnityEngine.Object.Destroy(_playTestDefaults);
                _playTestDefaults = null;
            }
        }
    }
}
