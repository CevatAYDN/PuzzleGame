using System;
using PuzzleGame.Domain.Interfaces;
using PuzzleGame.Domain.Models;

namespace PuzzleGame.Domain.Services
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

            var sourceTop = source.TopLayer;
            var targetTop = target.TopLayer;
            if (sourceTop == null || targetTop == null) return false;

            return ColorsMatch(sourceTop.Value.Color, targetTop.Value.Color);
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
            Math.Abs(a.R - b.R) < _colorTolerance &&
            Math.Abs(a.G - b.G) < _colorTolerance &&
            Math.Abs(a.B - b.B) < _colorTolerance &&
            Math.Abs(a.A - b.A) < _colorTolerance;
    }
}
