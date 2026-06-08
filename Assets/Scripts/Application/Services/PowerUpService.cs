using System;
using System.Collections.Generic;
using PuzzleGame.Application.Events;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Domain.Models;

namespace PuzzleGame.Application.Services
{
    /// <summary>
    /// Default power-up service implementation.
    /// Manages charge inventory and delegates activation to gameplay systems
    /// via events (IPourSystemController, IMoldSpawner, etc. listen).
    /// Charges are persisted via PlayerPrefs.
    /// </summary>
    public class PowerUpService : IPowerUpService
    {
        private readonly IEventAggregator _events;
        private readonly Dictionary<PowerUpType, int> _charges = new Dictionary<PowerUpType, int>();

        private static readonly PowerUpDescriptor[] Descriptors =
        {
            new PowerUpDescriptor(PowerUpType.Shuffle,   "powerup_shuffle",    50,  5),
            new PowerUpDescriptor(PowerUpType.ExtraMold, "powerup_extra_mold", 100, 3),
            new PowerUpDescriptor(PowerUpType.UndoLayer, "powerup_undo_layer", 30,  5),
            new PowerUpDescriptor(PowerUpType.Hint,      "powerup_hint",       20,  5),
            new PowerUpDescriptor(PowerUpType.ColorBomb, "powerup_color_bomb", 150, 2),
        };

        public PowerUpService(IEventAggregator events)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
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
            return GetCharges(type) > 0;
        }

        public bool Activate(PowerUpType type, int moldIndex = -1)
        {
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
                int saved = UnityEngine.PlayerPrefs.GetInt($"PowerUp_{desc.Type}", desc.MaxCharges);
                _charges[desc.Type] = Math.Max(0, saved);
            }
        }

        private void SaveCharges()
        {
            foreach (var kv in _charges)
            {
                UnityEngine.PlayerPrefs.SetInt($"PowerUp_{kv.Key}", kv.Value);
            }
            UnityEngine.PlayerPrefs.Save();
        }
    }
}
