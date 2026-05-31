using BottleShaders.Domain.Models;
using UnityEngine;

namespace BottleShaders.Infrastructure.Interfaces
{
    public interface IRendererService
    {
        void UpdateLiquid(Renderer renderer, BottleState state, float saturationBoost, float brightnessBoost);
        void UpdateGlass(Renderer renderer, BottleState state);
    }
}
