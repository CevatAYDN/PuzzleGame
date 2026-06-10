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

            var sourceTop = source.TopLayer;
            if (sourceTop == null || sourceTop.Value.IsEmpty) return false;

            // Target "empty-like" check: sometimes a mold may contain layers that are
            // considered empty at the OreLayer level (transparent / amount epsilon / ColorType.None).
            if (target.IsEmpty) return true;

            var targetTop = target.TopLayer;
            if (targetTop == null || targetTop.Value.IsEmpty) return true;

            return ColorsMatch(sourceTop.Value.Color, targetTop.Value.Color);
        }

        /// <exception cref="ArgumentNullException">If Mold is null.</exception>
        /// <remarks>
        /// Fix #2: An empty mold is NOT "complete" — the puzzle is only complete when
        /// every ore is sorted into a uniformly full mold. Returning true for empty
        /// would silently allow win checks to pass on a half-finished level. Callers
        /// (e.g. <c>WinLoseEvaluator</c>) skip empty molds in their loop; the few
        /// callers that need the "is this mold uniformly full?" semantic should use
        /// this method's new return value directly. Per fail-loudly policy, callers
        /// must be aware: empty ⇒ not complete.
        /// </remarks>
        public bool IsComplete(MoldState Mold)
        {
            if (Mold == null) throw new ArgumentNullException(nameof(Mold));
            if (Mold.IsEmpty) return false;
            if (!Mold.IsFull) return false;

            var layers = Mold.Layers;
            if (layers.Count == 0) return false;

            // Any empty-like layer means the mold is not complete.
            if (layers[0].IsEmpty) return false;

            var firstColor = layers[0].Color;
            int count = layers.Count;
            for (int i = 1; i < count; i++)
            {
                if (layers[i].IsEmpty) return false;
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
