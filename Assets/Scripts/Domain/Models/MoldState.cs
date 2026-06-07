using System;
using System.Collections.Generic;
using PuzzleGame.Domain;

namespace PuzzleGame.Domain.Models
{
    /// <summary>
    /// State of a single Mold: ordered Ore layers + capacity metadata.
    /// Pure C# — no UnityEngine dependency. Lives in Domain layer.
    /// 
    /// FAIL-LOUDLY POLICY:
    ///   All mutating methods that operate on an invalid state THROW
    ///   <see cref="InvalidOperationException"/> instead of returning false/null.
    ///   Callers MUST validate the precondition (e.g. via <see cref="IMoldValidator"/>
    ///   or by checking <see cref="IsFull"/>/<see cref="IsEmpty"/>) before mutating.
    ///   Silent fallbacks hide bugs — we want them to surface.
    /// </summary>
    public class MoldState
    {
        /// <summary>Backward-compatible alias. Prefer <see cref="ForgeConstants.MaxLayers"/>.</summary>
        public const int MaxSupportedLayers = ForgeConstants.MaxLayers;

        public IReadOnlyList<OreLayer> Layers => _layers;
        public int MaxLayers { get; }

        private readonly List<OreLayer> _layers;
        private float _totalFill;

        /// <summary>Constructs a Mold with the given maximum layer capacity.</summary>
        /// <exception cref="ArgumentOutOfRangeException">If maxLayers &lt; 1 or &gt; ForgeConstants.MaxLayers.</exception>
        public MoldState(int maxLayers)
        {
            if (maxLayers < 1 || maxLayers > ForgeConstants.MaxLayers)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxLayers),
                    maxLayers,
                    $"Mold capacity must be in [1, {ForgeConstants.MaxLayers}].");
            }

            MaxLayers = maxLayers;
            _layers   = new List<OreLayer>(maxLayers);
            _totalFill = 0f;
        }

        public float TotalFill  => _totalFill;
        public int LayerCount => _layers.Count;
        public bool  IsEmpty    => _layers.Count == 0;

        public bool IsFull => _layers.Count >= MaxLayers;

        /// <summary>Returns the top layer. Returns null only if the Mold is empty (use IsEmpty to check).</summary>
        public OreLayer? TopLayer => IsEmpty ? (OreLayer?)null : _layers[_layers.Count - 1];

        /// <summary>Alias for <see cref="TopLayer"/>. Returns null only when empty.</summary>
        public OreLayer? PeekTopLayer() => TopLayer;

        /// <exception cref="ArgumentOutOfRangeException">If index is negative or &gt;= LayerCount.</exception>
        public OreLayer GetLayerAt(int index)
        {
            if (index < 0 || index >= _layers.Count)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(index), index, $"Layer index out of range [0, {_layers.Count}).");
            }
            return _layers[index];
        }

        /// <exception cref="InvalidOperationException">If the Mold is full (caller must check IsFull).</exception>
        public void AddLayer(OreLayer layer)
        {
            if (_layers.Count >= MaxLayers)
            {
                throw new InvalidOperationException(
                    $"Mold is full ({_layers.Count}/{MaxLayers}). Caller must check IsFull before AddLayer.");
            }
            _layers.Add(layer);
            _totalFill += layer.Amount;
            // Fix #8: Notify observers on every mutating method, not only ReplaceLayers.
            // Subscribers (e.g. MoldController visual sync) need to know about AddLayer
            // and PopTopLayer too, otherwise visuals desync from state on normal casts.
            OnLayersChanged?.Invoke(this);
        }

        /// <exception cref="InvalidOperationException">If the Mold is empty (caller must check IsEmpty).</exception>
        public OreLayer PopTopLayer()
        {
            if (IsEmpty)
            {
                throw new InvalidOperationException(
                    "Cannot pop from empty Mold. Caller must check IsEmpty before PopTopLayer.");
            }
            var top = _layers[_layers.Count - 1];
            _layers.RemoveAt(_layers.Count - 1);
            _totalFill -= top.Amount;
            if (_totalFill < ForgeConstants.TotalFillEpsilon) _totalFill = 0f;
            // Fix #8: see AddLayer.
            OnLayersChanged?.Invoke(this);
            return top;
        }

        public void Clear()
        {
            _layers.Clear();
            _totalFill = 0f;
            // Fix #8: see AddLayer.
            OnLayersChanged?.Invoke(this);
        }

        /// <exception cref="ArgumentOutOfRangeException">If index is negative or &gt;= LayerCount.</exception>
        public void ReplaceAtIndex(int index, OreLayer newLayer)
        {
            if (index < 0 || index >= _layers.Count)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(index), index, $"Layer index out of range [0, {_layers.Count}).");
            }
            var oldLayer = _layers[index];
            _layers[index] = newLayer;
            _totalFill = _totalFill - oldLayer.Amount + newLayer.Amount;
            // Fix #8: see AddLayer.
            OnLayersChanged?.Invoke(this);
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
            if (_totalFill < ForgeConstants.TotalFillEpsilon) _totalFill = 0f;
            // Fix #8: see AddLayer.
            OnLayersChanged?.Invoke(this);
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
        public void ReplaceLayers(IEnumerable<OreLayer> newLayers)
        {
            if (newLayers == null)
                throw new ArgumentNullException(nameof(newLayers), "Layer sequence cannot be null.");

            // Fix #15: Materialize in one pass — avoids double enumeration of IEnumerable sources
            // (e.g. LINQ pipelines that would be executed twice).
            var temp = new List<OreLayer>(MaxLayers);
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

            // Observer pattern: notify listeners (e.g. MoldController.UpdateVisualsFromState).
            OnLayersChanged?.Invoke(this);
        }


        /// <summary>
        /// Layers değiştiğinde tetiklenen olay.
        /// UI/view observer'ları buraya abone olur (decoupling).
        /// </summary>
        public event Action<MoldState> OnLayersChanged;

        public override string ToString() =>
            $"MoldState(layers={_layers.Count}/{MaxLayers}, fill={TotalFill:P0})";
    }
}