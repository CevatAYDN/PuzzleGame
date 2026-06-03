using System;
using System.Collections.Generic;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Domain;
using PuzzleGame.Domain.Interfaces;
using PuzzleGame.Domain.Models;
using PuzzleGame.Infrastructure.Interfaces;
using PuzzleGame.Logging;
using UnityEngine;

namespace PuzzleGame
{
    [RequireComponent(typeof(Renderer))]
    [RequireComponent(typeof(Wobble))]
    public class BottleController : MonoBehaviour, IBottleView
    {
        [Header("Materials (assigned by BottleMeshGenerator or Editor tool)")]
        public Material glassMaterial;
        public Material liquidMaterial;

        [Header("Configuration")]
        public Configuration.BottleVisualConfig visualConfig;

        public GameObject corkObject;

        public BottleState State { get; private set; }
        public IReadOnlyList<LiquidLayer> VisualLayers => _visualLayers;
        public float VisualTotalFill => _visualTotalFill;
        public float Height => _meshGenerator != null ? _meshGenerator.height : BottleConstants.DefaultBottleHeight;
        public bool IsCapped => _corkController != null && _corkController.IsCapped;
        public Transform Transform => transform;
        public GameObject GameObject => gameObject;

        private readonly List<LiquidLayer> _visualLayers = new List<LiquidLayer>();
        private float _visualTotalFill = 0f;

        private IRendererService _rendererService;
        private IBottleValidator _validator;
        private IAnimationService _animationService;
        private BottleVisualRenderer _visualRenderer;
        private BottleCorkController _corkController;
        private BottleMeshGenerator _meshGenerator;
        private Renderer _renderer;
        private Wobble _wobble;

        public void Initialize(IRendererService rendererService,
                               IBottleValidator  validator,
                               IAnimationService animationService,
                               List<LiquidLayer> initialLayers)
        {
            if (rendererService == null) throw new ArgumentNullException(nameof(rendererService));
            if (validator == null)       throw new ArgumentNullException(nameof(validator));
            if (animationService == null) throw new ArgumentNullException(nameof(animationService));
            if (initialLayers == null)    throw new ArgumentNullException(nameof(initialLayers));

            _rendererService = rendererService;
            _validator       = validator;
            _animationService = animationService;
            _meshGenerator   = GetComponent<BottleMeshGenerator>();
            _renderer        = GetComponent<Renderer>();
            _wobble          = GetComponent<Wobble>();

            _visualRenderer = new BottleVisualRenderer(
                _renderer, _rendererService, visualConfig,
                () => _visualLayers, () => _visualTotalFill);

            _corkController = new BottleCorkController(
                transform, _animationService,
                () => Height,
                () => _meshGenerator != null ? _meshGenerator.neckRadius : BottleConstants.CorkRadius,
                corkObject);
            _corkController.EnsureCork();
            corkObject = _corkController.CorkObject;
            SetSelectionHighlight(false);

            int maxLayers = visualConfig != null ? visualConfig.maxLayers : BottleConstants.DefaultLayerCapacity;
            State = new BottleState(maxLayers);
            _visualLayers.Clear();
            foreach (var layer in initialLayers)
            {
                State.AddLayer(layer);
                _visualLayers.Add(layer);
            }
            _visualTotalFill = State.TotalFill;

            BottleLogger.LogDebug($"Bottle '{name}' initialized with {initialLayers.Count} layers.");
            UpdateVisuals();
        }

        public bool IsEmpty => State?.IsEmpty ?? true;
        public bool IsFull()  => State?.IsFull  ?? false;

        public bool HasSingleColorContent()
        {
            if (State == null || State.IsEmpty || State.Layers.Count == 0) return true;
            var firstColor = State.Layers[0].Color;
            for (int i = 1; i < State.Layers.Count; i++)
                if (!_validator.ColorsMatch(State.Layers[i].Color, firstColor)) return false;
            return true;
        }

        public void AddWobbleImpulse(Vector3 direction, float strength)
        {
            _wobble?.AddImpulse(direction, strength);
        }

        public bool TryPourTo(IBottleView target)
        {
            if (target == null)
            {
                BottleLogger.LogWarning($"'{name}': TryPourTo called with null target.");
                return false;
            }

            if (!_validator.CanPour(State, target.State))
            {
                BottleLogger.LogDebug($"'{name}' → '{target.GameObject.name}': pour rejected by validator.");
                return false;
            }

            LiquidLayer layer;
            try
            {
                layer = State.PopTopLayer();
            }
            catch (InvalidOperationException ex)
            {
                throw new InvalidOperationException(
                    $"Bottle '{name}' invariant violated: validator approved pour but bottle is empty.", ex);
            }

            try
            {
                target.State.AddLayer(layer);
            }
            catch (InvalidOperationException ex)
            {
                State.AddLayer(layer);
                throw new InvalidOperationException(
                    $"Bottle '{name}' → '{target.GameObject.name}': AddLayer threw after validator approval. Rolled back.",
                    ex);
            }

            float impulse = visualConfig != null
                ? visualConfig.pourImpulseStrength
                : BottleConstants.DefaultPourImpulseStrength;
            Vector3 pourDirection = (target.Transform.position - transform.position).normalized;
            _wobble?.AddImpulse(-pourDirection, impulse);
            target.AddWobbleImpulse(pourDirection, impulse * BottleConstants.WobbleTargetMultiplier);

            BottleLogger.LogInfo($"Poured {layer.Color} from '{name}' to '{target.GameObject.name}'.");
            return true;
        }

        public void SetVisualState(List<LiquidLayer> layers, float totalFill)
        {
            _visualLayers.Clear();
            _visualLayers.AddRange(layers);
            _visualTotalFill = totalFill;
            UpdateVisuals();
        }

        public void SetVisualPourProgress(LayerSnapshot startLayers, float t, bool isSource, LiquidLayer pouredLayer)
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
                    if (layer.Amount > BottleConstants.LayerAmountEpsilon)
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
                var extra = pouredLayer.WithAmount(pouredLayer.Amount * t);
                if (extra.Amount > BottleConstants.LayerAmountEpsilon)
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
            UpdateVisuals();
        }

        public void UpdateVisuals()
        {
            if (_visualRenderer == null)
            {
                BottleLogger.LogWarning($"'{name}': UpdateVisuals called before Initialize.");
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

            int liquidIndex = visualConfig != null ? visualConfig.liquidMaterialIndex : BottleConstants.DefaultLiquidMaterialIndex;
            if (_renderer != null && _renderer.sharedMaterials.Length > liquidIndex && _animationService != null)
            {
                _animationService.AnimateLiquidFlash(
                    _renderer, liquidIndex,
                    BottleConstants.CompletionFlashIntensity,
                    BottleConstants.CompletionFlashDuration,
                    onComplete: null);
            }
        }

        public void PlaySettleBounce()
        {
            _animationService?.AnimateSettleBounce(this, BottleConstants.SettleBounceDuration, onComplete: null);
        }

        private void OnDestroy()
        {
            _corkController?.DisposeResources();
        }
    }
}
