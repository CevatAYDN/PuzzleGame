using System.Collections.Generic;
using PuzzleGame.Domain.Models;
using PuzzleGame.Infrastructure.Interfaces;
using PuzzleGame.Infrastructure;
using UnityEngine;

namespace PuzzleGame.Infrastructure.Implementations
{
    public class RendererService : IRendererService
    {
        private static readonly int[] ColorIDs = new int[]
        {
            Shader.PropertyToID("_Color1"),
            Shader.PropertyToID("_Color2"),
            Shader.PropertyToID("_Color3"),
            Shader.PropertyToID("_Color4"),
        };

        private static readonly int[] FillIDs = new int[]
        {
            Shader.PropertyToID("_Fill1"),
            Shader.PropertyToID("_Fill2"),
            Shader.PropertyToID("_Fill3"),
            Shader.PropertyToID("_Fill4"),
        };

        private static readonly int SurfaceHeightID = Shader.PropertyToID("_SurfaceHeight");
        private static readonly int GlassColorID    = Shader.PropertyToID("_Color");

        private readonly MaterialPropertyBlock _liquidBlock = new MaterialPropertyBlock();
        private readonly MaterialPropertyBlock _glassBlock  = new MaterialPropertyBlock();

        public void UpdateLiquid(Renderer renderer, IReadOnlyList<LiquidLayer> layers, float totalFill,
                                 float saturationBoost, float brightnessBoost, int materialIndex = 1)
        {
            // Visually merge consecutive layers of the same color to prevent boundary line glitches
            var merged = new List<LiquidLayer>();
            foreach (var layer in layers)
            {
                if (layer.Amount <= 0.0001f) continue;

                if (merged.Count > 0 && merged[merged.Count - 1].Color == layer.Color)
                {
                    var prev = merged[merged.Count - 1];
                    merged[merged.Count - 1] = new LiquidLayer(prev.Color, prev.Amount + layer.Amount);
                }
                else
                {
                    merged.Add(layer);
                }
            }

            float cumulative = 0f;
            int maxLayers = Mathf.Min(BottleState.MaxSupportedLayers, ColorIDs.Length);

            for (int i = 0; i < maxLayers; i++)
            {
                Color color = Color.clear;
                float fill  = cumulative;

                if (i < merged.Count)
                {
                    var layer = merged[i];
                    color      = AdjustColor(ColorAdapter.ToUnity(layer.Color), saturationBoost, brightnessBoost);
                    cumulative += layer.Amount;
                    fill       = cumulative;
                }

                _liquidBlock.SetColor(ColorIDs[i], color);
                _liquidBlock.SetFloat(FillIDs[i],  fill);
            }

            _liquidBlock.SetFloat(SurfaceHeightID, totalFill);
            renderer.SetPropertyBlock(_liquidBlock, materialIndex);
        }

        public void UpdateGlass(Renderer renderer, bool isEmpty, DomainColor baseColor, int materialIndex = 0)
        {
            Color glassColor;

            if (isEmpty)
            {
                glassColor = new Color(1f, 1f, 1f, 0.18f);
            }
            else
            {
                var uColor = ColorAdapter.ToUnity(baseColor);
                glassColor = new Color(
                    uColor.r * 0.15f + 0.85f,
                    uColor.g * 0.15f + 0.85f,
                    uColor.b * 0.15f + 0.85f,
                    0.25f);
            }

            _glassBlock.SetColor(GlassColorID, glassColor);
            renderer.SetPropertyBlock(_glassBlock, materialIndex);
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