using System.Collections.Generic;
using BottleShaders.Domain.Models;
using BottleShaders.Infrastructure.Interfaces;
using UnityEngine;

namespace BottleShaders.Infrastructure.Implementations
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
                                 float saturationBoost, float brightnessBoost)
        {
            float cumulative = 0f;

            int maxLayers = Mathf.Min(BottleState.MaxSupportedLayers, ColorIDs.Length);
            for (int i = 0; i < maxLayers; i++)
            {
                Color color = Color.clear;
                float fill  = cumulative;

                if (i < layers.Count)
                {
                    var layer = layers[i];
                    color      = AdjustColor(layer.Color.ToUnityColor(), saturationBoost, brightnessBoost);
                    cumulative += layer.Amount;
                    fill       = cumulative;
                }

                _liquidBlock.SetColor(ColorIDs[i], color);
                _liquidBlock.SetFloat(FillIDs[i],  fill);
            }

            _liquidBlock.SetFloat(SurfaceHeightID, totalFill);
            renderer.SetPropertyBlock(_liquidBlock, 1);
        }

        public void UpdateGlass(Renderer renderer, bool isEmpty, DomainColor baseColor)
        {
            Color glassColor;

            if (isEmpty)
            {
                glassColor = new Color(1f, 1f, 1f, 0.18f);
            }
            else
            {
                var uColor = baseColor.ToUnityColor();
                glassColor = new Color(
                    uColor.r * 0.15f + 0.85f,
                    uColor.g * 0.15f + 0.85f,
                    uColor.b * 0.15f + 0.85f,
                    0.25f);
            }

            _glassBlock.SetColor(GlassColorID, glassColor);
            renderer.SetPropertyBlock(_glassBlock, 0);
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