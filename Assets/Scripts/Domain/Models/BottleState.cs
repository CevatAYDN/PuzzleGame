using System.Collections.Generic;
using System.Linq;

namespace PuzzleGame.Domain.Models
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
        public int LayerCount => _layers.Count;
        public bool  IsEmpty    => _layers.Count == 0;

        public bool IsFull => _layers.Count >= MaxLayers;

        public LiquidLayer? TopLayer => IsEmpty ? (LiquidLayer?)null : _layers[_layers.Count - 1];
        
        // Aliases for compatibility
        public LiquidLayer? PeekTopLayer() => TopLayer;
        
        public LiquidLayer? GetLayerAt(int index)
        {
            if (index < 0 || index >= _layers.Count) return null;
            return _layers[index];
        }

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
        
        public bool ReplaceAtIndex(int index, LiquidLayer newLayer)
        {
            if (index < 0 || index >= _layers.Count) return false;
            
            var oldLayer = _layers[index];
            _layers[index] = newLayer;
            _totalFill = _totalFill - oldLayer.Amount + newLayer.Amount;
            return true;
        }
        
        public bool RemoveAtIndex(int index)
        {
            if (index < 0 || index >= _layers.Count) return false;
            
            var removed = _layers[index];
            _layers.RemoveAt(index);
            _totalFill -= removed.Amount;
            if (_totalFill < 0.0001f) _totalFill = 0f;
            return true;
        }

        /// <summary>
        /// Snapshot'tan yükle (Undo için).
        /// Atomic: yeni veri valid değilse state hiç değişmez.
        /// </summary>
        public bool ReplaceLayers(IEnumerable<LiquidLayer> newLayers)
        {
            if (newLayers == null) return false;

            // Önce sayım — invalid ise hiçbir değişiklik yapma
            int count = 0;
            foreach (var _ in newLayers) count++;
            if (count > MaxLayers) return false;

            // Geçici listede validate et
            var temp = new List<LiquidLayer>(count);
            float total = 0f;
            foreach (var layer in newLayers)
            {
                temp.Add(layer);
                total += layer.Amount;
            }

            // Hepsi geçerli — atomik olarak state'i güncelle
            _layers.Clear();
            _layers.AddRange(temp);
            _totalFill = total;

            // Observer pattern: değişiklik bildirimi
            // BottleController gibi observer'lar bu event'i dinleyerek
            // UpdateVisualsFromState() çağırır. "Tell, don't ask" prensibi.
            OnLayersChanged?.Invoke(this);
            return true;
        }

        /// <summary>
        /// Layers değiştiğinde tetiklenen olay.
        /// UI/view observer'ları buraya abone olur (decoupling).
        /// </summary>
        public event System.Action<BottleState> OnLayersChanged;

        public override string ToString() =>
            $"BottleState(layers={_layers.Count}/{MaxLayers}, fill={TotalFill:P0})";
    }
}