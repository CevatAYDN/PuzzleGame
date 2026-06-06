using PuzzleGame.Application.Configuration;
using PuzzleGame.Application.Interfaces;

namespace PuzzleGame.Tests.Fakes
{
    /// <summary>
    /// Controllable fake for IActiveMoldsProvider. Mirrors the public surface used
    /// by the SUT (Molds query + ActivateOptionalMolds command).
    /// </summary>
    public class FakeActiveMoldsProvider : IActiveMoldsProvider
    {
        public IMoldView[] Molds { get; set; } = System.Array.Empty<IMoldView>();

        public int ActivateOptionalMoldsCallCount { get; private set; }
        public LevelData LastActivatedLevel { get; private set; }

        /// <summary>
        /// When set, calling ActivateOptionalMolds appends the supplied views to
        /// Molds (mirroring the real pool behaviour) so Perfect-Forge bonus tests
        /// can observe the updated array.
        /// </summary>
        public IMoldView[] OptionalMoldsToAddOnActivation { get; set; }

        public void ActivateOptionalMolds(LevelData level)
        {
            ActivateOptionalMoldsCallCount++;
            LastActivatedLevel = level;

            if (level == null || level.optionalTargets == null || level.optionalTargets.Count == 0)
                return;

            if (OptionalMoldsToAddOnActivation != null && OptionalMoldsToAddOnActivation.Length > 0)
            {
                var combined = new IMoldView[Molds.Length + OptionalMoldsToAddOnActivation.Length];
                System.Array.Copy(Molds, combined, Molds.Length);
                System.Array.Copy(OptionalMoldsToAddOnActivation, 0, combined, Molds.Length, OptionalMoldsToAddOnActivation.Length);
                Molds = combined;
            }
        }
    }
}
