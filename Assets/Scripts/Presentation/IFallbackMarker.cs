namespace PuzzleGame.Presentation
{
    /// <summary>
    /// Marker for components that were synthesised at runtime by the DI fallback
    /// path (i.e. <c>GameInstaller.FindOrThrow</c>) instead of being authored
    /// in the scene. Used by <c>GameManager</c> to detect play-test mode without
    /// paying the per-frame cost of <c>GameObject.name.Contains("[Fallback]")</c>.
    /// </summary>
    public interface IFallbackMarker
    {
        /// <summary>True when this component was created by the DI fallback path.</summary>
        bool IsFallback { get; }

        /// <summary>
        /// Called by the DI installer when the component was synthesised at runtime.
        /// Implementations should set the internal fallback flag.
        /// </summary>
        void MarkAsFallback();
    }
}
