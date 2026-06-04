using System;
using System.Collections.Generic;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Domain;
using PuzzleGame.Domain.Interfaces;
using PuzzleGame.Domain.Models;
using PuzzleGame.Application.Logging;
using UnityEngine;
using PuzzleGame.Application.Configuration;
using PuzzleGame.Infrastructure;

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
        /// Pool-assigned index set by MoldPoolInitializer.
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
            if (_state == null || _state.MaxLayers != maxLayers)
            {
                _state = new MoldState(maxLayers);
            }
            else
            {
                _state.Clear();
            }
            _visualLayers.Clear();
            _serializedLayers.Clear();
            
            int initialCount = initialLayers.Count;
            for (int i = 0; i < initialCount; i++)
            {
                var layer = initialLayers[i];
                _state.AddLayer(layer);
                _visualLayers.Add(layer);
                _serializedLayers.Add(new LevelLayerData { color = ColorAdapter.ToUnityStatic(layer.Color), amount = layer.Amount });
            }
            _visualTotalFill = _state.TotalFill;

            MoldLogger.LogDebug($"Mold '{name}' initialized with {initialCount} layers.");
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

        private void RestoreStateFromSerialized(bool isFromOnValidate = false)
        {
#if UNITY_EDITOR
            if (!UnityEngine.Application.isPlaying)
            {
                if (_rendererService == null)
                {
                    // Fallback for editor validation to keep scene previews working outside playmode.
                    // Completely fully-qualified here to remove the top-level Dependency Inversion Principle violation.
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
                MoldLogger.LogWarning($"MoldController '{name}' initialized from serialized state without DI services. Editor preview mode may lack visuals/validation.");
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
            
            // Fix: Cork is initialized properly during state restores to avoid desync
            _corkController.EnsureCork(isFromOnValidate);
            corkObject = _corkController.CorkObject;

            int maxLayers = visualConfig != null ? visualConfig.maxLayers : ForgeConstants.DefaultLayerCapacity;
            _state = new MoldState(maxLayers);
            _visualLayers.Clear();
            if (_serializedLayers != null)
            {
                int count = _serializedLayers.Count;
                for (int i = 0; i < count; i++)
                {
                    var layerData = _serializedLayers[i];
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
            
            float startTotalFill = 0f;
            int startCount = startLayers.Count;
            for (int i = 0; i < startCount; i++)
            {
                startTotalFill += startLayers.Get(i).Amount;
            }

            if (isSource)
            {
                float totalVolumeToCast = Mathf.Max(0f, startTotalFill - State.TotalFill);
                float volumeToRemove = totalVolumeToCast * t;

                for (int i = 0; i < startCount; i++)
                {
                    _visualLayers.Add(startLayers.Get(i));
                }

                for (int i = _visualLayers.Count - 1; i >= 0 && volumeToRemove > 0f; i--)
                {
                    var layer = _visualLayers[i];
                    if (layer.Amount <= volumeToRemove)
                    {
                        volumeToRemove -= layer.Amount;
                        _visualLayers.RemoveAt(i);
                    }
                    else
                    {
                        _visualLayers[i] = layer.WithAmount(layer.Amount - volumeToRemove);
                        volumeToRemove = 0f;
                    }
                }

                float totalFill = 0f;
                for (int i = _visualLayers.Count - 1; i >= 0; i--)
                {
                    var layer = _visualLayers[i];
                    if (layer.Amount <= ForgeConstants.LayerAmountEpsilon)
                    {
                        _visualLayers.RemoveAt(i);
                    }
                    else
                    {
                        totalFill += layer.Amount;
                    }
                }
                _visualTotalFill = totalFill;
            }
            else
            {
                float totalVolumeToCast = Mathf.Max(0f, State.TotalFill - startTotalFill);
                float volumeToAdd = totalVolumeToCast * t;

                float totalFill = 0f;
                for (int i = 0; i < startCount; i++)
                {
                    var layer = startLayers.Get(i);
                    _visualLayers.Add(layer);
                    totalFill += layer.Amount;
                }

                if (volumeToAdd > ForgeConstants.LayerAmountEpsilon)
                {
                    _visualLayers.Add(CastedLayer.WithAmount(volumeToAdd));
                    totalFill += volumeToAdd;
                }
                _visualTotalFill = totalFill;
            }

            UpdateVisuals();
        }

        public void UpdateVisualsFromState()
        {
            if (State == null) return;
            _visualLayers.Clear();
            
            // Fix: Loop directly on IReadOnlyList using standard for loop to avoid IEnumerable boxing allocations
            var layers = State.Layers;
            int count = layers.Count;
            for (int i = 0; i < count; i++)
            {
                _visualLayers.Add(layers[i]);
            }
            _visualTotalFill = State.TotalFill;
            _serializedLayers.Clear();
            for (int i = 0; i < count; i++)
            {
                var layer = layers[i];
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
            RestoreStateFromSerialized(true);
            if (_meshGenerator != null)
            {
                _meshGenerator.BuildMesh();
            }
            UpdateVisuals();
        }
#endif

        private void OnDestroy()
        {
#if PRIME_TWEEN_INSTALLED
            PrimeTween.Tween.StopAll(transform);
            if (corkObject != null) PrimeTween.Tween.StopAll(corkObject.transform);
#endif

            _corkController?.DisposeResources();

            // Fix: Do NOT destroy glassMaterial or OreMaterial because they are assets from the database.
            // Destroying references here destroys project assets!
            // Do NOT call Destroy(lr.sharedMaterial) either, as that ruins the original shared material.

            _visualRenderer = null;
        }
    }
}
