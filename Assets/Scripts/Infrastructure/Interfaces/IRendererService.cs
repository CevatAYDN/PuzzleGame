using System.Collections.Generic;
using BottleShaders.Domain.Models;
using UnityEngine;

namespace BottleShaders.Infrastructure.Interfaces
{
    public interface IRendererService
    {
        void UpdateLiquid(Renderer renderer, IReadOnlyList<LiquidLayer> layers, float totalFill, float saturationBoost, float brightnessBoost);
        void UpdateGlass(Renderer renderer, bool isEmpty, DomainColor baseColor);
    }
}
