using PuzzleGame.Application.Configuration;

namespace PuzzleGame.Application.Interfaces
{
    /// <summary>
    /// Facade for the input handler subsystem. Combines three focused contracts:
    /// <list type="bullet">
    /// <item><see cref="IMoldInputRouter"/> — input → select/cast/deselect flow</item>
    /// <item><see cref="IMoldLookupCache"/> — collider / state → mold lookup</item>
    /// <item><see cref="IInputHandlerDefaults"/> — play-test LevelData fallback</item>
    /// </list>
    /// <para>
    /// Prefer depending on the specific interface that matches your need
    /// (e.g. <c>GameManager</c> should depend on <see cref="IMoldInputRouter"/>,
    /// <c>MoldPoolInitializer</c> on <see cref="IMoldLookupCache"/>).
    /// The facade exists for backward compatibility with code that already
    /// injects <c>IInputHandlerService</c>.
    /// </para>
    /// </summary>
    public interface IInputHandlerService
        : IMoldInputRouter, IMoldLookupCache, IInputHandlerDefaults
    {
        // No new members — pure marker facade.
    }
}
