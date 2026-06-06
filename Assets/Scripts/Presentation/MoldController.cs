using System;
using System.Collections.Generic;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Logging;
using PuzzleGame.Application.Configuration;
using PuzzleGame.Domain;
using PuzzleGame.Domain.Interfaces;
using PuzzleGame.Domain.Models;
using UnityEngine;

namespace PuzzleGame
{
    /// <summary>
    /// MonoBehaviour facade for a single Mold. Composes 3 focused POCOs:
    ///   - <see cref="MoldStateManager"/>: state lifecycle + editor preview
    ///   - <see cref="MoldVisualSync"/>: visual layer list + cast progress
    ///   - <see cref="MoldAnimator"/>: completion flash, settle bounce, wobble
    /// Plus pre-extracted <see cref="MoldVisualRenderer"/> (material/highlight) and
    /// <see cref="MoldCorkController"/> (cork lifecycle).
    /// SRP: this class only orchestrates component lifecycle and exposes IMoldView.
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    [RequireComponent(typeof(Wobble))]
    public class MoldController : MonoBehaviour, IMoldView
    {
        [Header("Materials (assigned by MoldMeshGenerator or Editor tool)")]
        public Material glassMaterial;
        public Material OreMaterial;

        [Header("Configuration")]
        public MoldVisualConfig visualConfig;

        public GameObject corkObject;

        [Header("Optional Target Settings")]
        public bool isOptionalTarget = false;

        [SerializeField] private List<LevelLayerData> _serializedLayers = new List<LevelLayerData>();

        public int MoldIndex { get; set; }

        public MoldState State => _stateManager?.State;
        public IReadOnlyList<OreLayer> VisualLayers => _visualSync?.VisualLayers;
        public float VisualTotalFill => _visualSync?.VisualTotalFill ?? 0f;
        public bool IsEmpty => State?.IsEmpty ?? true;
        public bool IsFull() => State?.IsFull ?? false;
        public bool IsCapped => _corkController != null && _corkController.IsCapped;
        public Transform Transform => transform;
        public GameObject GameObject => gameObject;
        public float Height => _meshGenerator != null
            ? _meshGenerator.height
            : visualConfig != null
                ? visualConfig.moldHeight
                : 2.4f;

        private MoldStateManager _stateManager;
        private MoldVisualSync _visualSync;
        private MoldAnimator _animator;
        private MoldVisualRenderer _visualRenderer;
        private MoldCorkController _corkController;
        private MoldMeshGenerator _meshGenerator;
        private Renderer _renderer;
        private Wobble _wobble;

        private IRendererService _rendererService;
        private IMoldValidator _validator;
        private IAnimationService _animationService;
        private ITweenService _tweenService;

        private void Awake()
        {
            if (_stateManager == null && _serializedLayers != null && _serializedLayers.Count > 0)
            {
                RestoreStateFromSerialized();
            }
        }

        public void Initialize(IRendererService rendererService,
                               IMoldValidator validator,
                               IAnimationService animationService,
                               List<OreLayer> initialLayers,
                               MoldVisualConfig visualConfigOverride = null,
                               ITweenService tweenService = null)
        {
            if (rendererService == null) throw new ArgumentNullException(nameof(rendererService));
            if (validator == null) throw new ArgumentNullException(nameof(validator));
            if (initialLayers == null) throw new ArgumentNullException(nameof(initialLayers));

            _rendererService = rendererService;
            _validator = validator;
            _animationService = animationService;
            _tweenService = tweenService;

            _meshGenerator = GetComponent<MoldMeshGenerator>();
            _renderer = GetComponent<Renderer>();
            _wobble = GetComponent<Wobble>();

            if (visualConfig == null)
            {
                visualConfig = visualConfigOverride;
                if (visualConfig == null)
                {
                    visualConfig = Resources.Load<MoldVisualConfig>("Data/MoldVisualConfig");
                    if (visualConfig == null)
                    {
                        MoldLogger.LogWarning(
                            "[MoldController] MoldVisualConfig not injected on " +
                            gameObject.name + ". And default not found in 'Resources/Data/MoldVisualConfig'. Using fallback SO.",
                            this);
                        visualConfig = ScriptableObject.CreateInstance<MoldVisualConfig>();
                        visualConfig.name = "MoldVisualConfig_Fallback";
                    }
                }
            }
            if (_renderer == null)
            {
                MoldLogger.LogError(
                    "[MoldController] Renderer component missing on " + gameObject.name + ". " +
                    "Visual updates will not apply. Ensure Renderer is attached to the Prefab.",
                    this);
            }
            if (_meshGenerator == null)
            {
                MoldLogger.LogWarning(
                    "[MoldController] MoldMeshGenerator missing on " + gameObject.name + ". " +
                    "Height calculations will rely on config default.");
            }

            int maxLayers = visualConfig != null ? visualConfig.maxLayers : ForgeConstants.DefaultLayerCapacity;

            _stateManager = new MoldStateManager(_serializedLayers);
            _stateManager.Initialize(maxLayers, initialLayers);

            _visualSync = new MoldVisualSync();
            _visualSync.BindStateTotalFillProvider(() => _stateManager.State?.TotalFill ?? 0f);

            _visualRenderer = new MoldVisualRenderer(
                _renderer, _rendererService, visualConfig,
                () => _visualSync != null ? (List<OreLayer>)_visualSync.VisualLayers : new List<OreLayer>(),
                () => _visualSync != null ? _visualSync.VisualTotalFill : 0f);

            _corkController = new MoldCorkController(
                transform, _animationService,
                () => Height,
                () => _meshGenerator != null ? _meshGenerator.neckRadius : PuzzleGame.Infrastructure.CorkConstants.Radius,
                corkObject);
            _corkController.EnsureCork();
            corkObject = _corkController.CorkObject;

            _animator = new MoldAnimator(_renderer, _animationService, (PuzzleGame.Wobble)_wobble, (PuzzleGame.Application.Configuration.MoldVisualConfig)visualConfig, (PuzzleGame.MoldCorkController)_corkController);

            _visualSync.CopyFromState(_stateManager.State);
            SetSelectionHighlight(false);

            MoldLogger.LogDebug($"Mold '{name}' initialized with {initialLayers.Count} layers.");
            UpdateVisuals();

#if UNITY_EDITOR
            if (!UnityEngine.Application.isPlaying)
            {
                UnityEditor.EditorUtility.SetDirty(this);
                if (gameObject.scene.IsValid())
                {
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
                }
            }
#endif
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (UnityEngine.Application.isPlaying) return;
            RestoreStateFromSerialized(true);
            if (_meshGenerator != null) _meshGenerator.BuildMesh();
            UpdateVisuals();
        }
#endif

        public void RestoreStateFromSerialized(bool isFromOnValidate = false)
        {
#if UNITY_EDITOR
            if (!UnityEngine.Application.isPlaying)
            {
                if (_rendererService == null)
                {
                    _rendererService = new PuzzleGame.Infrastructure.Implementations.RendererService();
                }
                if (_validator == null)
                {
                    _validator = new PuzzleGame.Domain.Services.MoldValidationService();
                }
            }
#endif

            if (_rendererService == null || _validator == null)
            {
#if UNITY_EDITOR
                if (!UnityEngine.Application.isPlaying)
                {
                    MoldLogger.LogDebug($"MoldController '{name}' editor preview — DI services not available; visuals/validation skipped until play mode.");
                }
#endif
            }

            _meshGenerator = GetComponent<MoldMeshGenerator>();
            _renderer = GetComponent<Renderer>();
            _wobble = GetComponent<Wobble>();

            if (visualConfig == null)
            {
                visualConfig = Resources.Load<MoldVisualConfig>("Data/MoldVisualConfig");
            }
            int maxLayers = visualConfig != null ? visualConfig.maxLayers : ForgeConstants.DefaultLayerCapacity;
            if (_stateManager == null) _stateManager = new MoldStateManager(_serializedLayers);
            _stateManager.Initialize(maxLayers, _stateManager.RebuildFromSerialized());

            if (_visualSync == null) _visualSync = new MoldVisualSync();
            _visualSync.BindStateTotalFillProvider(() => _stateManager.State?.TotalFill ?? 0f);

            if (_corkController == null)
            {
                _corkController = new MoldCorkController(
                    transform, _animationService,
                    () => Height,
                    () => _meshGenerator != null ? _meshGenerator.neckRadius : PuzzleGame.Infrastructure.CorkConstants.Radius,
                    corkObject);
            }
            _corkController.EnsureCork(isFromOnValidate);
            corkObject = _corkController.CorkObject;

            if (_visualRenderer == null && _renderer != null)
            {
                _visualRenderer = new MoldVisualRenderer(
                    _renderer, _rendererService, visualConfig,
                    () => _visualSync != null ? (List<OreLayer>)_visualSync.VisualLayers : new List<OreLayer>(),
                    () => _visualSync != null ? _visualSync.VisualTotalFill : 0f);
            }

            _visualSync.CopyFromState(_stateManager.State);
            _visualRenderer?.Update();
        }

        public IRendererService RendererService { set => _rendererService = value; }
        public IMoldValidator MoldValidator { set => _validator = value; }

        public void AddWobbleImpulse(Vector3 direction, float strength)
        {
            _animator?.AddWobbleImpulse(direction, strength);
        }

        public void SetVisualState(IReadOnlyList<OreLayer> layers, float totalFill)
        {
            _visualSync?.SetVisualState(layers, totalFill);
            _stateManager?.SyncSerializedFromLayers(layers);
            UpdateVisuals();

#if UNITY_EDITOR
            if (!UnityEngine.Application.isPlaying)
            {
                UnityEditor.EditorUtility.SetDirty(this);
                if (gameObject.scene.IsValid())
                {
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
                }
            }
#endif
        }

        public void SetVisualCastProgress(LayerSnapshot startLayers, float t, bool isSource, OreLayer CastedLayer)
        {
            _visualSync?.SetVisualCastProgress(startLayers, t, isSource, CastedLayer);
            UpdateVisuals();
        }

        public void UpdateVisualsFromState()
        {
            if (State == null) return;
            _visualSync?.CopyFromState(State);
            _stateManager?.SyncSerializedFromLayers(_visualSync.VisualLayers);
            UpdateVisuals();

#if UNITY_EDITOR
            if (!UnityEngine.Application.isPlaying)
            {
                UnityEditor.EditorUtility.SetDirty(this);
                if (gameObject.scene.IsValid())
                {
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
                }
            }
#endif
        }

        public void UpdateVisuals()
        {
            if (_visualRenderer == null)
            {
                MoldLogger.LogWarning($"'{name}': UpdateVisuals called before Initialize.");
                return;
            }
            _visualRenderer.Update();
        }

        public void SetSelectionHighlight(bool active)
        {
            _visualRenderer?.SetSelectionHighlight(active);
        }

        public void AnimateCompletion()
        {
            _animator?.AnimateCompletion();
        }

        public void PlaySettleBounce()
        {
            _animator?.PlaySettleBounce(this);
        }

        private void OnDestroy()
        {
            _tweenService?.StopAll(transform);
            if (corkObject != null) _tweenService?.StopAll(corkObject.transform);
            _corkController?.DisposeResources();
            _visualRenderer = null;
        }
    }
}
