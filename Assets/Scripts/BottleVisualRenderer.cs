using System;
using System.Collections.Generic;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Configuration;
using PuzzleGame.Domain;
using PuzzleGame.Domain.Models;
using PuzzleGame.Infrastructure.Interfaces;
using UnityEngine;

namespace PuzzleGame
{
    /// <summary>
    /// Handles all visual rendering for a bottle: liquid/glass material updates and selection highlight fresnel.
    /// Extracted from BottleController for SRP (single responsibility = visual presentation).
    /// Reads visual state via providers — does not own the source-of-truth list.
    /// </summary>
    public sealed class BottleVisualRenderer
    {
        private readonly Renderer _renderer;
        private readonly IRendererService _rendererService;
        private readonly BottleVisualConfig _visualConfig;
        private readonly Func<List<LiquidLayer>> _visualLayersProvider;
        private readonly Func<float> _visualTotalFillProvider;
        private MaterialPropertyBlock _propBlock;
        private bool _isHighlighted;

        private static readonly int FresnelIntensityID = Shader.PropertyToID("_FresnelIntensity");

        public BottleVisualRenderer(
            Renderer renderer,
            IRendererService rendererService,
            BottleVisualConfig visualConfig,
            Func<List<LiquidLayer>> visualLayersProvider,
            Func<float> visualTotalFillProvider)
        {
            _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
            _rendererService = rendererService ?? throw new ArgumentNullException(nameof(rendererService));
            _visualConfig = visualConfig;
            _visualLayersProvider = visualLayersProvider ?? throw new ArgumentNullException(nameof(visualLayersProvider));
            _visualTotalFillProvider = visualTotalFillProvider ?? throw new ArgumentNullException(nameof(visualTotalFillProvider));
        }

        /// <summary>
        /// Pushes current visual state (layers + fill) to the renderer service for both liquid and glass materials.
        /// </summary>
        public void Update()
        {
            if (_rendererService == null || _renderer == null) return;

            var visualLayers = _visualLayersProvider();
            float visualTotalFill = _visualTotalFillProvider();

            float sat = _visualConfig != null ? _visualConfig.saturationBoost : BottleConstants.DefaultSaturationBoost;
            float bri = _visualConfig != null ? _visualConfig.brightnessBoost : BottleConstants.DefaultBrightnessBoost;
            int liquidIndex = _visualConfig != null ? _visualConfig.liquidMaterialIndex : BottleConstants.DefaultLiquidMaterialIndex;
            _rendererService.UpdateLiquid(_renderer, visualLayers, visualTotalFill, sat, bri, liquidIndex);

            bool isEmpty = visualLayers.Count == 0 || visualTotalFill <= BottleConstants.LayerAmountEpsilon;
            DomainColor baseColor = visualLayers.Count > 0 ? visualLayers[0].Color : new DomainColor(0, 0, 0, 0);
            int glassIndex = _visualConfig != null ? _visualConfig.glassMaterialIndex : BottleConstants.DefaultGlassMaterialIndex;
            _rendererService.UpdateGlass(_renderer, isEmpty, baseColor, glassIndex);
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
            int glassIndex = _visualConfig != null ? _visualConfig.glassMaterialIndex : BottleConstants.DefaultGlassMaterialIndex;
            _renderer.GetPropertyBlock(_propBlock, glassIndex);
            _propBlock.SetFloat(FresnelIntensityID,
                active ? BottleConstants.HighlightActiveFresnel : BottleConstants.HighlightInactiveFresnel);
            _renderer.SetPropertyBlock(_propBlock, glassIndex);
        }
    }
}
