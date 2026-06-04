using System;

namespace PuzzleGame.Domain.Models
{
    /// <summary>
    /// Readonly snapshot of a mold's full state for debug display and tooling.
    /// Pure C# — no UnityEngine dependency. Zero allocation on copy (value type).
    /// Used by <see cref="IPourSystemController.GetAllMoldDebugStates"/>,
    /// the DebugOverlayUI, and the Pouring Lab editor tab.
    /// </summary>
    public readonly struct MoldDebugState
    {
        /// <summary>Pool-assigned index of the mold.</summary>
        public int MoldIndex { get; }

        /// <summary>True when the mold has zero layers.</summary>
        public bool IsEmpty { get; }

        /// <summary>True when the mold has reached capacity.</summary>
        public bool IsFull { get; }

        /// <summary>Current number of ore layers.</summary>
        public int LayerCount { get; }

        /// <summary>Sum of all layer amounts (0.0 to 1.0).</summary>
        public float TotalFill { get; }

        /// <summary>Height of the mold (from config or mesh).</summary>
        public float MoldHeight { get; }

        /// <summary>Whether the mold is currently corked.</summary>
        public bool IsCapped { get; }

        /// <summary>
        /// Layer colors as <see cref="DomainColor"/> values.
        /// Array length equals <see cref="LayerCount"/>.
        /// Null if the mold state was unavailable.
        /// </summary>
        public DomainColor[] LayerColors { get; }

        /// <summary>
        /// Layer amounts (0.0 to 1.0 each).
        /// Array length equals <see cref="LayerCount"/>.
        /// Null if the mold state was unavailable.
        /// </summary>
        public float[] LayerAmounts { get; }

        public MoldDebugState(
            int moldIndex,
            bool isEmpty,
            bool isFull,
            int layerCount,
            float totalFill,
            float moldHeight,
            bool isCapped,
            DomainColor[] layerColors,
            float[] layerAmounts)
        {
            MoldIndex = moldIndex;
            IsEmpty = isEmpty;
            IsFull = isFull;
            LayerCount = layerCount;
            TotalFill = totalFill;
            MoldHeight = moldHeight;
            IsCapped = isCapped;
            LayerColors = layerColors ?? Array.Empty<DomainColor>();
            LayerAmounts = layerAmounts ?? Array.Empty<float>();
        }

        /// <summary>
        /// Creates an "unavailable" state for a mold that could not be read.
        /// </summary>
        public static MoldDebugState Unavailable(int moldIndex)
        {
            return new MoldDebugState(moldIndex, false, false, 0, 0f, 0f, false, null, null);
        }

        public override string ToString()
        {
            return $"Mold[{MoldIndex}] layers={LayerCount} fill={TotalFill:F3} empty={IsEmpty} full={IsFull} capped={IsCapped}";
        }
    }
}
