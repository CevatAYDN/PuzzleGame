using System;
using System.Collections.Generic;
using PuzzleGame.Domain;

namespace PuzzleGame.Domain.Models
{
    /// <summary>
    /// Value-type snapshot of Ore layers (up to <see cref="ForgeConstants.MaxLayers"/>).
    /// Backing storage is a flat array — OCP-safe: capacity grows with the constant,
    /// not by adding more hardcoded slots.
    /// </summary>
    public readonly struct LayerSnapshot
    {
        public readonly int Count;
        private readonly OreLayer _l0;
        private readonly OreLayer _l1;
        private readonly OreLayer _l2;
        private readonly OreLayer _l3;
        private readonly OreLayer _l4;
        private readonly OreLayer _l5;
        private readonly OreLayer _l6;
        private readonly OreLayer _l7;

        public LayerSnapshot(IReadOnlyList<OreLayer> layers)
        {
            if (layers == null)
            {
                Count = 0;
                _l0 = default; _l1 = default; _l2 = default; _l3 = default;
                _l4 = default; _l5 = default; _l6 = default; _l7 = default;
                return;
            }

            int count = layers.Count;
            if (count > ForgeConstants.MaxLayers)
            {
                throw new ArgumentException(
                    $"LayerSnapshot supports max {ForgeConstants.MaxLayers} layers, got {count}.");
            }

            Count = count;
            _l0 = count > 0 ? layers[0] : default;
            _l1 = count > 1 ? layers[1] : default;
            _l2 = count > 2 ? layers[2] : default;
            _l3 = count > 3 ? layers[3] : default;
            _l4 = count > 4 ? layers[4] : default;
            _l5 = count > 5 ? layers[5] : default;
            _l6 = count > 6 ? layers[6] : default;
            _l7 = count > 7 ? layers[7] : default;
        }

        /// <exception cref="IndexOutOfRangeException">If index &lt; 0 or &gt;= Count.</exception>
        public OreLayer Get(int index)
        {
            if (index < 0 || index >= Count)
            {
                throw new IndexOutOfRangeException(
                    $"Index {index} is out of range (Count={Count}) for LayerSnapshot.");
            }
            switch (index)
            {
                case 0: return _l0;
                case 1: return _l1;
                case 2: return _l2;
                case 3: return _l3;
                case 4: return _l4;
                case 5: return _l5;
                case 6: return _l6;
                case 7: return _l7;
                default: throw new IndexOutOfRangeException();
            }
        }
    }
}
