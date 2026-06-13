using System;
using System.Collections.Generic;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Configuration;
using PuzzleGame.Domain;
using PuzzleGame.Domain.Models;
using UnityEngine;

namespace PuzzleGame
{
    /// <summary>
    /// Handles all visual rendering for a Mold: Ore/glass material updates and selection highlight fresnel.
    /// Extracted from MoldController for SRP (single responsibility = visual presentation).
    /// Reads visual state via providers — does not own the source-of-truth list.
    /// </summary>
    public sealed class MoldVisualRenderer
    {
        private readonly Renderer _renderer;
        private readonly IRendererService _rendererService;
        private readonly MoldVisualConfig _visualConfig;
        private readonly Func<List<OreLayer>> _visualLayersProvider;
        private readonly Func<float> _visualTotalFillProvider;
        private MaterialPropertyBlock _propBlock;
        private bool? _isHighlighted = null; // Fix: Nullable to ensure initial SetSelectionHighlight(false) is applied

        // Dirty flag cache to avoid redundant renderer service calls (Fix #12)
        private int _lastLayerHash = -1;
        private float _lastTotalFill = -1f;
        private bool _lastColorBlindActive = false;

        private static readonly int FresnelIntensityID = Shader.PropertyToID("_FresnelIntensity");

        public MoldVisualRenderer(
            Renderer renderer,
            IRendererService rendererService,
            MoldVisualConfig visualConfig,
            Func<List<OreLayer>> visualLayersProvider,
            Func<float> visualTotalFillProvider)
        {
            _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
            _rendererService = rendererService;
            _visualConfig = visualConfig;
            _visualLayersProvider = visualLayersProvider ?? throw new ArgumentNullException(nameof(visualLayersProvider));
            _visualTotalFillProvider = visualTotalFillProvider ?? throw new ArgumentNullException(nameof(visualTotalFillProvider));
        }

        /// <summary>
        /// Pushes current visual state (layers + fill) to the renderer service for both Ore and glass materials.
        /// Uses dirty flag pattern to skip redundant updates (Fix #12).
        /// </summary>
        public void Update()
        {
            if (_rendererService == null || _renderer == null) return;

            var visualLayers = _visualLayersProvider();
            float visualTotalFill = _visualTotalFillProvider();

            // Dirty flag: compute hash of current state
            int layerHash = visualLayers != null ? ComputeLayerHash(visualLayers) : 0;
            bool colorBlindActive = _rendererService.ColorBlindModeEnabled;

            // Skip if state unchanged
            if (layerHash == _lastLayerHash && 
                Mathf.Abs(visualTotalFill - _lastTotalFill) < 0.0001f && 
                colorBlindActive == _lastColorBlindActive)
            {
                return;
            }

            _lastLayerHash = layerHash;
            _lastTotalFill = visualTotalFill;
            _lastColorBlindActive = colorBlindActive;

            // Fix #K3: Guard against null visualLayers after dirty flag
            if (visualLayers == null)
            {
                visualLayers = new List<OreLayer>();
            }

            _rendererService.UpdateOre(_renderer, visualLayers, visualTotalFill, _visualConfig);

            bool isEmpty = visualLayers.Count == 0 || visualTotalFill <= ForgeConstants.LayerAmountEpsilon;
            DomainColor baseColor = visualLayers.Count > 0 ? visualLayers[0].Color : new DomainColor(0, 0, 0, 0);
            _rendererService.UpdateGlass(_renderer, isEmpty, baseColor, _visualConfig);
        }

        /// <summary>
        /// Computes a fast hash of layer list (color + amount) for dirty detection.
        /// </summary>
        private static int ComputeLayerHash(List<OreLayer> layers)
        {
            if (layers == null || layers.Count == 0) return 0;
            int hash = layers.Count;
            for (int i = 0; i < layers.Count; i++)
            {
                var layer = layers[i];
                hash = hash * 31 + layer.Color.GetHashCode();
                hash = hash * 31 + layer.Amount.GetHashCode();
            }
            return hash;
        }

        /// <summary>
        /// Toggles the selection highlight fresnel effect on the glass material slot.
        /// No-op if state already matches.
        /// </summary>
        public void SetSelectionHighlight(bool active)
        {
            if (_renderer == null) return;
            if (_isHighlighted.HasValue && _isHighlighted.Value == active) return;
            _isHighlighted = active;
            
            if (_propBlock == null) _propBlock = new MaterialPropertyBlock();
            int glassIndex = _visualConfig != null ? _visualConfig.moldMaterialIndex : 0;
            _renderer.GetPropertyBlock(_propBlock, glassIndex);
            
            float activeFresnel = _visualConfig != null ? _visualConfig.highlightActiveFresnel : 4.0f;
            float inactiveFresnel = _visualConfig != null ? _visualConfig.highlightInactiveFresnel : 1.5f;
            
            _propBlock.SetFloat(FresnelIntensityID, active ? activeFresnel : inactiveFresnel);
            _renderer.SetPropertyBlock(_propBlock, glassIndex);
        }

        /// <summary>
        /// Releases the cached <see cref="MaterialPropertyBlock"/> and clears
        /// the dirty-flag caches. Call from <c>MoldController.OnDestroy</c>
        /// so the cached state cannot survive past the host GameObject.
        ///
        /// This renderer does NOT own a <see cref="Material"/> instance — it
        /// uses <see cref="MaterialPropertyBlock"/> exclusively, so there is
        /// no per-instance material to <c>Destroy</c>. We still call
        /// <c>SetPropertyBlock(null)</c> to drop the reference the renderer
        /// is holding onto us.
        /// </summary>
        public void Dispose()
        {
            if (_renderer != null)
            {
                int glassIndex = _visualConfig != null ? _visualConfig.moldMaterialIndex : 0;
                _renderer.SetPropertyBlock(null, glassIndex);
            }
            _propBlock = null;
            _lastLayerHash = -1;
            _lastTotalFill = -1f;
        }
    }
}
