using System;
using System.Collections.Generic;
using PuzzleGame.Domain;
using PuzzleGame.Domain.Models;
using PuzzleGame.Infrastructure;
using PuzzleGame.Infrastructure.Interfaces;
using UnityEngine;

namespace PuzzleGame.Infrastructure.Implementations
{
    /// <summary>
    /// Writes liquid / glass material parameters via MaterialPropertyBlock.
    /// Shader property slot count is governed by <see cref="BottleConstants.MaxLayers"/> —
    /// shaders must declare matching _Color1.._ColorN / _Fill1.._FillN properties.
    /// </summary>
    public class RendererService : IRendererService
    {
        private static readonly int[] ColorIDs = BuildShaderPropertyIDs("_Color", BottleConstants.MaxLayers);
        private static readonly int[] FillIDs  = BuildShaderPropertyIDs("_Fill",  BottleConstants.MaxLayers);

        private static int[] BuildShaderPropertyIDs(string prefix, int count)
        {
            var ids = new int[count];
            for (int i = 0; i < count; i++)
            {
                ids[i] = Shader.PropertyToID($"{prefix}{i + 1}");
            }
            return ids;
        }

        private static readonly int SurfaceHeightID = Shader.PropertyToID("_SurfaceHeight");
        private static readonly int GlassColorID    = Shader.PropertyToID("_Color");

        private readonly MaterialPropertyBlock _liquidBlock = new MaterialPropertyBlock();
        private readonly MaterialPropertyBlock _glassBlock  = new MaterialPropertyBlock();
        private readonly List<LiquidLayer> _mergedLayers = new List<LiquidLayer>();

        public void UpdateLiquid(Renderer renderer, IReadOnlyList<LiquidLayer> layers, float totalFill,
                                 float saturationBoost, float brightnessBoost, int materialIndex = 1)
        {
            if (renderer == null) throw new ArgumentNullException(nameof(renderer));
            if (layers == null)   throw new ArgumentNullException(nameof(layers));

            // Visually merge consecutive layers of the same color to prevent boundary line glitches
            _mergedLayers.Clear();
            foreach (var layer in layers)
            {
                if (layer.Amount <= BottleConstants.LayerAmountEpsilon) continue;

                if (_mergedLayers.Count > 0 && _mergedLayers[_mergedLayers.Count - 1].Color == layer.Color)
                {
                    var prev = _mergedLayers[_mergedLayers.Count - 1];
                    _mergedLayers[_mergedLayers.Count - 1] = new LiquidLayer(prev.Color, prev.Amount + layer.Amount);
                }
                else
                {
                    _mergedLayers.Add(layer);
                }
            }

            float cumulative = 0f;
            int maxLayers = Mathf.Min(BottleConstants.MaxLayers, ColorIDs.Length);

            for (int i = 0; i < maxLayers; i++)
            {
                Color color = Color.clear;
                float fill  = cumulative;

                if (i < _mergedLayers.Count)
                {
                    var layer = _mergedLayers[i];
                    color      = AdjustColor(ColorAdapter.ToUnity(layer.Color), saturationBoost, brightnessBoost);
                    cumulative += layer.Amount;
                    fill       = cumulative;
                }

                _liquidBlock.SetColor(ColorIDs[i], color);
                _liquidBlock.SetFloat(FillIDs[i],  fill);
            }

            _liquidBlock.SetFloat(SurfaceHeightID, totalFill);
            renderer.SetPropertyBlock(_liquidBlock, materialIndex);

#if UNITY_EDITOR
            if (!UnityEngine.Application.isPlaying)
            {
                var mats = renderer.sharedMaterials;
                if (materialIndex >= 0 && materialIndex < mats.Length)
                {
                    var mat = mats[materialIndex];
                    if (mat != null)
                    {
                        cumulative = 0f;
                        for (int i = 0; i < maxLayers; i++)
                        {
                            Color color = Color.clear;
                            float fill  = cumulative;

                            if (i < _mergedLayers.Count)
                            {
                                var layer = _mergedLayers[i];
                                color      = AdjustColor(ColorAdapter.ToUnity(layer.Color), saturationBoost, brightnessBoost);
                                cumulative += layer.Amount;
                                fill       = cumulative;
                            }

                            mat.SetColor(ColorIDs[i], color);
                            mat.SetFloat(FillIDs[i],  fill);
                        }

                        mat.SetFloat(SurfaceHeightID, totalFill);
                        UnityEditor.EditorUtility.SetDirty(mat);
                    }
                }
            }
#endif
        }

        public void UpdateGlass(Renderer renderer, bool isEmpty, DomainColor baseColor, int materialIndex = 0)
        {
            if (renderer == null) throw new ArgumentNullException(nameof(renderer));

            Color glassColor;

            if (isEmpty)
            {
                glassColor = new Color(
                    BottleConstants.GlassEmptyR,
                    BottleConstants.GlassEmptyG,
                    BottleConstants.GlassEmptyB,
                    BottleConstants.GlassEmptyA);
            }
            else
            {
                var uColor = ColorAdapter.ToUnity(baseColor);
                glassColor = new Color(
                    uColor.r * BottleConstants.GlassTintMultiplier + BottleConstants.GlassTintBase,
                    uColor.g * BottleConstants.GlassTintMultiplier + BottleConstants.GlassTintBase,
                    uColor.b * BottleConstants.GlassTintMultiplier + BottleConstants.GlassTintBase,
                    BottleConstants.GlassTintAlpha);
            }

            _glassBlock.SetColor(GlassColorID, glassColor);
            renderer.SetPropertyBlock(_glassBlock, materialIndex);

#if UNITY_EDITOR
            if (!UnityEngine.Application.isPlaying)
            {
                var mats = renderer.sharedMaterials;
                if (materialIndex >= 0 && materialIndex < mats.Length)
                {
                    var mat = mats[materialIndex];
                    if (mat != null)
                    {
                        mat.SetColor(GlassColorID, glassColor);
                        UnityEditor.EditorUtility.SetDirty(mat);
                    }
                }
            }
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