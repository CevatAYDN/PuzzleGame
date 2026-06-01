using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using PuzzleGame.Domain.Models;
using PuzzleGame.Domain.Services;
using PuzzleGame.Domain.Interfaces;
using PuzzleGame.Application.Services;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Infrastructure.Interfaces;
using PuzzleGame.Infrastructure.Implementations;
using PuzzleGame.Infrastructure;
using PuzzleGame.Events;
using PuzzleGame.Logging;
using PuzzleGame.Configuration;

namespace PuzzleGame
{
    public class GameManager : MonoBehaviour, IUpdateable
    {
        [Header("Configuration (assign via Resources or Inspector)")]
        [SerializeField] private GameConfig     gameConfig;
        [SerializeField] private AnimationConfig animConfig;
        [SerializeField] private LevelConfig    levelConfig;

        [Header("HUD (optional)")]
        [SerializeField] private Canvas    hudCanvas;
        [SerializeField] private TMPro.TextMeshProUGUI moveCountText;
        [SerializeField] private GameObject winPanel;

        private IBottleValidator      _validator;
        private IRendererService      _rendererService;
        private IAnimationService     _animationService;
        private IBottleSelectionService _selectionService;
        private IInputHandler         _inputHandler;
        private IGameHistoryService   _gameHistoryService;
        private IGameStateMachine     _stateMachine;

        private BottleController[] _bottles;
        private Camera             _mainCam;

        private int     _moveCount;
        private Vector3 _selectedOriginalPos;

        private Action<BottleState> _onBottleSelectedHandler;
        private Action<BottleState> _onBottleDeselectedHandler;

        private static readonly WaitForSeconds WinCheckDelay = new WaitForSeconds(0.5f);
        private static readonly Color CamDefaultBgColor = new Color(0.08f, 0.05f, 0.16f, 1f);

        private static readonly Color[] DefaultPalette = new Color[]
        {
            new Color(0.95f, 0.20f, 0.25f),
            new Color(0.20f, 0.55f, 0.95f),
            new Color(0.30f, 0.85f, 0.35f),
            new Color(0.98f, 0.80f, 0.15f),
            new Color(0.70f, 0.30f, 0.90f),
            new Color(0.95f, 0.50f, 0.15f),
        };

        private void Awake()
        {
            BottleLogger.LogInfo("GameManager Awake — composing services.");
            ComposeServices();
        }

        private void Start()
        {
            BottleLogger.LogInfo("GameManager Start — setting up scene.");
            _stateMachine.TransitionTo(GameState.Menu);
            CacheBottles();
            SetupBottles();
            InitHUD();
            // Auto-advance to LevelLoading → Playing (will be replaced by level select flow later)
            _stateMachine.TransitionTo(GameState.LevelLoading);
            _stateMachine.TransitionTo(GameState.Playing);
        }

        private void OnEnable()
        {
            UpdateManager.Instance?.Register(this);
        }

        private void OnDisable()
        {
            if (UpdateManager.Instance != null)
                UpdateManager.Instance.Unregister(this);
        }

        private void OnDestroy()
        {
            if (_selectionService != null)
            {
                _selectionService.OnBottleSelected   -= _onBottleSelectedHandler;
                _selectionService.OnBottleDeselected -= _onBottleDeselectedHandler;
            }
            EventAggregator.Clear();
        }

        public void OnUpdate(float deltaTime)
        {
            if (_stateMachine == null || !_stateMachine.IsInState(GameState.Playing)) return;
            if (_animationService == null || _animationService.IsAnimating) return;
            if (_inputHandler == null) return;

            if (_inputHandler.GetPointerDown(out Vector2 screenPos))
                HandleInput(screenPos);
        }

        private void ComposeServices()
        {
            if (gameConfig == null)
            {
                gameConfig = Resources.Load<GameConfig>("Data/GameConfig");
                if (gameConfig == null)
                {
                    gameConfig = ScriptableObject.CreateInstance<GameConfig>();
                    BottleLogger.LogWarning("GameConfig not found — using defaults.");
                }
            }

            if (animConfig == null)
            {
                animConfig = Resources.Load<AnimationConfig>("Data/AnimationConfig");
                if (animConfig == null)
                {
                    animConfig = ScriptableObject.CreateInstance<AnimationConfig>();
                    BottleLogger.LogWarning("AnimationConfig not found — using defaults.");
                }
            }

            _mainCam = Camera.main ?? FindAnyObjectByType<Camera>();
            if (_mainCam == null)
            {
                BottleLogger.LogError("No camera found — input will be disabled.");
            }
            else
            {
                _inputHandler = new InputHandler(_mainCam);
            }

            _validator        = new BottleValidationService(gameConfig.colorMatchTolerance);
            _rendererService  = new RendererService();
            _animationService = new AnimationService(animConfig);
            _selectionService = new BottleSelectionService();
            _gameHistoryService = new GameHistoryService();
            _stateMachine = new GameStateMachine();

            _onBottleSelectedHandler = b => EventAggregator.Publish(
                new BottleSelectedEvent(b));
            _onBottleDeselectedHandler = b => EventAggregator.Publish(
                new BottleDeselectedEvent(b));

            _selectionService.OnBottleSelected   += _onBottleSelectedHandler;
            _selectionService.OnBottleDeselected += _onBottleDeselectedHandler;

            BottleLogger.LogDebug("All services composed.");
        }

        private static DomainColor GetBottleColorId(BottleState state)
        {
            return state.IsEmpty || state.Layers.Count == 0
                ? new DomainColor(0, 0, 0, 0)
                : state.Layers[0].Color;
        }

        private void CacheBottles()
        {
            _bottles = FindObjectsByType<BottleController>(FindObjectsInactive.Exclude)
                       .OrderBy(b => b.name)
                       .ToArray();

            BottleLogger.LogInfo($"Found {_bottles.Length} bottles in scene.");

            if (_bottles.Length == 0)
                BottleLogger.LogWarning("No BottleController found — level will be empty.");
        }

        private void SetupBottles()
        {
            if (_bottles.Length == 0) return;

            // Level generation params: levelConfig > fallback values
            bool autoGen = levelConfig != null ? levelConfig.autoGenerateLevel : true;
            int empties  = levelConfig != null ? levelConfig.emptyBottleCount : 2;
            int seed     = levelConfig != null ? levelConfig.randomSeed : 0;
            Color[] pal  = levelConfig != null && levelConfig.palette.Length > 0
                ? levelConfig.palette
                : DefaultPalette;

            List<List<LiquidLayer>> assignments = autoGen
                ? LevelGenerator.Generate(
                    _bottles.Length,
                    gameConfig.maxLayersPerBottle,
                    empties,
                    ConvertPalette(pal),
                    seed)
                : null;

            for (int i = 0; i < _bottles.Length; i++)
            {
                var bottle  = _bottles[i];
                var initial = (assignments != null && i < assignments.Count)
                    ? assignments[i]
                    : new List<LiquidLayer>();

                if (bottle.State == null || bottle.State.MaxLayers == 0)
                {
                    BottleLogger.LogDebug($"Initializing '{bottle.name}' with {initial.Count} layers.");
                    bottle.Initialize(_rendererService, _validator, initial);
                }
                else
                {
                    bottle.UpdateVisuals();
                }
            }

            if (_mainCam != null)
            {
                _mainCam.backgroundColor = CamDefaultBgColor;
                _mainCam.clearFlags      = CameraClearFlags.SolidColor;
            }
        }

        private DomainColor[] ConvertPalette(Color[] colors)
        {
            var result = new DomainColor[colors.Length];
            for (int i = 0; i < colors.Length; i++)
                result[i] = ColorAdapter.FromUnity(colors[i]);
            return result;
        }

        private void HandleInput(Vector2 screenPos)
        {
            if (!_inputHandler.Raycast(screenPos, gameConfig.bottleLayerMask, out RaycastHit hit))
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
                this, bottle.transform,
                animConfig.liftHeight, animConfig.liftDuration,
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

            if (source.TryPourTo(target))
            {
                RecordUndoSnapshot(); // hamle başarılıysa undo state'i kaydet
                _moveCount++;
                BottleLogger.LogInfo($"Pour succeeded. Moves: {_moveCount}.");
                UpdateHUD();

                _animationService.AnimatePour(
                    this, source, target,
                    animConfig.pourDuration,
                    onComplete: () =>
                    {
                        source.SetSelectionHighlight(false);
                        _animationService.AnimateBottleLower(
                            this, source.transform,
                            _selectedOriginalPos, animConfig.liftDuration);
                    });

                _selectionService.Deselect();
                EventAggregator.Publish(new PourCompletedEvent(source.State, target.State));
                StartCoroutine(DelayedWinCheck());
            }
            else
            {
                BottleLogger.LogDebug($"Pour rejected: '{source.name}' → '{target.name}'.");
                _animationService.AnimateErrorShake(this, source.transform, onComplete: () =>
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
                    this, selected.transform,
                    _selectedOriginalPos, animConfig.liftDuration);
            }
        }

        private BottleController FindBottleByState(BottleState state)
        {
            if (state == null || _bottles == null) return null;
            foreach (var b in _bottles)
                if (b != null && b.State == state) return b;
            return null;
        }

        private IEnumerator DelayedWinCheck()
        {
            yield return WinCheckDelay;
            CheckWinCondition();
        }

        private void CheckWinCondition()
        {
            if (_bottles == null || _bottles.Length == 0) return;

            bool hasLiquid   = false;
            bool allComplete = true;

            foreach (var bottle in _bottles)
            {
                if (bottle == null || bottle.IsEmpty()) continue;
                hasLiquid = true;

                bool isComplete = _validator.IsComplete(bottle.State);
                if (isComplete && !bottle.IsCapped)
                {
                    bottle.AnimateCompletion();
                }

                if (!isComplete)
                {
                    allComplete = false;
                }
            }

            if (hasLiquid && allComplete)
            {
                _stateMachine.TransitionTo(GameState.LevelComplete);
                BottleLogger.LogInfo($"Level complete in {_moveCount} moves.");
                EventAggregator.Publish(new LevelCompletedEvent(_moveCount));
                if (winPanel != null) winPanel.SetActive(true);
            }
        }

        private void InitHUD()
        {
            if (winPanel != null) winPanel.SetActive(false);
            UpdateHUD();
        }

        private void UpdateHUD()
        {
            if (moveCountText != null)
                moveCountText.text = $"Hamle: {_moveCount}";
        }

        private void RecordUndoSnapshot()
        {
            if (_bottles == null || _gameHistoryService == null) return;
            var states = new BottleState[_bottles.Length];
            for (int i = 0; i < _bottles.Length; i++)
                states[i] = _bottles[i]?.State;
            _gameHistoryService.RecordSnapshot(states);
        }

        public void Undo()
        {
            if (_stateMachine == null || !_stateMachine.IsInState(GameState.Playing)) return;
            if (_gameHistoryService == null || !_gameHistoryService.CanUndo) return;

            _gameHistoryService.Undo();
            var snapshots = _gameHistoryService.LastSnapshot;
            if (snapshots == null || _bottles == null) return;

            for (int i = 0; i < snapshots.Length && i < _bottles.Length; i++)
            {
                if (_bottles[i] == null) continue;
                _bottles[i].State.ReplaceLayers(snapshots[i]);
                _bottles[i].UpdateVisualsFromState();
            }

            _moveCount = Mathf.Max(0, _moveCount - 1);
            UpdateHUD();
            BottleLogger.LogInfo($"Undo. Moves: {_moveCount}");
        }

        public void RestartGame()
        {
            BottleLogger.LogInfo("Restarting game.");
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}