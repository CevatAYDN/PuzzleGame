using System;
using System.Collections.Generic;
using PuzzleGame.Application.Configuration;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Domain.Models;

namespace PuzzleGame.Infrastructure.Implementations
{
    /// <summary>
    /// Developer-facing pour system controller facade.
    /// Delegates specific responsibilities to focused sub-services:
    /// <list type="bullet">
    /// <item><see cref="PourSimulator"/> for previewing and execution of pours</item>
    /// <item><see cref="PourHistoryService"/> for snapshot recording and restore (undo)</item>
    /// <item><see cref="PourDebugController"/> for developer-only direct mutation and config overrides</item>
    /// </list>
    /// Composed to satisfy the consolidated <see cref="IPourSystemController"/> interface.
    /// </summary>
    public class PourSystemController : IPourSystemController, IDisposable
    {
        private IMoldView[] _molds;
        private readonly PourSimulator _simulator;
        private readonly PourHistoryService _historyService;
        private readonly PourDebugController _debugController;

        public bool IsDebugModeEnabled
        {
            get => _debugController.IsDebugModeEnabled;
            set => _debugController.IsDebugModeEnabled = value;
        }

        public bool IsAnimationDisabled
        {
            get => _debugController.IsAnimationDisabled;
            set => _debugController.IsAnimationDisabled = value;
        }

        public PourSystemController(
            ICastService castService,
            IAnimationService animationService,
            IEventAggregator eventAggregator)
        {
            // Note: animationService is unused in business logic but kept in the
            // constructor signature to prevent breaking backwards compatibility
            // with existing installer modules and unit tests.
            _ = animationService ?? throw new ArgumentNullException(nameof(animationService));
            
            _historyService = new PourHistoryService(() => _molds, eventAggregator);
            _simulator = new PourSimulator(() => _molds, castService, eventAggregator);
            _debugController = new PourDebugController(() => _molds, eventAggregator);
        }

        public void SetMolds(IMoldView[] molds)
        {
            _molds = molds ?? throw new ArgumentNullException(nameof(molds));
        }

        public void SetConfigs(AnimationConfig animConfig, MoldVisualConfig visualConfig)
        {
            _debugController.SetConfigs(animConfig, visualConfig);
        }

        // ── IPourSimulator ───────────────────────────────────────────────────
        
        public PourPreviewResult PreviewPour(int sourceIndex, int targetIndex)
        {
            ThrowIfMoldsNotSet();
            return _simulator.PreviewPour(sourceIndex, targetIndex);
        }

        public bool ExecuteInstantPour(int sourceIndex, int targetIndex)
        {
            ThrowIfMoldsNotSet();
            return _simulator.ExecuteInstantPour(sourceIndex, targetIndex);
        }

        // ── IPourHistoryService ───────────────────────────────────────────────
        
        public void SnapshotAllMolds()
        {
            ThrowIfMoldsNotSet();
            _historyService.SnapshotAllMolds();
        }

        public void RestoreSnapshot()
        {
            ThrowIfMoldsNotSet();
            _historyService.RestoreSnapshot();
        }

        // ── IPourDebugController ──────────────────────────────────────────────
        
        public void SetMoldLayers(int moldIndex, IReadOnlyList<OreLayer> layers)
        {
            ThrowIfMoldsNotSet();
            _debugController.SetMoldLayers(moldIndex, layers);
        }

        public void SetMoldColor(int moldIndex, int layerIndex, DomainColor color)
        {
            ThrowIfMoldsNotSet();
            _debugController.SetMoldColor(moldIndex, layerIndex, color);
        }

        public void SetMoldFillAmount(int moldIndex, float fillAmount)
        {
            ThrowIfMoldsNotSet();
            _debugController.SetMoldFillAmount(moldIndex, fillAmount);
        }

        public void OverrideAnimationConfig(Action<AnimationConfig> apply)
        {
            _debugController.OverrideAnimationConfig(apply);
        }

        public void OverrideMoldVisualConfig(Action<MoldVisualConfig> apply)
        {
            _debugController.OverrideMoldVisualConfig(apply);
        }

        public void ClearAllOverrides()
        {
            _debugController.ClearAllOverrides();
        }

        public IReadOnlyList<MoldDebugState> GetAllMoldDebugStates()
        {
            return _debugController.GetAllMoldDebugStates();
        }

        private void ThrowIfMoldsNotSet()
        {
            if (_molds == null)
                throw new InvalidOperationException("Molds not set. Call SetMolds() first.");
        }

        public void Dispose()
        {
            _historyService?.Dispose();
            _debugController?.Dispose();
            _molds = null;
        }
    }
}
