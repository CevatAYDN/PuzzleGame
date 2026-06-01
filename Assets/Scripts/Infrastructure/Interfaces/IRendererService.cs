using System.Collections.Generic;
using PuzzleGame.Domain.Models;
using UnityEngine;

namespace PuzzleGame.Infrastructure.Interfaces
{
    public interface IRendererService
    {
        void UpdateLiquid(Renderer renderer, IReadOnlyList<LiquidLayer> layers, float totalFill, float saturationBoost, float brightnessBoost);
        void UpdateGlass(Renderer renderer, bool isEmpty, DomainColor baseColor);
    }
}
