using System.Collections.Generic;
using PuzzleGame.Domain;
using PuzzleGame.Domain.Models;
using UnityEngine;

namespace PuzzleGame
{
    /// <summary>
    /// Owns the visual layer list + total fill used by the renderer.
    /// Extracted from MoldController for SRP (single responsibility = visual state sync).
    /// Methods mutate _visualLayers and _visualTotalFill; renderer is updated by the controller after each call.
    /// </summary>
    public sealed class MoldVisualSync
    {
        private readonly List<OreLayer> _visualLayers = new List<OreLayer>();
        private float _visualTotalFill;

        public IReadOnlyList<OreLayer> VisualLayers => _visualLayers;
        public float VisualTotalFill => _visualTotalFill;

        public MoldVisualSync()
        {
        }

        public void Reset()
        {
            _visualLayers.Clear();
            _visualTotalFill = 0f;
        }

        public void CopyFromState(MoldState state)
        {
            _visualLayers.Clear();
            if (state == null)
            {
                _visualTotalFill = 0f;
                return;
            }
            var layers = state.Layers;
            int count = layers.Count;
            for (int i = 0; i < count; i++)
            {
                _visualLayers.Add(layers[i]);
            }
            _visualTotalFill = state.TotalFill;
        }

        public void SetVisualState(IReadOnlyList<OreLayer> layers, float totalFill)
        {
            _visualLayers.Clear();
            int count = layers != null ? layers.Count : 0;
            for (int i = 0; i < count; i++)
            {
                _visualLayers.Add(layers[i]);
            }
            _visualTotalFill = totalFill;
        }

        public void SetVisualCastProgress(LayerSnapshot startLayers, float t, bool isSource, OreLayer castedLayer)
        {
            _visualLayers.Clear();

            float startTotalFill = 0f;
            int startCount = startLayers.Count;
            for (int i = 0; i < startCount; i++)
            {
                startTotalFill += startLayers.Get(i).Amount;
            }

            if (isSource)
            {
                float totalVolumeToCast = Mathf.Max(0f, startTotalFill - _GetStateTotalFill());
                float volumeToRemove = totalVolumeToCast * t;

                for (int i = 0; i < startCount; i++)
                {
                    _visualLayers.Add(startLayers.Get(i));
                }

                for (int i = _visualLayers.Count - 1; i >= 0 && volumeToRemove > 0f; i--)
                {
                    var layer = _visualLayers[i];
                    if (layer.Amount <= volumeToRemove)
                    {
                        volumeToRemove -= layer.Amount;
                        _visualLayers.RemoveAt(i);
                    }
                    else
                    {
                        _visualLayers[i] = layer.WithAmount(layer.Amount - volumeToRemove);
                        volumeToRemove = 0f;
                    }
                }

                float totalFill = 0f;
                for (int i = _visualLayers.Count - 1; i >= 0; i--)
                {
                    var layer = _visualLayers[i];
                    if (layer.Amount <= ForgeConstants.LayerAmountEpsilon)
                    {
                        _visualLayers.RemoveAt(i);
                    }
                    else
                    {
                        totalFill += layer.Amount;
                    }
                }
                _visualTotalFill = totalFill;
            }
            else
            {
                float totalVolumeToCast = Mathf.Max(0f, _GetStateTotalFill() - startTotalFill);
                float volumeToAdd = totalVolumeToCast * t;

                float totalFill = 0f;
                for (int i = 0; i < startCount; i++)
                {
                    var layer = startLayers.Get(i);
                    _visualLayers.Add(layer);
                    totalFill += layer.Amount;
                }

                if (volumeToAdd > ForgeConstants.LayerAmountEpsilon)
                {
                    _visualLayers.Add(castedLayer.WithAmount(volumeToAdd));
                    totalFill += volumeToAdd;
                }
                _visualTotalFill = totalFill;
            }
        }

        private float _GetStateTotalFill()
        {
            return _stateTotalFillProvider != null ? _stateTotalFillProvider() : _visualTotalFill;
        }

        private System.Func<float> _stateTotalFillProvider;

        public void BindStateTotalFillProvider(System.Func<float> provider)
        {
            _stateTotalFillProvider = provider;
        }
    }
}
