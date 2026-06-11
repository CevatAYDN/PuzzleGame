using System;
using System.Collections.Generic;
using PuzzleGame.Application.Events;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Logging;
using PuzzleGame.Domain.Models;

namespace PuzzleGame.Application.Services
{
    public class PowerUpService : IPowerUpService
    {
        private const string LogTag = "[PowerUpService]";

        private readonly IEventAggregator _events;
        private readonly IAnimationService _animationService;
        private readonly IChargeStorageService _chargeStorage;
        private readonly IRandomProvider _randomProvider;
        private readonly Dictionary<PowerUpType, int> _charges = new Dictionary<PowerUpType, int>();

        private static readonly PowerUpDescriptor[] Descriptors =
        {
            new PowerUpDescriptor(PowerUpType.Shuffle,   "powerup_shuffle",    50,  5),
            new PowerUpDescriptor(PowerUpType.ExtraMold, "powerup_extra_mold", 100, 3),
            new PowerUpDescriptor(PowerUpType.UndoLayer, "powerup_undo_layer", 30,  5),
            new PowerUpDescriptor(PowerUpType.Hint,      "powerup_hint",       20,  5),
            new PowerUpDescriptor(PowerUpType.ColorBomb, "powerup_color_bomb", 150, 2),
        };

        public PowerUpService(
            IEventAggregator events,
            IAnimationService animationService,
            IChargeStorageService chargeStorage,
            IRandomProvider randomProvider = null)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _animationService = animationService;
            _chargeStorage = chargeStorage ?? throw new ArgumentNullException(nameof(chargeStorage));
            _randomProvider = randomProvider ?? new SystemRandomProviderDefault();
            LoadCharges();
        }

        public int GetCharges(PowerUpType type)
        {
            return _charges.TryGetValue(type, out int count) ? count : 0;
        }

        public void AddCharges(PowerUpType type, int count)
        {
            if (count <= 0) return;

            if (!_charges.ContainsKey(type))
                _charges[type] = 0;

            int max = GetMaxCharges(type);
            _charges[type] = Math.Min(_charges[type] + count, max);
            SaveCharges();
        }

        public bool CanActivate(PowerUpType type)
        {
            if (_animationService != null && _animationService.IsAnimating)
                return false;
            return GetCharges(type) > 0;
        }

        public bool Activate(PowerUpType type, int moldIndex = -1)
        {
            if (_animationService != null && _animationService.IsAnimating)
            {
                MoldLogger.LogWarning($"{LogTag} Power-up activation blocked: animation in progress.");
                return false;
            }
            if (!CanActivate(type)) return false;

            _charges[type]--;
            SaveCharges();

            _events.Publish(new PowerUpActivatedEvent(type, moldIndex));
            return true;
        }

        public void ResetAll()
        {
            _charges.Clear();
            foreach (var desc in Descriptors)
            {
                _charges[desc.Type] = desc.MaxCharges;
            }
            SaveCharges();
        }

        public PowerUpDescriptor[] GetAllDescriptors() => Descriptors;

        public void ApplyColorBomb(IActiveMoldsProvider moldsProvider, int moldIndex)
        {
            var allMolds = moldsProvider.Molds;
            if (allMolds == null || moldIndex < 0 || moldIndex >= allMolds.Length)
            {
                MoldLogger.LogWarning($"{LogTag} ColorBomb: invalid mold index {moldIndex}.");
                return;
            }

            var state = allMolds[moldIndex].State;
            if (state == null || state.IsEmpty)
            {
                MoldLogger.LogWarning($"{LogTag} ColorBomb: mold is empty.");
                return;
            }

            var layers = state.Layers;
            if (layers.Count < 2) return;

            var merged = new List<OreLayer>(layers.Count);
            for (int i = 0; i < layers.Count; i++)
            {
                var current = layers[i];
                if (merged.Count > 0 && merged[merged.Count - 1].Color == current.Color)
                {
                    var prev = merged[merged.Count - 1];
                    merged[merged.Count - 1] = new OreLayer(
                        current.Color,
                        prev.Amount + current.Amount,
                        current.ColorType,
                        current.IsHidden,
                        current.Modifier);
                }
                else
                {
                    merged.Add(current);
                }
            }

            if (merged.Count < layers.Count)
            {
                state.ReplaceLayers(merged);
                allMolds[moldIndex].UpdateVisualsFromState();
                MoldLogger.LogInfo($"{LogTag} ColorBomb: merged same-color layers on mold {moldIndex}.");
            }
        }

        public void ApplyShuffle(IActiveMoldsProvider moldsProvider)
        {
            var allMolds = moldsProvider.Molds;
            if (allMolds == null || allMolds.Length == 0) return;

            var allLayers = new List<OreLayer>();
            foreach (var mold in allMolds)
            {
                if (mold?.State == null) continue;
                foreach (var layer in mold.State.Layers)
                    allLayers.Add(layer);
                mold.State.Clear();
            }

            if (allLayers.Count == 0) return;

            for (int i = allLayers.Count - 1; i > 0; i--)
            {
                int j = _randomProvider.Next(i + 1);
                (allLayers[i], allLayers[j]) = (allLayers[j], allLayers[i]);
            }

            int layerIdx = 0;
            foreach (var mold in allMolds)
            {
                if (mold?.State == null) continue;
                int take = Math.Min(allLayers.Count - layerIdx, mold.State.MaxLayers);
                if (take <= 0) break;
                var moldLayers = allLayers.GetRange(layerIdx, take);
                mold.State.ReplaceLayers(moldLayers);
                mold.UpdateVisualsFromState();
                layerIdx += take;
            }

            MoldLogger.LogInfo($"{LogTag} Shuffle: redistributed {allLayers.Count} layers across {allMolds.Length} molds.");
        }

        private int GetMaxCharges(PowerUpType type)
        {
            foreach (var d in Descriptors)
                if (d.Type == type) return d.MaxCharges;
            return 5;
        }

        private void LoadCharges()
        {
            foreach (var desc in Descriptors)
            {
                int saved = _chargeStorage.GetCharge(desc.Type, desc.MaxCharges);
                _charges[desc.Type] = Math.Max(0, saved);
            }
        }

        private void SaveCharges()
        {
            foreach (var kv in _charges)
            {
                _chargeStorage.SetCharge(kv.Key, kv.Value);
            }
            _chargeStorage.Save();
        }

        private sealed class SystemRandomProviderDefault : IRandomProvider
        {
            private readonly Random _rng = new Random();
            public int Next(int maxValue) => _rng.Next(maxValue);
        }
    }
}
