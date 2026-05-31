using System.Collections.Generic;
using System.Linq;

namespace BottleShaders.Domain.Models
{
    /// <summary>
    /// Pure domain model — holds the liquid layers of a single bottle.
    /// No Unity dependencies, fully testable.
    /// </summary>
    public class BottleState
    {
        public IReadOnlyList<LiquidLayer> Layers => _layers;
        public int MaxLayers { get; }

        private readonly List<LiquidLayer> _layers;

        public BottleState(int maxLayers)
        {
            MaxLayers = maxLayers;
            _layers   = new List<LiquidLayer>(maxLayers);
        }

        // ── Computed properties ──────────────────────────────────────────────

        public float TotalFill  => _layers.Sum(l => l.Amount);
        public bool  IsEmpty    => _layers.Count == 0;

        /// <summary>
        /// A bottle is full when it has reached its maximum layer capacity
        /// AND the total fill is at or above 99 %. Both conditions must hold
        /// so that IsFull and AddLayer are always consistent.
        /// </summary>
        public bool IsFull => _layers.Count >= MaxLayers && TotalFill >= 0.99f;

        public LiquidLayer? TopLayer => IsEmpty ? (LiquidLayer?)null : _layers[_layers.Count - 1];

        // ── Mutation ─────────────────────────────────────────────────────────

        /// <summary>
        /// Adds a layer. Returns false (and does nothing) when the bottle is already full.
        /// </summary>
        public bool AddLayer(LiquidLayer layer)
        {
            if (_layers.Count >= MaxLayers) return false;
            _layers.Add(layer);
            return true;
        }

        /// <summary>Removes and returns the top layer. Returns null when empty.</summary>
        public LiquidLayer? PopTopLayer()
        {
            if (IsEmpty) return null;
            var top = _layers[_layers.Count - 1];
            _layers.RemoveAt(_layers.Count - 1);
            return top;
        }

        public void Clear() => _layers.Clear();

        public override string ToString() =>
            $"BottleState(layers={_layers.Count}/{MaxLayers}, fill={TotalFill:P0})";
    }
}
