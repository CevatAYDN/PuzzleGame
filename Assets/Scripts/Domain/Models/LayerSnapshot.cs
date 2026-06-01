using System;
using System.Collections.Generic;

namespace BottleShaders.Domain.Models
{
    /// <summary>
    /// Value-type snapshot of liquid layers.
    /// Captures a copy of the layers (up to MaxSupportedLayers) without heap allocations.
    /// </summary>
    public readonly struct LayerSnapshot
    {
        public readonly int Count;
        public readonly LiquidLayer Layer0;
        public readonly LiquidLayer Layer1;
        public readonly LiquidLayer Layer2;
        public readonly LiquidLayer Layer3;

        public LayerSnapshot(IReadOnlyList<LiquidLayer> layers)
        {
            Count = layers != null ? layers.Count : 0;
            Layer0 = Count > 0 ? layers[0] : default;
            Layer1 = Count > 1 ? layers[1] : default;
            Layer2 = Count > 2 ? layers[2] : default;
            Layer3 = Count > 3 ? layers[3] : default;
        }

        public LiquidLayer Get(int index)
        {
            switch (index)
            {
                case 0: return Layer0;
                case 1: return Layer1;
                case 2: return Layer2;
                case 3: return Layer3;
                default: throw new IndexOutOfRangeException($"Index {index} is out of range for LayerSnapshot.");
            }
        }
    }
}
