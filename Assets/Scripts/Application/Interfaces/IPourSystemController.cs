namespace PuzzleGame.Application.Interfaces
{
    /// <summary>
    /// Facade for the pour system. Combines the three focused contracts:
    /// <list type="bullet">
    /// <item><see cref="IPourSimulator"/> — gameplay preview + execute</item>
    /// <item><see cref="IPourHistoryService"/> — undo support (snapshot stack)</item>
    /// <item><see cref="IPourDebugController"/> — dev tools (mutators, overrides, queries)</item>
    /// </list>
    /// <para>
    /// Prefer depending on the specific interface that matches your need
    /// (e.g. gameplay code should depend on <see cref="IPourSimulator"/>,
    /// not this facade). The facade exists for backward compatibility
    /// with code that already injects <c>IPourSystemController</c>.
    /// </para>
    /// </summary>
    public interface IPourSystemController
        : IPourSimulator, IPourHistoryService, IPourDebugController
    {
        // No new members — pure marker facade.
    }
}
