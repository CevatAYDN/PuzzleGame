using System.Collections.Generic;
using PuzzleGame.Domain.Models;
using UnityEngine;

namespace PuzzleGame.Application.Interfaces
{
    /// <summary>
    /// Soyut render servisi — liquid/glass material parametrelerini yönetir.
    /// Infrastructure implementasyonu (RendererService) MaterialPropertyBlock kullanır.
    /// </summary>
    public interface IRendererService
    {
        /// <summary>
        /// Update liquid material properties on a renderer.
        /// </summary>
        void UpdateLiquid(Renderer renderer, IReadOnlyList<LiquidLayer> layers, float totalFill, float saturationBoost, float brightnessBoost, int materialIndex = 1);

        /// <summary>
        /// Update glass material properties on a renderer.
        /// </summary>
        void UpdateGlass(Renderer renderer, bool isEmpty, DomainColor baseColor, int materialIndex = 0);
    }
}
