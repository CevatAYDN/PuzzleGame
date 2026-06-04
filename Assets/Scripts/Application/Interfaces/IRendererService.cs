using System.Collections.Generic;
using PuzzleGame.Domain.Models;
using UnityEngine;

namespace PuzzleGame.Application.Interfaces
{
    /// <summary>
    /// Soyut render servisi — Ore/glass material parametrelerini yönetir.
    /// Infrastructure implementasyonu (RendererService) MaterialPropertyBlock kullanır.
    /// </summary>
    public interface IRendererService
    {
        /// <summary>
        /// Update Ore material properties on a renderer.
        /// </summary>
        void UpdateOre(Renderer renderer, IReadOnlyList<OreLayer> layers, float totalFill, Configuration.MoldVisualConfig config);

        /// <summary>
        /// Update glass material properties on a renderer.
        /// </summary>
        void UpdateGlass(Renderer renderer, bool isEmpty, DomainColor baseColor, Configuration.MoldVisualConfig config);
    }
}
