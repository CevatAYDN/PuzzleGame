using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using BottleShaders.Domain.Models;
using BottleShaders.Domain.Services;
using BottleShaders.Domain.Interfaces;
using BottleShaders.Application.Services;
using BottleShaders.Application.Interfaces;
using BottleShaders.Infrastructure.Interfaces;
using BottleShaders.Infrastructure.Implementations;
using BottleShaders.Events;
using BottleShaders.Logging;
using BottleShaders.Configuration;

namespace BottleShaders
{
    public class GameManager : MonoBehaviour, IUpdateable
    {
        [Header("Configuration (assign via Resources or Inspector)")]
        [SerializeField] private GameConfig     gameConfig;
        [SerializeField] private AnimationConfig animConfig;

        [Header("Level Generation")]
        [SerializeField] private bool  autoGenerateLevel = true;
        [SerializeField] private int   emptyBottleCount  = 2;
        [SerializeField] private int   randomSeed        = 0;
        [SerializeField] private Color[] palette = new Color[]
        {
            new Color(0.95f, 0.20f, 0.25f),
            new Color(0.20f, 0.55f, 0.95f),
            new Color(0.30f, 0.85f, 0.35f),
            new Color(0.98f, 0.80f, 0.15f),
            new Color(0.70f, 0.30f, 0.90f),
            new Color(0.95f, 0.50f, 0.15f),
        };

        [Header("HUD (optional)")]
        [SerializeField] private Canvas    hudCanvas;
        [SerializeField] private TMPro.TextMeshProUGUI moveCountText;
        [SerializeField] private GameObject winPanel;

        private IBottleValidator      _validator;
        private IRendererService      _rendererService;
        private IAnimationService     _animationService;
        private IBottleSelectionService _selectionService;
        private IInputHandler         _inputHandler;

        private BottleController[] _bottles;
        private Camera             _mainCam;

        private bool    _gameWon;
        private int     _moveCount;
        private Vector3 _selectedOriginalPos;

        private Action<BottleState> _onBottleSelectedHandler;
        private Action<BottleState> _onBottleDeselectedHandler;

        private static readonly WaitForSeconds WinCheckDelay = new WaitForSeconds(0.5f);
        private static readonly Color CamDefaultBgColor = new Color(0.08f, 0.05f, 0.16f, 1f);

        private void Awake()
        {
            BottleLogger.LogInfo("GameManager Awake — composing services.");
            ComposeServices();
        }

        private void Start()
        {
            BottleLogger.LogInfo("GameManager Start — setting up scene.");
            CacheBottles();
            SetupBottles();
            InitHUD();
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
            if (_gameWon || _animationService.IsAnimating) return;
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

            List<List<LiquidLayer>> assignments = autoGenerateLevel
                ? GenerateLevel(_bottles.Length, gameConfig.maxLayersPerBottle)
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

        private List<List<LiquidLayer>> GenerateLevel(int bottleCount, int maxLayers)
        {
            var result = new List<List<LiquidLayer>>(bottleCount);
            for (int i = 0; i < bottleCount; i++)
                result.Add(new List<LiquidLayer>());

            int empties     = Mathf.Clamp(emptyBottleCount, 1, bottleCount - 1);
            int filledCount = bottleCount - empties;
            int numColors   = Mathf.Min(filledCount, palette.Length);

            if (numColors < 1)
            {
                BottleLogger.LogWarning("Level generation: not enough bottles or palette colors.");
                return result;
            }

            var pool = new List<Color>(numColors * maxLayers);
            for (int c = 0; c < numColors; c++)
                for (int k = 0; k < maxLayers; k++)
                    pool.Add(palette[c]);

            var rng = randomSeed == 0 ? new System.Random() : new System.Random(randomSeed);
            for (int i = pool.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (pool[i], pool[j]) = (pool[j], pool[i]);
            }

            float amountPerLayer = 1f / maxLayers;
            int   idx            = 0;

            for (int b = 0; b < numColors; b++)
                for (int k = 0; k < maxLayers; k++)
                    result[b].Add(new LiquidLayer(pool[idx++], amountPerLayer));

            BottleLogger.LogInfo($"Level generated: {numColors} colors, {filledCount} filled, {empties} empty.");
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
                _gameWon = true;
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

        public void RestartGame()
        {
            BottleLogger.LogInfo("Restarting game.");
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}