using System;
using System.Collections.Generic;
using PuzzleGame.Application.Configuration;
using PuzzleGame.Application.Events;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Logging;
using PuzzleGame.Domain.Models;
using UnityEngine;

namespace PuzzleGame.Infrastructure.Implementations
{
    public sealed class PourDebugController : IPourDebugController, IDisposable
    {
        private readonly Func<IMoldView[]> _moldsProvider;
        private readonly IEventAggregator _eventAggregator;

        private AnimationConfig _animConfigRef;
        private MoldVisualConfig _visualConfigRef;
        private AnimationConfig _animOverrides;
        private MoldVisualConfig _visualOverrides;

        public bool IsDebugModeEnabled { get; set; }
        public bool IsAnimationDisabled { get; set; }

        public PourDebugController(Func<IMoldView[]> moldsProvider, IEventAggregator eventAggregator)
        {
            _moldsProvider = moldsProvider ?? throw new ArgumentNullException(nameof(moldsProvider));
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
        }

        private IMoldView[] GetMolds()
        {
            var molds = _moldsProvider();
            if (molds == null)
                throw new InvalidOperationException("Molds not set. Call SetMolds() first.");
            return molds;
        }

        private void ThrowIfMoldIndexInvalid(IMoldView[] molds, int index)
        {
            if (index < 0 || index >= molds.Length)
                throw new ArgumentOutOfRangeException(nameof(index), index,
                    $"Mold index {index} out of range [0, {molds.Length - 1}].");
        }

        public void SetConfigs(AnimationConfig animConfig, MoldVisualConfig visualConfig)
        {
            _animConfigRef = animConfig;
            _visualConfigRef = visualConfig;
        }

        public void SetMoldLayers(int moldIndex, IReadOnlyList<OreLayer> layers)
        {
            var molds = GetMolds();
            ThrowIfMoldIndexInvalid(molds, moldIndex);
            var mold = molds[moldIndex];
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
            PublishMoldMutated(mold, moldIndex);
            MoldLogger.LogInfo($"SetMoldLayers: mold[{moldIndex}] now has {state.Layers.Count} layers, fill={state.TotalFill:F3}");
        }

        public void SetMoldColor(int moldIndex, int layerIndex, DomainColor color)
        {
            var molds = GetMolds();
            ThrowIfMoldIndexInvalid(molds, moldIndex);
            var mold = molds[moldIndex];
            if (mold == null) return;

            var layers = mold.State.Layers;
            if (layerIndex < 0 || layerIndex >= layers.Count)
                throw new ArgumentOutOfRangeException(nameof(layerIndex), layerIndex,
                    $"Layer index {layerIndex} out of range for mold[{moldIndex}] with {layers.Count} layers.");

            var existing = layers[layerIndex];
            var updated = new OreLayer(color, existing.Amount);
            mold.State.ReplaceAtIndex(layerIndex, updated);
            mold.UpdateVisualsFromState();
            PublishMoldMutated(mold, moldIndex);
        }

        public void SetMoldFillAmount(int moldIndex, float fillAmount)
        {
            var molds = GetMolds();
            ThrowIfMoldIndexInvalid(molds, moldIndex);
            var mold = molds[moldIndex];
            if (mold == null) return;
            mold.SetVisualState(mold.VisualLayers, Mathf.Clamp01(fillAmount));
        }

        public void OverrideAnimationConfig(Action<AnimationConfig> apply)
        {
            if (_animOverrides == null && _animConfigRef != null)
            {
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

        public IReadOnlyList<MoldDebugState> GetAllMoldDebugStates()
        {
            var molds = _moldsProvider();
            if (molds == null) return Array.Empty<MoldDebugState>();

            var result = new MoldDebugState[molds.Length];
            for (int i = 0; i < molds.Length; i++)
            {
                var mold = molds[i];
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

        private void PublishMoldMutated(IMoldView mold, int index)
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
            _animConfigRef = null;
            _visualConfigRef = null;
            _animOverrides = null;
            _visualOverrides = null;
        }
    }
}
