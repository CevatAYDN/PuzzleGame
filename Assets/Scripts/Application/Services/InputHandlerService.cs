using PuzzleGame.Application.Configuration;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Domain.Models;

namespace PuzzleGame.Infrastructure.Implementations
{
    /// <summary>
    /// Thin facade composing the three input-handler subsystems:
    /// <list type="bullet">
    /// <item><see cref="IMoldInputRouter"/> — input routing + selection/cast flow</item>
    /// <item><see cref="IMoldLookupCache"/> — collider / state → mold lookup</item>
    /// <item><see cref="IInputHandlerDefaults"/> — play-test LevelData fallback</item>
    /// </list>
    /// Kept for backward compatibility with existing consumers
    /// (<c>GameManager</c>, <c>MoldPoolInitializer</c>) that inject
    /// <see cref="IInputHandlerService"/>. New code should depend on the
    /// specific interface it needs.
    /// </summary>
    public sealed class InputHandlerService : IInputHandlerService
    {
        private readonly IMoldInputRouter _router;
        private readonly IMoldLookupCache _lookup;
        private readonly IInputHandlerDefaults _defaults;

        public InputHandlerService(
            IMoldInputRouter router,
            IMoldLookupCache lookup,
            IInputHandlerDefaults defaults)
        {
            _router = router;
            _lookup = lookup;
            _defaults = defaults;
        }

        // ── IMoldInputRouter ────────────────────────────────────────────────
        public void ProcessInput() => _router.ProcessInput();
        public void SetLevelData(LevelData levelData) => _router.SetLevelData(levelData);
        public void DisposeDefaults() => _router.DisposeDefaults();

        // ── IMoldLookupCache ────────────────────────────────────────────────
        public void SetMolds(IMoldView[] molds) => _lookup.SetMolds(molds);
        public IMoldView FindByCollider(UnityEngine.Collider collider) => _lookup.FindByCollider(collider);
        public IMoldView FindByState(MoldState state) => _lookup.FindByState(state);

        // ── IInputHandlerDefaults ───────────────────────────────────────────
        public LevelData GetActiveLevelData(LevelData currentLevelData) =>
            _defaults.GetActiveLevelData(currentLevelData);
        public void Dispose() => _defaults.Dispose();
    }
}
