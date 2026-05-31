using UnityEngine;
using BottleShaders.Infrastructure.Interfaces;
using BottleShaders.Domain.Models;
using BottleShaders.Domain.Interfaces;
using BottleShaders.Logging;
using System.Collections.Generic;

namespace BottleShaders
{
    /// <summary>
    /// MonoBehaviour façade for a single bottle.
    /// Owns the visual representation; delegates all game-rule decisions
    /// to the injected <see cref="IBottleValidator"/>.
    ///
    /// Responsibilities:
    ///   • Hold the domain state (<see cref="BottleState"/>)
    ///   • Trigger visual updates via <see cref="IRendererService"/>
    ///   • Execute a pour when the caller has already validated it
    ///
    /// NOT responsible for:
    ///   • Deciding whether a pour is legal  → IBottleValidator
    ///   • Selecting / deselecting bottles   → IBottleSelectionService
    ///   • Animating the bottle              → IAnimationService
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public class BottleController : MonoBehaviour
    {
        // ── Inspector ────────────────────────────────────────────────────────

        [Header("Materials (assigned by BottleMeshGenerator or Editor tool)")]
        public Material glassMaterial;
        public Material liquidMaterial;

        [Header("Visual Tuning")]
        [SerializeField] private float saturationBoost = 1.35f;
        [SerializeField] private float brightnessBoost = 1.2f;

        [Header("Bottle Capacity")]
        [SerializeField] private int maxLayers = 4;

        // ── Runtime state ────────────────────────────────────────────────────

        public BottleState State { get; private set; }

        private IRendererService  _rendererService;
        private IBottleValidator  _validator;
        private Renderer          _renderer;

        // ── Initialisation ───────────────────────────────────────────────────

        /// <summary>
        /// Must be called once before the bottle is used.
        /// Replaces Unity's Awake/Start for dependency injection.
        /// </summary>
        public void Initialize(IRendererService rendererService,
                               IBottleValidator  validator,
                               List<LiquidLayer> initialLayers)
        {
            _rendererService = rendererService;
            _validator       = validator;
            _renderer        = GetComponent<Renderer>();

            State = new BottleState(maxLayers);
            foreach (var layer in initialLayers)
                State.AddLayer(layer);

            BottleLogger.LogDebug($"Bottle '{name}' initialized with {initialLayers.Count} layers.");
            UpdateVisuals();
        }

        // ── Queries ──────────────────────────────────────────────────────────

        public bool IsEmpty() => State?.IsEmpty ?? true;
        public bool IsFull()  => State?.IsFull  ?? false;

        public bool HasSingleColorContent()
        {
            if (State == null || State.IsEmpty) return true;
            var first = State.Layers[0].Color;
            foreach (var layer in State.Layers)
                if (!_validator.ColorsMatch(layer.Color, first)) return false;
            return true;
        }

        // ── Pour ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Attempts to pour the top layer of this bottle into <paramref name="target"/>.
        /// Returns false without side-effects when the move is illegal.
        /// </summary>
        public bool TryPourTo(BottleController target)
        {
            if (target == null)
            {
                BottleLogger.LogWarning($"'{name}': TryPourTo called with null target.");
                return false;
            }

            if (!_validator.CanPour(State, target.State))
            {
                BottleLogger.LogDebug($"'{name}' → '{target.name}': pour rejected by validator.");
                return false;
            }

            var layer = State.PopTopLayer();
            if (layer == null)
            {
                BottleLogger.LogError($"'{name}': validator allowed pour but PopTopLayer returned null.");
                return false;
            }

            bool added = target.State.AddLayer(layer.Value);
            if (!added)
            {
                // Validator said OK but AddLayer failed — roll back to keep state consistent
                State.AddLayer(layer.Value);
                BottleLogger.LogError($"'{name}' → '{target.name}': AddLayer failed after validator approval. Rolled back.");
                return false;
            }

            BottleLogger.LogInfo($"Poured {layer.Value.Color} from '{name}' to '{target.name}'.");
            UpdateVisuals();
            target.UpdateVisuals();
            return true;
        }

        // ── Visuals ──────────────────────────────────────────────────────────

        public void UpdateVisuals()
        {
            if (_rendererService == null)
            {
                BottleLogger.LogWarning($"'{name}': UpdateVisuals called before Initialize.");
                return;
            }

            if (_renderer == null)
                _renderer = GetComponent<Renderer>();

            _rendererService.UpdateLiquid(_renderer, State, saturationBoost, brightnessBoost);
            _rendererService.UpdateGlass(_renderer, State);
        }
    }
}
