using System;
using System.Collections.Generic;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Configuration;
using PuzzleGame.Application.Events;
using PuzzleGame.Application.Logging;
using PuzzleGame.Domain;
using PuzzleGame.Domain.Interfaces;
using PuzzleGame.Domain.Models;
using UnityEngine;

namespace PuzzleGame.Infrastructure.Implementations
{
    /// <summary>
    /// Developer-facing pour system controller.
    /// Provides runtime mold manipulation, pour simulation, config overrides,
    /// state snapshots, and debug mode flags.
    ///
    /// Plain C# class — no MonoBehaviour. Registered as singleton via DI.
    /// Molds array is set via <see cref="SetMolds"/> after pool initialization.
    /// </summary>
    public class PourSystemController : IPourSystemController, IDisposable
    {
        private readonly ICastService _castService;
        private readonly IAnimationService _animationService;
        private readonly IEventAggregator _eventAggregator;
        private IMoldView[] _molds;

        // ── Config Overrides ──────────────────────────────────────────────────
        private AnimationConfig _animConfigRef;
        private MoldVisualConfig _visualConfigRef;
        private AnimationConfig _animOverrides;
        private MoldVisualConfig _visualOverrides;

        // ── Snapshot Stack ────────────────────────────────────────────────────
        // Each snapshot is a serialized MoldState copy for every active mold.
        //
        // Fix #9: Previously a bounded `Stack<T>` with manual rotation on overflow.
        // The rotation logic was wrong: `Stack<T>.ToArray()` returns the stack
        // contents in LIFO order (top → bottom), but the rotation code reused the
        // existing top-of-stack as the new top, which kept the most-recent snapshot
        // at the BOTTOM and evicted the *second*-most-recent instead of the oldest.
        // A `Queue<T>` is the right primitive here — FIFO with a single
        // `Dequeue()` call to drop the oldest entry when capacity is exceeded.
        private readonly Queue<List<OreLayer>[]> _snapshots = new Queue<List<OreLayer>[]>(32);

        // ── Debug Flags ───────────────────────────────────────────────────────
        public bool IsDebugModeEnabled { get; set; }
        public bool IsAnimationDisabled { get; set; }

        public PourSystemController(
            ICastService castService,
            IAnimationService animationService,
            IEventAggregator eventAggregator)
        {
            _castService = castService ?? throw new ArgumentNullException(nameof(castService));
            _animationService = animationService ?? throw new ArgumentNullException(nameof(animationService));
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
        }

        /// <summary>
        /// Called by GameManager after mold pool initialization.
        /// </summary>
        public void SetMolds(IMoldView[] molds)
        {
            _molds = molds ?? throw new ArgumentNullException(nameof(molds));
        }

        /// <summary>
        /// Stores references to the active configs so overrides can be applied.
        /// Called by GameManager after configs are loaded.
        /// </summary>
        public void SetConfigs(AnimationConfig animConfig, MoldVisualConfig visualConfig)
        {
            _animConfigRef = animConfig;
            _visualConfigRef = visualConfig;
        }

        // ── Mold State Manipulation ──────────────────────────────────────────

        public void SetMoldLayers(int moldIndex, IReadOnlyList<OreLayer> layers)
        {
            ThrowIfMoldIndexInvalid(moldIndex);
            var mold = _molds[moldIndex];
            if (mold == null) return;

            var state = mold.State;
            state.Clear();
            if (layers != null)
            {
                foreach (var layer in layers)
                {
                    state.AddLayer(layer);
                }
            }

            mold.UpdateVisualsFromState();
            PublishMoldMutated(moldIndex, mold);
            MoldLogger.LogInfo($"SetMoldLayers: mold[{moldIndex}] now has {state.Layers.Count} layers, fill={state.TotalFill:F3}");
        }

        public void SetMoldColor(int moldIndex, int layerIndex, DomainColor color)
        {
            ThrowIfMoldIndexInvalid(moldIndex);
            var mold = _molds[moldIndex];
            if (mold == null) return;

            var layers = mold.State.Layers;
            if (layerIndex < 0 || layerIndex >= layers.Count)
                throw new ArgumentOutOfRangeException(nameof(layerIndex), layerIndex,
                    $"Layer index {layerIndex} out of range for mold[{moldIndex}] with {layers.Count} layers.");

            var existing = layers[layerIndex];
            var updated = new OreLayer(color, existing.Amount);
            mold.State.ReplaceAtIndex(layerIndex, updated);
            mold.UpdateVisualsFromState();
            PublishMoldMutated(moldIndex, mold);
        }

        public void SetMoldFillAmount(int moldIndex, float fillAmount)
        {
            ThrowIfMoldIndexInvalid(moldIndex);
            var mold = _molds[moldIndex];
            if (mold == null) return;
            mold.SetVisualState(mold.VisualLayers, Mathf.Clamp01(fillAmount));
        }

        // ── Pour Simulation ──────────────────────────────────────────────────

        public PourPreviewResult PreviewPour(int sourceIndex, int targetIndex)
        {
            ThrowIfMoldIndexInvalid(sourceIndex);
            ThrowIfMoldIndexInvalid(targetIndex);

            var source = _molds[sourceIndex];
            var target = _molds[targetIndex];
            if (source == null || target == null)
                return PourPreviewResult.Rejected("mold_null");

            // Deep-copy current state
            var sourceBefore = CopyLayers(source.State.Layers);
            var targetBefore = CopyLayers(target.State.Layers);

            // Simulate: find top source layers that match
            int sourceCount = sourceBefore.Length;
            if (sourceCount == 0)
                return PourPreviewResult.Rejected("source_empty");

            var topColor = sourceBefore[sourceCount - 1].Color;
            int layersToTransfer = 0;

            // Count consecutive matching layers from top
            for (int i = sourceCount - 1; i >= 0; i--)
            {
                if (sourceBefore[i].Color == topColor)
                    layersToTransfer++;
                else
                    break;
            }

            // Check if target has room
            int targetCount = targetBefore.Length;
            if (targetCount + layersToTransfer > target.State.MaxLayers)
                return PourPreviewResult.Rejected("target_full");

            // Check if target top matches or is empty
            if (targetCount > 0 && targetBefore[targetCount - 1].Color != topColor)
                return PourPreviewResult.Rejected("validator_rejected");

            // Build predicted state
            var sourceAfter = new OreLayer[sourceCount - layersToTransfer];
            for (int i = 0; i < sourceAfter.Length; i++)
                sourceAfter[i] = sourceBefore[i];

            var targetAfterList = new List<OreLayer>(targetBefore);
            for (int i = sourceBefore.Length - layersToTransfer; i < sourceBefore.Length; i++)
                targetAfterList.Add(sourceBefore[i]);

            var targetAfter = targetAfterList.ToArray();

            return new PourPreviewResult(
                true, null, layersToTransfer,
                sourceBefore, targetBefore, sourceAfter, targetAfter);
        }

        public bool ExecuteInstantPour(int sourceIndex, int targetIndex)
        {
            ThrowIfMoldIndexInvalid(sourceIndex);
            ThrowIfMoldIndexInvalid(targetIndex);

            var source = _molds[sourceIndex];
            var target = _molds[targetIndex];
            if (source == null || target == null)
            {
                _eventAggregator.Publish(new PourErrorEvent(sourceIndex, targetIndex, "mold_null",
                    "One or both molds are null."));
                return false;
            }

            // Snapshot for history before executing
            SnapshotAllMolds();

            var preview = PreviewPour(sourceIndex, targetIndex);
            if (!preview.IsValid)
            {
                _eventAggregator.Publish(new CastRejectedEvent(sourceIndex, targetIndex, preview.RejectionReason));
                _eventAggregator.Publish(new PourErrorEvent(sourceIndex, targetIndex, preview.RejectionReason,
                    $"Cast rejected: {preview.RejectionReason}"));
                return false;
            }

            // Execute: pop from source, push to target
            var stateSource = source.State;
            var stateTarget = target.State;
            int count = preview.LayersToTransfer;
            var transferred = new OreLayer[count];

            for (int i = 0; i < count; i++)
            {
                transferred[i] = stateSource.PopTopLayer();
                stateTarget.AddLayer(transferred[i]);
            }

            source.UpdateVisualsFromState();
            target.UpdateVisualsFromState();

            _eventAggregator.Publish(new CastCompletedEvent(stateSource, stateTarget));
            PublishMoldMutated(sourceIndex, source);
            PublishMoldMutated(targetIndex, target);

            MoldLogger.LogInfo($"ExecuteInstantPour: {count} layers from mold[{sourceIndex}] to mold[{targetIndex}]");
            return true;
        }

        // ── Config Overrides ─────────────────────────────────────────────────

        public void OverrideAnimationConfig(Action<AnimationConfig> apply)
        {
            if (_animOverrides == null && _animConfigRef != null)
            {
                // First override: clone the current config
                _animOverrides = UnityEngine.ScriptableObject.Instantiate(_animConfigRef);
            }
            apply?.Invoke(_animOverrides ?? _animConfigRef);
        }

        public void OverrideMoldVisualConfig(Action<MoldVisualConfig> apply)
        {
            if (_visualOverrides == null && _visualConfigRef != null)
            {
                _visualOverrides = UnityEngine.ScriptableObject.Instantiate(_visualConfigRef);
            }
            apply?.Invoke(_visualOverrides ?? _visualConfigRef);
        }

        /// <summary>
        /// Returns true when the controller is in editor (non-play) mode. Config
        /// overrides (which clone the source ScriptableObject) are cheap to skip
        /// outside the editor and risky in shipping builds if accidentally wired
        /// up — they leak the clone past the current scope unless <see cref="ClearAllOverrides"/>
        /// is called.
        /// </summary>
        public bool IsEditorOverrideContext =>
#if UNITY_EDITOR
            !UnityEngine.Application.isPlaying;
#else
            false;
#endif

        public void ClearAllOverrides()
        {
            if (_animOverrides != null)
            {
                if (_animOverrides != _animConfigRef)
                    UnityEngine.Object.Destroy(_animOverrides);
                _animOverrides = null;
            }
            if (_visualOverrides != null)
            {
                if (_visualOverrides != _visualConfigRef)
                    UnityEngine.Object.Destroy(_visualOverrides);
                _visualOverrides = null;
            }
            MoldLogger.LogDebug("Config overrides cleared.");
        }

        // ── State History ────────────────────────────────────────────────────

        public void SnapshotAllMolds()
        {
            if (_molds == null) return;

            var snap = new List<OreLayer>[_molds.Length];
            for (int i = 0; i < _molds.Length; i++)
            {
                var mold = _molds[i];
                snap[i] = mold != null
                    ? new List<OreLayer>(mold.State.Layers)
                    : new List<OreLayer>(0);
            }
            _snapshots.Enqueue(snap);

            // Fix #9: Simple FIFO eviction — drop the oldest snapshot when full.
            // The previous Stack-based rotation kept the newest at the bottom and
            // popped the second-newest, which silently broke undo ordering.
            while (_snapshots.Count > 32)
            {
                _snapshots.Dequeue();
            }
        }

        public void RestoreSnapshot()
        {
            if (_snapshots.Count == 0) return;
            var snap = _snapshots.Dequeue();

            for (int i = 0; i < snap.Length && i < (_molds?.Length ?? 0); i++)
            {
                var mold = _molds[i];
                if (mold == null) continue;

                var state = mold.State;
                state.Clear();
                foreach (var layer in snap[i])
                    state.AddLayer(layer);
                mold.UpdateVisualsFromState();
                PublishMoldMutated(i, mold);
            }
            MoldLogger.LogInfo($"RestoreSnapshot: {_snapshots.Count} snapshots remaining.");
        }

        // ── Debug Queries ────────────────────────────────────────────────────

        public IReadOnlyList<MoldDebugState> GetAllMoldDebugStates()
        {
            if (_molds == null) return Array.Empty<MoldDebugState>();

            var result = new MoldDebugState[_molds.Length];
            for (int i = 0; i < _molds.Length; i++)
            {
                var mold = _molds[i];
                if (mold == null)
                {
                    result[i] = MoldDebugState.Unavailable(i);
                    continue;
                }

                var state = mold.State;
                var layers = state.Layers;
                var colors = new DomainColor[layers.Count];
                var amounts = new float[layers.Count];
                for (int j = 0; j < layers.Count; j++)
                {
                    colors[j] = layers[j].Color;
                    amounts[j] = layers[j].Amount;
                }

                result[i] = new MoldDebugState(
                    mold.MoldIndex,
                    state.IsEmpty,
                    state.IsFull,
                    layers.Count,
                    state.TotalFill,
                    mold.Height,
                    mold.IsCapped,
                    colors,
                    amounts);
            }
            return result;
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private void ThrowIfMoldIndexInvalid(int index)
        {
            if (_molds == null)
                throw new InvalidOperationException("Molds not set. Call SetMolds() first.");
            if (index < 0 || index >= _molds.Length)
                throw new ArgumentOutOfRangeException(nameof(index), index,
                    $"Mold index {index} out of range [0, {_molds.Length - 1}].");
        }

        private OreLayer[] CopyLayers(IReadOnlyList<OreLayer> layers)
        {
            var result = new OreLayer[layers.Count];
            for (int i = 0; i < layers.Count; i++)
                result[i] = layers[i];
            return result;
        }

        private void PublishMoldMutated(int index, IMoldView mold)
        {
            var state = mold.State;
            var layers = state.Layers;
            var colors = new DomainColor[layers.Count];
            var amounts = new float[layers.Count];
            for (int j = 0; j < layers.Count; j++)
            {
                colors[j] = layers[j].Color;
                amounts[j] = layers[j].Amount;
            }
            var debugState = new MoldDebugState(
                index, state.IsEmpty, state.IsFull, layers.Count,
                state.TotalFill, mold.Height, mold.IsCapped, colors, amounts);

            _eventAggregator.Publish(new MoldStateMutatedEvent(index, debugState));
        }

        public void Dispose()
        {
            ClearAllOverrides();
            _snapshots.Clear();
            _molds = null;
            _animConfigRef = null;
            _visualConfigRef = null;
            _animOverrides = null;
            _visualOverrides = null;
        }
    }
}
