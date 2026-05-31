using BottleShaders.Domain.Interfaces;
using BottleShaders.Domain.Models;
using System.Linq;
using UnityEngine;

namespace BottleShaders.Domain.Services
{
    /// <summary>
    /// Single source of truth for all pour / completion rules.
    /// Pure C# — no MonoBehaviour, no Unity scene dependencies.
    /// </summary>
    public class BottleValidationService : IBottleValidator
    {
        private readonly float _colorTolerance;

        public BottleValidationService(float colorTolerance = 0.05f)
        {
            _colorTolerance = colorTolerance;
        }

        // ── IBottleValidator ─────────────────────────────────────────────────

        /// <inheritdoc/>
        public bool CanPour(BottleState source, BottleState target)
        {
            if (source == null || target == null) return false;
            if (source == target)                 return false;
            if (source.IsEmpty)                   return false;
            if (target.IsFull)                    return false;

            // Empty target always accepts any color
            if (target.IsEmpty) return true;

            // Colors must match
            return ColorsMatch(source.TopLayer!.Value.Color, target.TopLayer!.Value.Color);
        }

        /// <inheritdoc/>
        public bool IsComplete(BottleState bottle)
        {
            if (bottle == null)   return false;
            if (bottle.IsEmpty)   return true;
            if (!bottle.IsFull)   return false;

            var firstColor = bottle.Layers[0].Color;
            return bottle.Layers.All(l => ColorsMatch(l.Color, firstColor));
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        public bool ColorsMatch(Color a, Color b) =>
            Mathf.Abs(a.r - b.r) < _colorTolerance &&
            Mathf.Abs(a.g - b.g) < _colorTolerance &&
            Mathf.Abs(a.b - b.b) < _colorTolerance;
    }
}
