using System;
using System.Collections.Generic;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Logging;
using PuzzleGame.Application.Configuration;
using PuzzleGame.Domain;
using PuzzleGame.Domain.Interfaces;
using PuzzleGame.Domain.Models;
using UnityEngine;

namespace PuzzleGame.Presentation
{
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

        private MoldBootstrapper _bootstrapper = new MoldBootstrapper();
        private ITweenService _tweenService;
        private Collider _cachedCollider;

        private void Awake()
        {
            _cachedCollider = GetComponent<Collider>();
            
            // Polish Task 1: Hitbox enlargement (Invisible padding)
            if (UnityEngine.Application.isPlaying && _cachedCollider != null)
            {
                if (_cachedCollider is BoxCollider box)
                {
                    var size = box.size;
                    box.size = new Vector3(size.x * 1.25f, size.y, size.z * 1.25f);
                }
                else if (_cachedCollider is CapsuleCollider capsule)
                {
                    capsule.radius *= 1.25f;
                }
            }

            if (_serializedLayers != null && _serializedLayers.Count > 0)
            {
                RestoreStateFromSerialized();
            }
        }

        public List<LevelLayerData> GetSerializedLayers() => _serializedLayers;

        public void Initialize(IRendererService rendererService,
                               IMoldValidator validator,
                               IAnimationService animationService,
                               List<OreLayer> initialLayers,
                               MoldVisualConfig visualConfigOverride = null,
                               ITweenService tweenService = null)
        {
            _tweenService = tweenService;
            _bootstrapper.Initialize(this, rendererService, validator, animationService, initialLayers, visualConfigOverride, tweenService);

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
            var meshGenerator = GetComponent<MoldMeshGenerator>();
            if (meshGenerator != null) meshGenerator.BuildMesh();
            _bootstrapper.Adapter?.UpdateVisuals();
        }
#endif

        public void RestoreStateFromSerialized(bool isFromOnValidate = false)
        {
            IRendererService rendererService = null;
            IMoldValidator validator = null;
            IAnimationService animationService = null;

#if UNITY_EDITOR
            if (!UnityEngine.Application.isPlaying)
            {
                rendererService = new PuzzleGame.Infrastructure.Implementations.RendererService();
                validator = new PuzzleGame.Domain.Services.MoldValidationService();
            }
#endif

            _bootstrapper.RestoreStateFromSerialized(this, rendererService, validator, animationService, isFromOnValidate);
        }

        private void OnDestroy()
        {
            _bootstrapper?.Dispose(this, _tweenService);
        }

        // ==========================================
        // IMoldView Delegation
        // ==========================================
        
        public MoldState State => _bootstrapper.Adapter?.State;
        public IReadOnlyList<OreLayer> VisualLayers => _bootstrapper.Adapter?.VisualLayers;
        public float VisualTotalFill => _bootstrapper.Adapter?.VisualTotalFill ?? 0f;
        public bool IsEmpty => _bootstrapper.Adapter?.IsEmpty ?? true;
        public bool IsFull() => _bootstrapper.Adapter?.IsFull() ?? false;
        public bool IsCapped => _bootstrapper.Adapter?.IsCapped ?? false;
        public Transform Transform => transform;
        public GameObject GameObject => gameObject;
        public Collider Collider => _cachedCollider;
        public float Height => _bootstrapper.Adapter?.Height ?? 2.4f;
        
        public int MoldIndex 
        { 
            get => _bootstrapper.Adapter?.MoldIndex ?? 0; 
            set { if (_bootstrapper.Adapter != null) _bootstrapper.Adapter.MoldIndex = value; } 
        }

        public void SetSelectionHighlight(bool active) => _bootstrapper.Adapter?.SetSelectionHighlight(active);
        public void AnimateCompletion() => _bootstrapper.Adapter?.AnimateCompletion();
        public void UpdateVisualsFromState() => _bootstrapper.Adapter?.UpdateVisualsFromState();
        public void SetVisualState(IReadOnlyList<OreLayer> layers, float totalFill) => _bootstrapper.Adapter?.SetVisualState(layers, totalFill);
        public void SetVisualCastProgress(LayerSnapshot startLayers, float t, bool isSource, OreLayer CastedLayer) => _bootstrapper.Adapter?.SetVisualCastProgress(startLayers, t, isSource, CastedLayer);
        public void PlaySettleBounce() => _bootstrapper.Adapter?.PlaySettleBounce();
        public void AddWobbleImpulse(Vector3 direction, float strength) => _bootstrapper.Adapter?.AddWobbleImpulse(direction, strength);
    }
}
