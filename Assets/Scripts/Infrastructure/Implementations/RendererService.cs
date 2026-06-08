using System;
using System.Collections.Generic;
using PuzzleGame.Domain;
using PuzzleGame.Domain.Models;
using PuzzleGame.Infrastructure;
using PuzzleGame.Application.Interfaces;
using UnityEngine;

namespace PuzzleGame.Infrastructure.Implementations
{
    /// <summary>
    /// Writes Ore / glass material parameters via MaterialPropertyBlock.
    /// Shader property slot count is governed by <see cref="ForgeConstants.MaxLayers"/> —
    /// shaders must declare matching _Color1.._ColorN / _Fill1.._FillN properties.
    /// </summary>
    public class RendererService : IRendererService
    {
        private static readonly int[] ColorIDs = BuildShaderPropertyIDs("_Color", ForgeConstants.MaxLayers);
        private static readonly int[] FillIDs  = BuildShaderPropertyIDs("_Fill",  ForgeConstants.MaxLayers);

        private static int[] BuildShaderPropertyIDs(string prefix, int count)
        {
            var ids = new int[count];
            for (int i = 0; i < count; i++)
            {
                ids[i] = Shader.PropertyToID($"{prefix}{i + 1}");
            }
            return ids;
        }

        private static readonly int SurfaceHeightID      = Shader.PropertyToID("_SurfaceHeight");
        private static readonly int GlassColorID         = Shader.PropertyToID("_Color");
        private static readonly int SparkleIntensityID   = Shader.PropertyToID("_SparkleIntensity");
        private static readonly int SparkleSizeID        = Shader.PropertyToID("_SparkleSize");
        private static readonly int LayerBoundaryWidthID = Shader.PropertyToID("_LayerBoundaryWidth");

        private readonly IColorAdapter _colorAdapter;

        // MaterialPropertyBlock instances are reused across every mold render.
        // Crucially we do NOT create per-renderer Material instances — that
        // would leak GPU memory on every level transition. The blocks are
        // shared, and the underlying Renderer owns the actual material asset.
        private readonly MaterialPropertyBlock _OreBlock = new MaterialPropertyBlock();
        private readonly MaterialPropertyBlock _glassBlock  = new MaterialPropertyBlock();
        private readonly List<OreLayer> _mergedLayers = new List<OreLayer>();

        public RendererService() : this(new ColorAdapter()) { }

        public RendererService(IColorAdapter colorAdapter)
        {
            _colorAdapter = colorAdapter ?? throw new ArgumentNullException(nameof(colorAdapter));
        }

        public void UpdateOre(Renderer renderer, IReadOnlyList<OreLayer> layers, float totalFill, PuzzleGame.Application.Configuration.MoldVisualConfig config)
        {
            if (renderer == null) throw new ArgumentNullException(nameof(renderer));
            if (layers == null)   throw new ArgumentNullException(nameof(layers));

            // Visually merge consecutive layers of the same color to prevent boundary line glitches
            _mergedLayers.Clear();
            foreach (var layer in layers)
            {
                if (layer.Amount <= ForgeConstants.LayerAmountEpsilon) continue;

                if (_mergedLayers.Count > 0 && _mergedLayers[_mergedLayers.Count - 1].Color == layer.Color)
                {
                    var prev = _mergedLayers[_mergedLayers.Count - 1];
                    _mergedLayers[_mergedLayers.Count - 1] = new OreLayer(prev.Color, prev.Amount + layer.Amount);
                }
                else
                {
                    _mergedLayers.Add(layer);
                }
            }

            float cumulative = 0f;
            int maxLayers = Mathf.Min(ForgeConstants.MaxLayers, ColorIDs.Length);

            for (int i = 0; i < maxLayers; i++)
            {
                Color color = Color.clear;
                float fill  = cumulative;

                if (i < _mergedLayers.Count)
                {
                    var layer = _mergedLayers[i];
                    float sat = config != null ? config.saturationBoost : 1.25f;
                    float bri = config != null ? config.brightnessBoost : 1.15f;
                    color      = AdjustColor(_colorAdapter.ToUnity(layer.Color), sat, bri);
                    cumulative += layer.Amount;
                    fill       = cumulative;
                }

                _OreBlock.SetColor(ColorIDs[i], color);
                _OreBlock.SetFloat(FillIDs[i],  fill);
            }

            _OreBlock.SetFloat(SurfaceHeightID, totalFill);
            if (config != null)
            {
                _OreBlock.SetFloat(SparkleIntensityID, config.sparkleIntensity);
                _OreBlock.SetFloat(SparkleSizeID,      config.sparkleSize);
                _OreBlock.SetFloat(LayerBoundaryWidthID, config.layerBoundaryWidth);
            }
            int materialIndex = config != null ? config.oreMaterialIndex : 1;
            int matsCount = renderer.sharedMaterials != null ? renderer.sharedMaterials.Length : 0;
            if (matsCount > 0)
            {
                int targetIndex = Mathf.Clamp(materialIndex, 0, matsCount - 1);
                renderer.SetPropertyBlock(_OreBlock, targetIndex);
            }

#if UNITY_EDITOR
            // Editor-time shared material modification removed — modifying shared
            // material assets at edit time permanently corrupts them on disk and
            // affects all mold instances using the same asset. Use the MPB path
            // for runtime (play mode) rendering; editor previews should use
            // EditorGUI.DrawPreview or a dedicated preview material instead.
#endif
        }

        public void UpdateGlass(Renderer renderer, bool isEmpty, DomainColor baseColor, PuzzleGame.Application.Configuration.MoldVisualConfig config)
        {
            if (renderer == null) throw new ArgumentNullException(nameof(renderer));

            Color glassColor;

            if (isEmpty)
            {
                glassColor = config != null ? config.moldEmptyColor : new Color(1.0f, 1.0f, 1.0f, 0.18f);
            }
            else
            {
                var uColor = _colorAdapter.ToUnity(baseColor);
                float tintBase = config != null ? config.moldTintBase : 0.85f;
                float tintMult = config != null ? config.moldTintMultiplier : 0.15f;
                float tintAlpha = config != null ? config.moldTintAlpha : 0.25f;
                glassColor = new Color(
                    uColor.r * tintMult + tintBase,
                    uColor.g * tintMult + tintBase,
                    uColor.b * tintMult + tintBase,
                    tintAlpha);
            }

            int materialIndex = config != null ? config.moldMaterialIndex : 0;
            _glassBlock.SetColor(GlassColorID, glassColor);
            int matsCount = renderer.sharedMaterials != null ? renderer.sharedMaterials.Length : 0;
            if (matsCount > 0)
            {
                int targetIndex = Mathf.Clamp(materialIndex, 0, matsCount - 1);
                renderer.SetPropertyBlock(_glassBlock, targetIndex);
            }

#if UNITY_EDITOR
            // Editor-time shared material modification removed — see comment in UpdateOre.
#endif
        }

        private static Color AdjustColor(Color c, float sat, float bright)
        {
            float avg = (c.r + c.g + c.b) / 3f;
            return new Color(
                Mathf.Clamp01((avg + (c.r - avg) * sat) * bright),
                Mathf.Clamp01((avg + (c.g - avg) * sat) * bright),
                Mathf.Clamp01((avg + (c.b - avg) * sat) * bright),
                c.a);
        }
    }
}