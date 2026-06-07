using System.Collections.Generic;
using PuzzleGame.Application;
using PuzzleGame.Application.Configuration;
using PuzzleGame.Domain;
using PuzzleGame.Domain.Models;
using PuzzleGame.Infrastructure;

namespace PuzzleGame
{
    /// <summary>
    /// Owns MoldState lifecycle and serialized layer data used for editor preview.
    /// Extracted from MoldController for SRP (single responsibility = state).
    /// Lazy-initializes State from serialized data on first access (editor preview path).
    /// </summary>
    public sealed class MoldStateManager
    {
        private readonly List<LevelLayerData> _serializedLayers;
        private MoldState _state;
        private int _maxLayers;

        public MoldState State
        {
            get
            {
                if (_state == null)
                {
                    RestoreFromSerialized();
                }
                return _state;
            }
        }

        public int MaxLayers => _maxLayers;

        public MoldStateManager(List<LevelLayerData> serializedLayers)
        {
            _serializedLayers = serializedLayers ?? new List<LevelLayerData>();
        }

        public void Initialize(int maxLayers, IReadOnlyList<OreLayer> initialLayers)
        {
            _maxLayers = maxLayers;
            if (_state == null || _state.MaxLayers != maxLayers)
            {
                _state = new MoldState(maxLayers);
            }
            else
            {
                _state.Clear();
            }

            _serializedLayers.Clear();
            int count = initialLayers != null ? initialLayers.Count : 0;
            for (int i = 0; i < count; i++)
            {
                var layer = initialLayers[i];
                _state.AddLayer(layer);
                _serializedLayers.Add(new LevelLayerData
                {
                    color = ColorAdapter.ToUnityStatic(layer.Color),
                    amount = layer.Amount
                });
            }
        }

        public void RestoreFromSerialized()
        {
            int capacity = _maxLayers > 0 ? _maxLayers : ForgeConstants.DefaultLayerCapacity;
            _state = new MoldState(capacity);

            if (_serializedLayers == null) return;
            int count = _serializedLayers.Count;
            for (int i = 0; i < count; i++)
            {
                var layerData = _serializedLayers[i];
                var layer = new OreLayer(ColorAdapter.FromUnityStatic(layerData.color), layerData.amount);
                _state.AddLayer(layer);
            }
        }

        public IReadOnlyList<OreLayer> RebuildFromSerialized()
        {
            var result = new List<OreLayer>();
            if (_serializedLayers == null) return result;
            int count = _serializedLayers.Count;
            for (int i = 0; i < count; i++)
            {
                var layerData = _serializedLayers[i];
                result.Add(new OreLayer(ColorAdapter.FromUnityStatic(layerData.color), layerData.amount));
            }
            return result;
        }

        public void SyncSerializedFromLayers(IReadOnlyList<OreLayer> layers)
        {
            _serializedLayers.Clear();
            int count = layers != null ? layers.Count : 0;
            for (int i = 0; i < count; i++)
            {
                var layer = layers[i];
                _serializedLayers.Add(new LevelLayerData
                {
                    color = ColorAdapter.ToUnityStatic(layer.Color),
                    amount = layer.Amount
                });
            }
        }
    }
}
