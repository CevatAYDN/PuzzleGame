using System;
using System.Collections.Generic;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Domain;
using PuzzleGame.Domain.Interfaces;
using PuzzleGame.Domain.Models;
using PuzzleGame.Domain.Services;
using PuzzleGame.Application.Logging;
using UnityEngine;
using PuzzleGame.Application.Configuration;
using PuzzleGame.Infrastructure;
using PuzzleGame.Infrastructure.Implementations;

namespace PuzzleGame
{
    [RequireComponent(typeof(Renderer))]
    [RequireComponent(typeof(Wobble))]
    public class MoldController : MonoBehaviour, IMoldView
    {
        [Header("Materials (assigned by MoldMeshGenerator or Editor tool)")]
        public Material glassMaterial;
        public Material OreMaterial;

        [Header("Configuration")]
        public Application.Configuration.MoldVisualConfig visualConfig;

        public GameObject corkObject;

        [SerializeField] private List<LevelLayerData> _serializedLayers = new List<LevelLayerData>();

        public MoldState State
        {
            get
            {
                if (_state == null)
                {
                    RestoreStateFromSerialized();
                }
                return _state;
            }
            private set => _state = value;
        }
        private MoldState _state;

        public IReadOnlyList<OreLayer> VisualLayers => _visualLayers;
        public float VisualTotalFill => _visualTotalFill;
        public float Height => _meshGenerator != null ? _meshGenerator.height : visualConfig != null ? visualConfig.moldHeight : 2.4f;
        public bool IsCapped => _corkController != null && _corkController.IsCapped;
        public Transform Transform => transform;
        public GameObject GameObject => gameObject;

        /// <summary>
        /// Fix #14: Pool-assigned index set by MoldPoolInitializer.
        /// Replaces the fragile GameObject.name-parsing approach in CastService.
        /// </summary>
        public int MoldIndex { get; set; }

        private readonly List<OreLayer> _visualLayers = new List<OreLayer>();
        private float _visualTotalFill = 0f;

        private IRendererService _rendererService;
        private IMoldValidator _validator;
        private IAnimationService _animationService;
        private MoldVisualRenderer _visualRenderer;
        private MoldCorkController _corkController;
        private MoldMeshGenerator _meshGenerator;
        private Renderer _renderer;
        private Wobble _wobble;

        public void Initialize(IRendererService rendererService,
                               IMoldValidator  validator,
                               IAnimationService animationService,
                               List<OreLayer> initialLayers)
        {
            if (rendererService == null) throw new ArgumentNullException(nameof(rendererService));
            if (validator == null)       throw new ArgumentNullException(nameof(validator));
            if (initialLayers == null)    throw new ArgumentNullException(nameof(initialLayers));

            _rendererService = rendererService;
            _validator       = validator;
            _animationService = animationService;
            _meshGenerator   = GetComponent<MoldMeshGenerator>();
            _renderer        = GetComponent<Renderer>();
            _wobble          = GetComponent<Wobble>();

            _visualRenderer = new MoldVisualRenderer(
                _renderer, _rendererService, visualConfig,
                () => _visualLayers, () => _visualTotalFill);

            _corkController = new MoldCorkController(
                transform, _animationService,
                () => Height,
                () => _meshGenerator != null ? _meshGenerator.neckRadius : PuzzleGame.Infrastructure.CorkConstants.Radius,
                corkObject);
            _corkController.EnsureCork();
            corkObject = _corkController.CorkObject;
            SetSelectionHighlight(false);

            int maxLayers = visualConfig != null ? visualConfig.maxLayers : ForgeConstants.DefaultLayerCapacity;
            _state = new MoldState(maxLayers);
            _visualLayers.Clear();
            _serializedLayers.Clear();
            foreach (var layer in initialLayers)
            {
                _state.AddLayer(layer);
                _visualLayers.Add(layer);
                _serializedLayers.Add(new LevelLayerData { color = ColorAdapter.ToUnityStatic(layer.Color), amount = layer.Amount });
            }
            _visualTotalFill = _state.TotalFill;

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

        private void RestoreStateFromSerialized()
        {
            // FIX: Removed runtime reflection. Services should be injected via Initialize() or set via property.
            // If not injected, create default implementations directly (non-DI fallback for Editor).
            if (_rendererService == null)
            {
                _rendererService = new RendererService();
            }
            if (_validator == null)
            {
                _validator = new MoldValidationService();
            }

            _meshGenerator = GetComponent<MoldMeshGenerator>();
            _renderer = GetComponent<Renderer>();
            _wobble = GetComponent<Wobble>();

            _visualRenderer = new MoldVisualRenderer(
                _renderer, _rendererService, visualConfig,
                () => _visualLayers, () => _visualTotalFill);

            _corkController = new MoldCorkController(
                transform, _animationService,
                () => Height,
                () => _meshGenerator != null ? _meshGenerator.neckRadius : PuzzleGame.Infrastructure.CorkConstants.Radius,
                corkObject);

            int maxLayers = visualConfig != null ? visualConfig.maxLayers : ForgeConstants.DefaultLayerCapacity;
            _state = new MoldState(maxLayers);
            _visualLayers.Clear();
            if (_serializedLayers != null)
            {
                foreach (var layerData in _serializedLayers)
                {
                    var layer = new OreLayer(ColorAdapter.FromUnityStatic(layerData.color), layerData.amount);
                    _state.AddLayer(layer);
                    _visualLayers.Add(layer);
                }
            }
            _visualTotalFill = _state.TotalFill;
        }

        // Property-based injection for non-DI scenarios (Editor preview)
        public IRendererService RendererService
        {
            set => _rendererService = value;
        }

        public IMoldValidator MoldValidator
        {
            set => _validator = value;
        }
 
        public bool IsEmpty => State?.IsEmpty ?? true;
        public bool IsFull()  => State?.IsFull  ?? false;
 
        public void AddWobbleImpulse(Vector3 direction, float strength)
        {
            _wobble?.AddImpulse(direction, strength);
        }

        public void SetVisualState(IReadOnlyList<OreLayer> layers, float totalFill)
        {
            _visualLayers.Clear();
            _serializedLayers.Clear();
            if (layers != null)
            {
                int count = layers.Count;
                for (int i = 0; i < count; i++)
                {
                    _visualLayers.Add(layers[i]);
                    _serializedLayers.Add(new LevelLayerData { color = ColorAdapter.ToUnityStatic(layers[i].Color), amount = layers[i].Amount });
                }
            }
            _visualTotalFill = totalFill;
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
            _visualLayers.Clear();
            float totalFill = 0f;
            if (isSource)
            {
                int count = startLayers.Count;
                for (int i = 0; i < count; i++)
                {
                    var layer = startLayers.Get(i);
                    if (i == count - 1)
                    {
                        layer = layer.WithAmount(layer.Amount * (1f - t));
                    }
                    if (layer.Amount > ForgeConstants.LayerAmountEpsilon)
                    {
                        _visualLayers.Add(layer);
                        totalFill += layer.Amount;
                    }
                }
            }
            else
            {
                int count = startLayers.Count;
                for (int i = 0; i < count; i++)
                {
                    var layer = startLayers.Get(i);
                    _visualLayers.Add(layer);
                    totalFill += layer.Amount;
                }
                var extra = CastedLayer.WithAmount(CastedLayer.Amount * t);
                if (extra.Amount > ForgeConstants.LayerAmountEpsilon)
                {
                    _visualLayers.Add(extra);
                    totalFill += extra.Amount;
                }
            }
            _visualTotalFill = totalFill;
            UpdateVisuals();
        }

        public void UpdateVisualsFromState()
        {
            if (State == null) return;
            _visualLayers.Clear();
            _visualLayers.AddRange(State.Layers);
            _visualTotalFill = State.TotalFill;
            _serializedLayers.Clear();
            foreach (var layer in State.Layers)
            {
                _serializedLayers.Add(new LevelLayerData { color = ColorAdapter.ToUnityStatic(layer.Color), amount = layer.Amount });
            }
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
            if (IsCapped) return;
            _corkController?.AnimateDrop();

            int oreIndex = visualConfig != null ? visualConfig.oreMaterialIndex : 1;
            if (_renderer != null && _renderer.sharedMaterials.Length > oreIndex && _animationService != null)
            {
                float intensity = visualConfig != null ? visualConfig.completionFlashIntensity : 4.0f;
                float duration = visualConfig != null ? visualConfig.completionFlashDuration : 0.6f;
                _animationService.AnimateOreFlash(
                    _renderer, oreIndex,
                    intensity,
                    duration,
                    onComplete: null);
            }
        }

        public void PlaySettleBounce()
        {
            float duration = visualConfig != null ? visualConfig.settleBounceDuration : 0.6f;
            _animationService?.AnimateSettleBounce(this, duration, onComplete: null);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (UnityEngine.Application.isPlaying) return;
            RestoreStateFromSerialized();
            UpdateVisuals();
        }
#endif

        private void OnDestroy()
        {
            _corkController?.DisposeResources();

            // FIX: Complete material disposal to prevent memory leaks
            var lr = GetComponent<LineRenderer>();
            if (lr != null && lr.sharedMaterial != null)
            {
                Destroy(lr.sharedMaterial);
            }

            // Dispose instantiated materials (not shared)
            if (glassMaterial != null && !ReferenceEquals(glassMaterial, null))
            {
                Destroy(glassMaterial);
            }
            if (OreMaterial != null && !ReferenceEquals(OreMaterial, null))
            {
                Destroy(OreMaterial);
            }

            // Dispose visual renderer resources (MaterialPropertyBlock is value type, no explicit dispose needed)
            _visualRenderer = null;
        }
    }
}
