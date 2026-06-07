using System;
using System.Collections.Generic;
using PuzzleGame.Domain;

namespace PuzzleGame.Domain.Models
{
    /// <summary>
    /// Immutable snapshot of ore layers (up to <see cref="ForgeConstants.MaxLayers"/>).
    ///
    /// Fix #7: The previous implementation used 8 hardcoded fields
    /// (<c>_l0</c>..<c>_l7</c>) which contradicted the class's OCP-safe intent:
    /// increasing <see cref="ForgeConstants.MaxLayers"/> above 8 would require
    /// editing this class again. This implementation stores a compact copied array
    /// whose size is validated against the domain constant, so the class follows
    /// MaxLayers without adding more fields.
    ///
    /// The array is private and never exposed; callers can only read through
    /// <see cref="Get"/>. That gives immutable snapshot semantics without the
    /// compile risk of hand-rolled readonly inline-array setters.
    /// </summary>
    public readonly struct LayerSnapshot
    {
        private static readonly OreLayer[] EmptyLayers = Array.Empty<OreLayer>();

        private readonly OreLayer[] _layers;

        public int Count { get; }

        public LayerSnapshot(IReadOnlyList<OreLayer> layers)
        {
            if (layers == null || layers.Count == 0)
            {
                _layers = EmptyLayers;
                Count = 0;
                return;
            }

            int count = layers.Count;
            if (count > ForgeConstants.MaxLayers)
            {
                throw new ArgumentException(
                    $"LayerSnapshot supports max {ForgeConstants.MaxLayers} layers, got {count}.",
                    nameof(layers));
            }

            _layers = new OreLayer[count];
            for (int i = 0; i < count; i++)
            {
                _layers[i] = layers[i];
            }
            Count = count;
        }

        /// <exception cref="IndexOutOfRangeException">If index &lt; 0 or &gt;= Count.</exception>
        public OreLayer Get(int index)
        {
            if (index < 0 || index >= Count)
            {
                throw new IndexOutOfRangeException(
                    $"Index {index} is out of range (Count={Count}) for LayerSnapshot.");
            }
            return _layers[index];
        }
    }
}
