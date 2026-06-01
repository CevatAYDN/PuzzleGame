using System;
using System.Collections.Generic;

namespace PuzzleGame.Domain.Models
{
    /// <summary>
    /// Value-type snapshot of liquid layers (up to BottleState.MaxSupportedLayers).
    /// Indexer erişimi sabit sayıda slot üzerinden yapılır.
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
            if (layers == null)
            {
                Count = 0;
                Layer0 = default; Layer1 = default; Layer2 = default; Layer3 = default;
                return;
            }

            int count = layers.Count;
            if (count > BottleState.MaxSupportedLayers)
            {
                throw new ArgumentException(
                    $"LayerSnapshot supports max {BottleState.MaxSupportedLayers} layers, got {count}.");
            }

            Count = count;
            Layer0 = count > 0 ? layers[0] : default;
            Layer1 = count > 1 ? layers[1] : default;
            Layer2 = count > 2 ? layers[2] : default;
            Layer3 = count > 3 ? layers[3] : default;
        }

        public LiquidLayer Get(int index)
        {
            if (index < 0 || index >= Count)
                throw new IndexOutOfRangeException(
                    $"Index {index} is out of range (Count={Count}) for LayerSnapshot.");
            switch (index)
            {
                case 0: return Layer0;
                case 1: return Layer1;
                case 2: return Layer2;
                case 3: return Layer3;
                default: return default; // unreachable
            }
        }
    }
}
