using System;
using System.Collections.Generic;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Configuration;
using PuzzleGame.Domain;
using PuzzleGame.Domain.Models;
// IRendererService now in PuzzleGame.Application.Interfaces
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
        private bool _isHighlighted;

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
        /// </summary>
        public void Update()
        {
            if (_rendererService == null || _renderer == null) return;

            var visualLayers = _visualLayersProvider();
            float visualTotalFill = _visualTotalFillProvider();

            _rendererService.UpdateOre(_renderer, visualLayers, visualTotalFill, _visualConfig);

            bool isEmpty = visualLayers.Count == 0 || visualTotalFill <= ForgeConstants.LayerAmountEpsilon;
            DomainColor baseColor = visualLayers.Count > 0 ? visualLayers[0].Color : new DomainColor(0, 0, 0, 0);
            _rendererService.UpdateGlass(_renderer, isEmpty, baseColor, _visualConfig);
        }

        /// <summary>
        /// Toggles the selection highlight fresnel effect on the glass material slot.
        /// No-op if state already matches.
        /// </summary>
        public void SetSelectionHighlight(bool active)
        {
            if (_renderer == null) return;
            if (_isHighlighted == active) return;
            _isHighlighted = active;
            if (_propBlock == null) _propBlock = new MaterialPropertyBlock();
            int glassIndex = _visualConfig != null ? _visualConfig.moldMaterialIndex : 0;
            _renderer.GetPropertyBlock(_propBlock, glassIndex);
            
            float activeFresnel = _visualConfig != null ? _visualConfig.highlightActiveFresnel : 4.0f;
            float inactiveFresnel = _visualConfig != null ? _visualConfig.highlightInactiveFresnel : 1.5f;
            
            _propBlock.SetFloat(FresnelIntensityID, active ? activeFresnel : inactiveFresnel);
            _renderer.SetPropertyBlock(_propBlock, glassIndex);
        }
    }
}
