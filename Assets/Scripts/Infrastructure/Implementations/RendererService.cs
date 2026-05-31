using BottleShaders.Domain.Models;
using BottleShaders.Infrastructure.Interfaces;
using UnityEngine;

namespace BottleShaders.Infrastructure.Implementations
{
    /// <summary>
    /// Pushes bottle state into the GPU via MaterialPropertyBlocks.
    /// One shared block per instance — no material duplication, no GC pressure.
    /// Supports up to 4 liquid layers (matches the LayeredLiquid shader).
    /// </summary>
    public class RendererService : IRendererService
    {
        // ── Shader property IDs (cached once, never GC'd) ────────────────────

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

        // ── Per-instance property blocks (reused, not re-allocated) ──────────

        private readonly MaterialPropertyBlock _liquidBlock = new MaterialPropertyBlock();
        private readonly MaterialPropertyBlock _glassBlock  = new MaterialPropertyBlock();

        // ── IRendererService ─────────────────────────────────────────────────

        /// <inheritdoc/>
        public void UpdateLiquid(Renderer renderer, BottleState state,
                                 float saturationBoost, float brightnessBoost)
        {
            float cumulative = 0f;

            for (int i = 0; i < 4; i++)
            {
                Color color = Color.clear;
                float fill  = cumulative;

                if (i < state.Layers.Count)
                {
                    var layer = state.Layers[i];
                    color      = AdjustColor(layer.Color, saturationBoost, brightnessBoost);
                    cumulative += layer.Amount;
                    fill       = cumulative;
                }

                _liquidBlock.SetColor(ColorIDs[i], color);
                _liquidBlock.SetFloat(FillIDs[i],  fill);
            }

            _liquidBlock.SetFloat(SurfaceHeightID, state.TotalFill);
            renderer.SetPropertyBlock(_liquidBlock, 1); // sub-mesh 1 = liquid
        }

        /// <inheritdoc/>
        public void UpdateGlass(Renderer renderer, BottleState state)
        {
            Color glassColor;

            if (state.IsEmpty)
            {
                glassColor = new Color(1f, 1f, 1f, 0.18f);
            }
            else
            {
                var base_ = state.Layers[0].Color;
                glassColor = new Color(
                    base_.r * 0.15f + 0.85f,
                    base_.g * 0.15f + 0.85f,
                    base_.b * 0.15f + 0.85f,
                    0.25f);
            }

            _glassBlock.SetColor(GlassColorID, glassColor);
            renderer.SetPropertyBlock(_glassBlock, 0); // sub-mesh 0 = glass
        }

        // ── Helpers ──────────────────────────────────────────────────────────

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
