using System;
using System.Collections.Generic;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Domain.Interfaces;
using PuzzleGame.Domain.Models;
using PuzzleGame.Domain;
using PuzzleGame.Application.Configuration;
using UnityEngine;

namespace PuzzleGame.Presentation
{
    /// <summary>
    /// Pure data adapter for a Mold. Implements IMoldView without Unity lifecycle methods.
    /// Delegates to inner POCOs (StateManager, VisualSync, Animator, VisualRenderer).
    /// </summary>
    public sealed class MoldViewAdapter : IMoldView
    {
        private readonly MoldStateManager _stateManager;
        private readonly MoldVisualSync _visualSync;
        private readonly MoldVisualRenderer _visualRenderer;
        private readonly MoldAnimator _animator;
        private readonly MoldCorkController _corkController;

        public MoldState State => _stateManager?.State;
        public IReadOnlyList<OreLayer> VisualLayers => _visualSync?.VisualLayers;
        public float VisualTotalFill => _visualSync?.VisualTotalFill ?? 0f;
        public bool IsEmpty => State?.IsEmpty ?? true;
        public bool IsFull() => State?.IsFull ?? false;
        public bool IsCapped => _corkController != null && _corkController.IsCapped;

        public Transform Transform { get; }
        public GameObject GameObject { get; }
        public Collider Collider { get; }
        public float Height { get; }
        public int MoldIndex { get; set; }

        public MoldViewAdapter(
            Transform transform,
            GameObject gameObject,
            Collider collider,
            float height,
            MoldStateManager stateManager,
            MoldVisualSync visualSync,
            MoldVisualRenderer visualRenderer,
            MoldAnimator animator,
            MoldCorkController corkController)
        {
            Transform = transform;
            GameObject = gameObject;
            Collider = collider;
            Height = height;

            _stateManager = stateManager;
            _visualSync = visualSync;
            _visualRenderer = visualRenderer;
            _animator = animator;
            _corkController = corkController;
        }

        public void Initialize(IRendererService rendererService, IMoldValidator validator, IAnimationService animationService, List<OreLayer> initialLayers, MoldVisualConfig visualConfigOverride = null, ITweenService tweenService = null)
        {
            // Initialization is handled by Bootstrapper now.
            // This is just to satisfy the IMoldView interface if called externally, 
            // though it shouldn't be since Bootstrapper wires it.
        }

        public void SetSelectionHighlight(bool active)
        {
            _visualRenderer?.SetSelectionHighlight(active);
        }

        public void AnimateCompletion()
        {
            _animator?.AnimateCompletion();
        }

        public void UpdateVisualsFromState()
        {
            if (State == null) return;
            _visualSync?.CopyFromState(State);
            _stateManager?.SyncSerializedFromLayers(_visualSync.VisualLayers);
            _visualRenderer?.Update();
        }

        public void SetVisualState(IReadOnlyList<OreLayer> layers, float totalFill)
        {
            _visualSync?.SetVisualState(layers, totalFill);
            _stateManager?.SyncSerializedFromLayers(layers);
            _visualRenderer?.Update();
        }

        public void SetVisualCastProgress(LayerSnapshot startLayers, float t, bool isSource, OreLayer CastedLayer)
        {
            _visualSync?.SetVisualCastProgress(startLayers, t, isSource, CastedLayer);
            _visualRenderer?.Update();
        }

        public void PlaySettleBounce()
        {
            // Requires IMoldView reference, we pass 'this'
            _animator?.PlaySettleBounce(this);
        }

        public void AddWobbleImpulse(Vector3 direction, float strength)
        {
            _animator?.AddWobbleImpulse(direction, strength);
        }
        
        public void UpdateVisuals()
        {
            _visualRenderer?.Update();
        }
    }
}
