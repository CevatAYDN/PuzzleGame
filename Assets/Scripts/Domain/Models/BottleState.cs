using System;
using System.Collections.Generic;
using PuzzleGame.Domain;

namespace PuzzleGame.Domain.Models
{
    /// <summary>
    /// State of a single bottle: ordered liquid layers + capacity metadata.
    /// Pure C# — no UnityEngine dependency. Lives in Domain layer.
    /// 
    /// FAIL-LOUDLY POLICY:
    ///   All mutating methods that operate on an invalid state THROW
    ///   <see cref="InvalidOperationException"/> instead of returning false/null.
    ///   Callers MUST validate the precondition (e.g. via <see cref="IBottleValidator"/>
    ///   or by checking <see cref="IsFull"/>/<see cref="IsEmpty"/>) before mutating.
    ///   Silent fallbacks hide bugs — we want them to surface.
    /// </summary>
    public class BottleState
    {
        /// <summary>Backward-compatible alias. Prefer <see cref="BottleConstants.MaxLayers"/>.</summary>
        public const int MaxSupportedLayers = BottleConstants.MaxLayers;

        public IReadOnlyList<LiquidLayer> Layers => _layers;
        public int MaxLayers { get; }

        private readonly List<LiquidLayer> _layers;
        private float _totalFill;

        /// <summary>Constructs a bottle with the given maximum layer capacity.</summary>
        /// <exception cref="ArgumentOutOfRangeException">If maxLayers &lt; 1 or &gt; BottleConstants.MaxLayers.</exception>
        public BottleState(int maxLayers)
        {
            if (maxLayers < 1 || maxLayers > BottleConstants.MaxLayers)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxLayers),
                    maxLayers,
                    $"Bottle capacity must be in [1, {BottleConstants.MaxLayers}].");
            }

            MaxLayers = maxLayers;
            _layers   = new List<LiquidLayer>(maxLayers);
            _totalFill = 0f;
        }

        public float TotalFill  => _totalFill;
        public int LayerCount => _layers.Count;
        public bool  IsEmpty    => _layers.Count == 0;

        public bool IsFull => _layers.Count >= MaxLayers;

        /// <summary>Returns the top layer. Returns null only if the bottle is empty (use IsEmpty to check).</summary>
        public LiquidLayer? TopLayer => IsEmpty ? (LiquidLayer?)null : _layers[_layers.Count - 1];

        /// <summary>Alias for <see cref="TopLayer"/>. Returns null only when empty.</summary>
        public LiquidLayer? PeekTopLayer() => TopLayer;

        /// <exception cref="ArgumentOutOfRangeException">If index is negative or &gt;= LayerCount.</exception>
        public LiquidLayer GetLayerAt(int index)
        {
            if (index < 0 || index >= _layers.Count)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(index), index, $"Layer index out of range [0, {_layers.Count}).");
            }
            return _layers[index];
        }

        /// <exception cref="InvalidOperationException">If the bottle is full (caller must check IsFull).</exception>
        public void AddLayer(LiquidLayer layer)
        {
            if (_layers.Count >= MaxLayers)
            {
                throw new InvalidOperationException(
                    $"Bottle is full ({_layers.Count}/{MaxLayers}). Caller must check IsFull before AddLayer.");
            }
            _layers.Add(layer);
            _totalFill += layer.Amount;
        }

        /// <exception cref="InvalidOperationException">If the bottle is empty (caller must check IsEmpty).</exception>
        public LiquidLayer PopTopLayer()
        {
            if (IsEmpty)
            {
                throw new InvalidOperationException(
                    "Cannot pop from empty bottle. Caller must check IsEmpty before PopTopLayer.");
            }
            var top = _layers[_layers.Count - 1];
            _layers.RemoveAt(_layers.Count - 1);
            _totalFill -= top.Amount;
            if (_totalFill < BottleConstants.TotalFillEpsilon) _totalFill = 0f;
            return top;
        }

        public void Clear()
        {
            _layers.Clear();
            _totalFill = 0f;
        }

        /// <exception cref="ArgumentOutOfRangeException">If index is negative or &gt;= LayerCount.</exception>
        public void ReplaceAtIndex(int index, LiquidLayer newLayer)
        {
            if (index < 0 || index >= _layers.Count)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(index), index, $"Layer index out of range [0, {_layers.Count}).");
            }
            var oldLayer = _layers[index];
            _layers[index] = newLayer;
            _totalFill = _totalFill - oldLayer.Amount + newLayer.Amount;
        }

        /// <exception cref="ArgumentOutOfRangeException">If index is negative or &gt;= LayerCount.</exception>
        public void RemoveAtIndex(int index)
        {
            if (index < 0 || index >= _layers.Count)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(index), index, $"Layer index out of range [0, {_layers.Count}).");
            }
            var removed = _layers[index];
            _layers.RemoveAt(index);
            _totalFill -= removed.Amount;
            if (_totalFill < BottleConstants.TotalFillEpsilon) _totalFill = 0f;
        }

        /// <summary>
        /// Replaces the layer list atomically (used for undo / snapshot restore).
        /// Fires <see cref="OnLayersChanged"/> on success.
        ///
        /// Fix #15: IEnumerable is materialized to a List in a single pass —
        /// previously enumerated twice (once for count, once to read values).
        /// </summary>
        /// <exception cref="ArgumentNullException">If newLayers is null.</exception>
        /// <exception cref="ArgumentException">If newLayers contains more than MaxLayers items.</exception>
        public void ReplaceLayers(IEnumerable<LiquidLayer> newLayers)
        {
            if (newLayers == null)
                throw new ArgumentNullException(nameof(newLayers), "Layer sequence cannot be null.");

            // Fix #15: Materialize in one pass — avoids double enumeration of IEnumerable sources
            // (e.g. LINQ pipelines that would be executed twice).
            var temp = new List<LiquidLayer>(MaxLayers);
            float total = 0f;
            foreach (var layer in newLayers)
            {
                temp.Add(layer);
                total += layer.Amount;
                if (temp.Count > MaxLayers)
                    throw new ArgumentException(
                        $"Too many layers (> {MaxLayers}).", nameof(newLayers));
            }

            // All valid — atomically update state.
            _layers.Clear();
            _layers.AddRange(temp);
            _totalFill = total;

            // Observer pattern: notify listeners (e.g. BottleController.UpdateVisualsFromState).
            OnLayersChanged?.Invoke(this);
        }


        /// <summary>
        /// Layers değiştiğinde tetiklenen olay.
        /// UI/view observer'ları buraya abone olur (decoupling).
        /// </summary>
        public event Action<BottleState> OnLayersChanged;

        public override string ToString() =>
            $"BottleState(layers={_layers.Count}/{MaxLayers}, fill={TotalFill:P0})";
    }
}