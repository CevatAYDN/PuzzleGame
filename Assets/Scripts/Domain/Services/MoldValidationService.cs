using System;
using PuzzleGame.Domain;
using PuzzleGame.Domain.Interfaces;
using PuzzleGame.Domain.Models;

namespace PuzzleGame.Domain.Services
{
    /// <summary>
    /// Validates Cast legality and Mold-completion.
    /// Pure C# Domain service — no UnityEngine dependency.
    /// </summary>
    public class MoldValidationService : IMoldValidator
    {
        private readonly float _colorTolerance;

        /// <exception cref="ArgumentOutOfRangeException">If tolerance is &lt;= 0.</exception>
        public MoldValidationService(float colorTolerance = ForgeConstants.ColorMatchEpsilon)
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
        public bool CanCast(MoldState source, MoldState target)
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

        /// <exception cref="ArgumentNullException">If Mold is null.</exception>
        public bool IsComplete(MoldState Mold)
        {
            if (Mold == null) throw new ArgumentNullException(nameof(Mold));
            if (Mold.IsEmpty) return true;
            if (!Mold.IsFull) return false;

            var layers = Mold.Layers;
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
