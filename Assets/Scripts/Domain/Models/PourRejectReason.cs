namespace PuzzleGame.Domain.Models
{
    /// <summary>
    /// Enumerated rejection reasons for pour attempts. Use the static
    /// <see cref="PourRejectReasons"/> helper to obtain the string code that
    /// <see cref="PourPreviewResult.RejectionReason"/> accepts — this keeps
    /// magic strings out of call sites while preserving the existing API.
    /// </summary>
    public enum PourRejectReason
    {
        Unknown = 0,
        SourceEmpty,
        TargetFull,
        ValidatorRejected,
        NoMatchingLayers,
        SameMold,
        InvalidMoldIndex,
    }

    /// <summary>
    /// String mapping for <see cref="PourRejectReason"/>. Keeps the wire format
    /// stable (snake_case) while letting call sites use the enum for safety.
    /// </summary>
    public static class PourRejectReasons
    {
        public const string SourceEmpty = "source_empty";
        public const string TargetFull = "target_full";
        public const string ValidatorRejected = "validator_rejected";
        public const string NoMatchingLayers = "no_matching_layers";
        public const string SameMold = "same_mold";
        public const string InvalidMoldIndex = "invalid_mold_index";

        /// <summary>Maps the enum back to the snake_case string used by <see cref="PourPreviewResult"/>.</summary>
        public static string ToCode(PourRejectReason reason)
        {
            switch (reason)
            {
                case PourRejectReason.SourceEmpty:        return SourceEmpty;
                case PourRejectReason.TargetFull:         return TargetFull;
                case PourRejectReason.ValidatorRejected:  return ValidatorRejected;
                case PourRejectReason.NoMatchingLayers:   return NoMatchingLayers;
                case PourRejectReason.SameMold:           return SameMold;
                case PourRejectReason.InvalidMoldIndex:   return InvalidMoldIndex;
                default:                                  return "unknown";
            }
        }

        /// <summary>Best-effort reverse mapping. Returns <see cref="PourRejectReason.Unknown"/> when the code is not recognised.</summary>
        public static PourRejectReason FromCode(string code)
        {
            if (string.IsNullOrEmpty(code)) return PourRejectReason.Unknown;
            switch (code)
            {
                case SourceEmpty:        return PourRejectReason.SourceEmpty;
                case TargetFull:         return PourRejectReason.TargetFull;
                case ValidatorRejected:  return PourRejectReason.ValidatorRejected;
                case NoMatchingLayers:   return PourRejectReason.NoMatchingLayers;
                case SameMold:           return PourRejectReason.SameMold;
                case InvalidMoldIndex:   return PourRejectReason.InvalidMoldIndex;
                default:                 return PourRejectReason.Unknown;
            }
        }
    }
}
