using System;

namespace PuzzleGame.Domain.Models
{
    /// <summary>
    /// Holds the predicted outcome of a pour attempt before execution.
    /// Pure C# readonly struct — no UnityEngine dependency.
    /// Used by <see cref="IPourSystemController.PreviewPour"/> and
    /// the Pouring Lab editor tab for live preview.
    /// </summary>
    public readonly struct PourPreviewResult
    {
        /// <summary>Whether the pour would succeed according to the validator.</summary>
        public bool IsValid { get; }

        /// <summary>
        /// Stable rejection reason code (e.g. "source_empty", "validator_rejected", "no_matching_layers").
        /// Null when <see cref="IsValid"/> is true.
        /// </summary>
        public string RejectionReason { get; }

        /// <summary>Number of layers that would be transferred (0 if invalid).</summary>
        public int LayersToTransfer { get; }

        /// <summary>Copies of source layers before the pour.</summary>
        public OreLayer[] SourceLayersBefore { get; }

        /// <summary>Copies of target layers before the pour.</summary>
        public OreLayer[] TargetLayersBefore { get; }

        /// <summary>Predicted source layers after the pour.</summary>
        public OreLayer[] SourceLayersAfter { get; }

        /// <summary>Predicted target layers after the pour.</summary>
        public OreLayer[] TargetLayersAfter { get; }

        /// <summary>
        /// Creates a valid pour preview with full before/after state.
        /// Arrays are stored as-is — caller must not mutate them afterwards.
        /// </summary>
        public PourPreviewResult(
            bool isValid,
            string rejectionReason,
            int layersToTransfer,
            OreLayer[] sourceBefore,
            OreLayer[] targetBefore,
            OreLayer[] sourceAfter,
            OreLayer[] targetAfter)
        {
            IsValid = isValid;
            RejectionReason = rejectionReason;
            LayersToTransfer = layersToTransfer;
            SourceLayersBefore = sourceBefore ?? Array.Empty<OreLayer>();
            TargetLayersBefore = targetBefore ?? Array.Empty<OreLayer>();
            SourceLayersAfter = sourceAfter ?? Array.Empty<OreLayer>();
            TargetLayersAfter = targetAfter ?? Array.Empty<OreLayer>();
        }

        /// <summary>Factory for a rejected pour with a reason code.</summary>
        public static PourPreviewResult Rejected(string reason)
        {
            if (string.IsNullOrEmpty(reason))
                throw new ArgumentNullException(nameof(reason));
            return new PourPreviewResult(false, reason, 0, null, null, null, null);
        }

        public override string ToString()
        {
            return IsValid
                ? $"PourPreview: {LayersToTransfer} layers will transfer"
                : $"PourPreview: REJECTED ({RejectionReason})";
        }
    }
}
