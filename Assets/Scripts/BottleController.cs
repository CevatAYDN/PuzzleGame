using UnityEngine;
using BottleShaders.Infrastructure.Interfaces;
using BottleShaders.Domain.Models;
using BottleShaders.Domain.Interfaces;
using BottleShaders.Logging;
using System.Collections.Generic;

namespace BottleShaders
{
    [RequireComponent(typeof(Renderer))]
    public class BottleController : MonoBehaviour
    {
        [Header("Materials (assigned by BottleMeshGenerator or Editor tool)")]
        public Material glassMaterial;
        public Material liquidMaterial;

        [Header("Visual Tuning")]
        [SerializeField] private float saturationBoost = 1.35f;
        [SerializeField] private float brightnessBoost = 1.2f;

        [Header("Bottle Capacity")]
        [SerializeField] private int maxLayers = 4;

        public BottleState State { get; private set; }

        private IRendererService  _rendererService;
        private IBottleValidator  _validator;
        private Renderer          _renderer;

        public void Initialize(IRendererService rendererService,
                               IBottleValidator  validator,
                               List<LiquidLayer> initialLayers)
        {
            _rendererService = rendererService;
            _validator       = validator;
            _renderer        = GetComponent<Renderer>();

            State = new BottleState(maxLayers);
            foreach (var layer in initialLayers)
                State.AddLayer(layer);

            BottleLogger.LogDebug($"Bottle '{name}' initialized with {initialLayers.Count} layers.");
            UpdateVisuals();
        }

        public bool IsEmpty() => State?.IsEmpty ?? true;
        public bool IsFull()  => State?.IsFull  ?? false;

        public bool HasSingleColorContent()
        {
            if (State == null || State.IsEmpty || State.Layers.Count == 0) return true;
            var firstColor = State.Layers[0].Color;
            for (int i = 1; i < State.Layers.Count; i++)
                if (!_validator.ColorsMatch(State.Layers[i].Color, firstColor)) return false;
            return true;
        }

        public bool TryPourTo(BottleController target)
        {
            if (target == null)
            {
                BottleLogger.LogWarning($"'{name}': TryPourTo called with null target.");
                return false;
            }

            if (!_validator.CanPour(State, target.State))
            {
                BottleLogger.LogDebug($"'{name}' → '{target.name}': pour rejected by validator.");
                return false;
            }

            var layer = State.PopTopLayer();
            if (layer == null)
            {
                BottleLogger.LogError($"'{name}': validator allowed pour but PopTopLayer returned null.");
                return false;
            }

            bool added = target.State.AddLayer(layer.Value);
            if (!added)
            {
                State.AddLayer(layer.Value);
                BottleLogger.LogError($"'{name}' → '{target.name}': AddLayer failed after validator approval. Rolled back.");
                return false;
            }

            BottleLogger.LogInfo($"Poured {layer.Value.Color} from '{name}' to '{target.name}'.");
            UpdateVisuals();
            target.UpdateVisuals();
            return true;
        }

        public void UpdateVisuals()
        {
            if (_rendererService == null)
            {
                BottleLogger.LogWarning($"'{name}': UpdateVisuals called before Initialize.");
                return;
            }

            if (_renderer == null)
                _renderer = GetComponent<Renderer>();

            _rendererService.UpdateLiquid(_renderer, State, saturationBoost, brightnessBoost);
            _rendererService.UpdateGlass(_renderer, State);
        }
    }
}