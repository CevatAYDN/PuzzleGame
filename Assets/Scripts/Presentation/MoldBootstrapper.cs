using System;
using System.Collections.Generic;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Domain.Interfaces;
using PuzzleGame.Domain.Models;
using PuzzleGame.Application.Configuration;
using PuzzleGame.Application.Logging;
using PuzzleGame.Application;
using UnityEngine;

namespace PuzzleGame.Presentation
{
    /// <summary>
    /// Handles composition and initialization of a Mold. Extracts the "kernel" logic
    /// out of the MoldController MonoBehaviour.
    /// </summary>
    public sealed class MoldBootstrapper
    {
        private MoldStateManager _stateManager;
        private MoldVisualSync _visualSync;
        private MoldVisualRenderer _visualRenderer;
        private MoldAnimator _animator;
        private MoldCorkController _corkController;

        public MoldViewAdapter Adapter { get; private set; }

        public MoldBootstrapper()
        {
        }

        public MoldViewAdapter Initialize(
            MoldController controller,
            IRendererService rendererService,
            IMoldValidator validator,
            IAnimationService animationService,
            List<OreLayer> initialLayers,
            MoldVisualConfig visualConfigOverride = null,
            ITweenService tweenService = null)
        {
            if (rendererService == null) throw new ArgumentNullException(nameof(rendererService));
            if (validator == null) throw new ArgumentNullException(nameof(validator));
            if (initialLayers == null) throw new ArgumentNullException(nameof(initialLayers));

            var visualConfig = controller.visualConfig ?? visualConfigOverride;
            if (visualConfig == null)
            {
#if UNITY_EDITOR
                if (!UnityEngine.Application.isPlaying) return null;
#endif
                throw new InvalidOperationException($"[MoldBootstrapper] MoldVisualConfig is not assigned on {controller.gameObject.name}!");
            }

            var renderer = controller.GetComponent<Renderer>();
            var wobble = controller.GetComponent<Wobble>();
            var meshGenerator = controller.GetComponent<MoldMeshGenerator>();

            if (renderer == null) MoldLogger.LogError("Renderer missing", controller);

            float height = meshGenerator != null ? meshGenerator.height : visualConfig.moldHeight;
            int maxLayers = visualConfig.maxLayers;

            _stateManager = new MoldStateManager(controller.GetSerializedLayers());
            _stateManager.Initialize(maxLayers, initialLayers);

            _visualSync = new MoldVisualSync();
            _visualSync.BindStateTotalFillProvider(() => _stateManager.State?.TotalFill ?? 0f);

            _visualRenderer = new MoldVisualRenderer(
                renderer, rendererService, visualConfig,
                () => _visualSync != null ? (List<OreLayer>)_visualSync.VisualLayers : new List<OreLayer>(),
                () => _visualSync != null ? _visualSync.VisualTotalFill : 0f);

            _corkController = new MoldCorkController(
                controller.transform, animationService,
                () => height,
                () => meshGenerator != null ? meshGenerator.neckRadius : CorkConstants.Radius,
                controller.corkObject);
            _corkController.EnsureCork();
            controller.corkObject = _corkController.CorkObject;

            _animator = new MoldAnimator(renderer, animationService, wobble, visualConfig, _corkController);

            Adapter = new MoldViewAdapter(
                controller.transform,
                controller.gameObject,
                controller.Collider,
                height,
                _stateManager,
                _visualSync,
                _visualRenderer,
                _animator,
                _corkController);

            Adapter.MoldIndex = controller.MoldIndex;
            
            _visualSync.CopyFromState(_stateManager.State);
            Adapter.SetSelectionHighlight(false);
            Adapter.UpdateVisuals();

            return Adapter;
        }

        public MoldViewAdapter RestoreStateFromSerialized(
            MoldController controller,
            IRendererService rendererService,
            IMoldValidator validator,
            IAnimationService animationService,
            bool isFromOnValidate)
        {
            var visualConfig = controller.visualConfig;
            if (visualConfig == null) return null;

            var renderer = controller.GetComponent<Renderer>();
            var wobble = controller.GetComponent<Wobble>();
            var meshGenerator = controller.GetComponent<MoldMeshGenerator>();
            float height = meshGenerator != null ? meshGenerator.height : visualConfig.moldHeight;

            int maxLayers = visualConfig.maxLayers;

            if (_stateManager == null) _stateManager = new MoldStateManager(controller.GetSerializedLayers());
            _stateManager.Initialize(maxLayers, _stateManager.RebuildFromSerialized());

            if (_visualSync == null) _visualSync = new MoldVisualSync();
            _visualSync.BindStateTotalFillProvider(() => _stateManager.State?.TotalFill ?? 0f);

            if (_corkController == null)
            {
                _corkController = new MoldCorkController(
                    controller.transform, animationService,
                    () => height,
                    () => meshGenerator != null ? meshGenerator.neckRadius : CorkConstants.Radius,
                    controller.corkObject);
            }
            _corkController.EnsureCork(isFromOnValidate);
            controller.corkObject = _corkController.CorkObject;

            if (_visualRenderer == null && renderer != null && rendererService != null)
            {
                _visualRenderer = new MoldVisualRenderer(
                    renderer, rendererService, visualConfig,
                    () => _visualSync != null ? (List<OreLayer>)_visualSync.VisualLayers : new List<OreLayer>(),
                    () => _visualSync != null ? _visualSync.VisualTotalFill : 0f);
            }

            if (_animator == null && renderer != null)
            {
                _animator = new MoldAnimator(renderer, animationService, wobble, visualConfig, _corkController);
            }

            _visualSync.CopyFromState(_stateManager.State);
            _visualRenderer?.Update();

            if (Adapter == null)
            {
                Adapter = new MoldViewAdapter(
                    controller.transform,
                    controller.gameObject,
                    controller.Collider,
                    height,
                    _stateManager,
                    _visualSync,
                    _visualRenderer,
                    _animator,
                    _corkController);
            }

            return Adapter;
        }

        public void Dispose(MoldController controller, ITweenService tweenService)
        {
            tweenService?.StopAll(controller.transform);
            if (controller.corkObject != null) tweenService?.StopAll(controller.corkObject.transform);
            _corkController?.DisposeResources();
            _visualRenderer = null;
        }
    }
}
