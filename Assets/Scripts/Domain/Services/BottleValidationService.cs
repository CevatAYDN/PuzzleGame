using BottleShaders.Domain.Interfaces;
using BottleShaders.Domain.Models;
using UnityEngine;

namespace BottleShaders.Domain.Services
{
    public class BottleValidationService : IBottleValidator
    {
        private readonly float _colorTolerance;

        public BottleValidationService(float colorTolerance = 0.05f)
        {
            _colorTolerance = colorTolerance;
        }

        public bool CanPour(BottleState source, BottleState target)
        {
            if (source == null || target == null) return false;
            if (source == target)                 return false;
            if (source.IsEmpty)                   return false;
            if (target.IsFull)                    return false;

            if (target.IsEmpty) return true;

            return ColorsMatch(source.TopLayer!.Value.Color, target.TopLayer!.Value.Color);
        }

        public bool IsComplete(BottleState bottle)
        {
            if (bottle == null)   return false;
            if (bottle.IsEmpty)   return true;
            if (!bottle.IsFull)   return false;

            var layers = bottle.Layers;
            var firstColor = layers[0].Color;
            int count = layers.Count;
            for (int i = 1; i < count; i++)
            {
                if (!ColorsMatch(layers[i].Color, firstColor))
                    return false;
            }
            return true;
        }

        public bool ColorsMatch(DomainColor a, DomainColor b) =>
            Mathf.Abs(a.R - b.R) < _colorTolerance &&
            Mathf.Abs(a.G - b.G) < _colorTolerance &&
            Mathf.Abs(a.B - b.B) < _colorTolerance;

        public bool UnityColorsMatch(Color a, Color b) =>
            Mathf.Abs(a.r - b.r) < _colorTolerance &&
            Mathf.Abs(a.g - b.g) < _colorTolerance &&
            Mathf.Abs(a.b - b.b) < _colorTolerance;
    }
}