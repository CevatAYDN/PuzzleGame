using System;
using PuzzleGame.Domain;
using PuzzleGame.Domain.Interfaces;
using PuzzleGame.Domain.Models;

namespace PuzzleGame.Domain.Services
{
    /// <summary>
    /// Validates pour legality and bottle-completion.
    /// Pure C# Domain service — no UnityEngine dependency.
    /// </summary>
    public class BottleValidationService : IBottleValidator
    {
        private readonly float _colorTolerance;

        /// <exception cref="ArgumentOutOfRangeException">If tolerance is &lt;= 0.</exception>
        public BottleValidationService(float colorTolerance = BottleConstants.ColorMatchEpsilon)
        {
            if (colorTolerance <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(colorTolerance), colorTolerance,
                    "Color match tolerance must be strictly positive.");
            }
            _colorTolerance = colorTolerance;
        }

        /// <exception cref="ArgumentNullException">If source or target is null.</exception>
        public bool CanPour(BottleState source, BottleState target)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (source == target) return false;
            if (source.IsEmpty)  return false;
            if (target.IsFull)   return false;

            if (target.IsEmpty) return true;

            var sourceTop = source.TopLayer;
            var targetTop = target.TopLayer;
            if (sourceTop == null || targetTop == null) return false;

            return ColorsMatch(sourceTop.Value.Color, targetTop.Value.Color);
        }

        /// <exception cref="ArgumentNullException">If bottle is null.</exception>
        public bool IsComplete(BottleState bottle)
        {
            if (bottle == null) throw new ArgumentNullException(nameof(bottle));
            if (bottle.IsEmpty) return true;
            if (!bottle.IsFull) return false;

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
