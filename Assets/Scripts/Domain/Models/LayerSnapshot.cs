using System;
using System.Collections.Generic;
using PuzzleGame.Domain;

namespace PuzzleGame.Domain.Models
{
    /// <summary>
    /// Value-type snapshot of liquid layers (up to <see cref="BottleConstants.MaxLayers"/>).
    /// Backing storage is a flat array — OCP-safe: capacity grows with the constant,
    /// not by adding more hardcoded slots.
    /// </summary>
    public readonly struct LayerSnapshot
    {
        public readonly int Count;
        private readonly LiquidLayer[] _layers;

        public LayerSnapshot(IReadOnlyList<LiquidLayer> layers)
        {
            if (layers == null)
            {
                Count = 0;
                _layers = Array.Empty<LiquidLayer>();
                return;
            }

            int count = layers.Count;
            if (count > BottleConstants.MaxLayers)
            {
                throw new ArgumentException(
                    $"LayerSnapshot supports max {BottleConstants.MaxLayers} layers, got {count}.");
            }

            Count = count;
            _layers = new LiquidLayer[count];
            for (int i = 0; i < count; i++)
            {
                _layers[i] = layers[i];
            }
        }

        /// <exception cref="IndexOutOfRangeException">If index &lt; 0 or &gt;= Count.</exception>
        public LiquidLayer Get(int index)
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
