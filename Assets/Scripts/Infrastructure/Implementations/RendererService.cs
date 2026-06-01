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

        public void UpdateLiquid(Renderer renderer, BottleState state,
                                 float saturationBoost, float brightnessBoost)
        {
            float cumulative = 0f;

            for (int i = 0; i < BottleState.MaxSupportedLayers; i++)
            {
                Color color = Color.clear;
                float fill  = cumulative;

                if (i < state.Layers.Count)
                {
                    var layer = state.Layers[i];
                    color      = AdjustColor(layer.Color.ToUnityColor(), saturationBoost, brightnessBoost);
                    cumulative += layer.Amount;
                    fill       = cumulative;
                }

                _liquidBlock.SetColor(ColorIDs[i], color);
                _liquidBlock.SetFloat(FillIDs[i],  fill);
            }

            _liquidBlock.SetFloat(SurfaceHeightID, state.TotalFill);
            renderer.SetPropertyBlock(_liquidBlock, 1);
        }

        public void UpdateGlass(Renderer renderer, BottleState state)
        {
            Color glassColor;

            if (state.IsEmpty)
            {
                glassColor = new Color(1f, 1f, 1f, 0.18f);
            }
            else
            {
                var baseColor = state.Layers[0].Color.ToUnityColor();
                glassColor = new Color(
                    baseColor.r * 0.15f + 0.85f,
                    baseColor.g * 0.15f + 0.85f,
                    baseColor.b * 0.15f + 0.85f,
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