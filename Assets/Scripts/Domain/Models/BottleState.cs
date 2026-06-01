using System.Collections.Generic;
using System.Linq;

namespace BottleShaders.Domain.Models
{
    public class BottleState
    {
        public const int MaxSupportedLayers = 4;

        public IReadOnlyList<LiquidLayer> Layers => _layers;
        public int MaxLayers { get; }

        private readonly List<LiquidLayer> _layers;
        private float _totalFill;

        public BottleState(int maxLayers)
        {
            MaxLayers = maxLayers;
            _layers   = new List<LiquidLayer>(maxLayers);
            _totalFill = 0f;
        }

        public float TotalFill  => _totalFill;
        public bool  IsEmpty    => _layers.Count == 0;

        public bool IsFull => _layers.Count >= MaxLayers;

        public LiquidLayer? TopLayer => IsEmpty ? (LiquidLayer?)null : _layers[_layers.Count - 1];

        public bool AddLayer(LiquidLayer layer)
        {
            if (_layers.Count >= MaxLayers) return false;
            _layers.Add(layer);
            _totalFill += layer.Amount;
            return true;
        }

        public LiquidLayer? PopTopLayer()
        {
            if (IsEmpty) return null;
            var top = _layers[_layers.Count - 1];
            _layers.RemoveAt(_layers.Count - 1);
            _totalFill -= top.Amount;
            if (_totalFill < 0.0001f) _totalFill = 0f;
            return top;
        }

        public void Clear()
        {
            _layers.Clear();
            _totalFill = 0f;
        }

        public override string ToString() =>
            $"BottleState(layers={_layers.Count}/{MaxLayers}, fill={TotalFill:P0})";
    }
}