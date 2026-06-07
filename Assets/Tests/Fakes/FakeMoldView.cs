using System.Collections.Generic;
using UnityEngine;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Configuration;
using PuzzleGame.Domain.Interfaces;
using PuzzleGame.Domain.Models;
// IRendererService now in PuzzleGame.Application.Interfaces

namespace PuzzleGame.Tests.Fakes
{
    /// <summary>
    /// Reusable fake IMoldView for unit tests.
    /// Takes a MoldState in constructor, all visual methods are no-ops.
    /// </summary>
    public class FakeMoldView : IMoldView
    {
        public MoldState State { get; }
        public bool IsEmpty => State.IsEmpty;
        public bool IsCapped { get; set; }
        public int MoldIndex { get; set; }
        public Transform Transform { get; set; }
        public GameObject GameObject { get; set; }
        public Collider Collider => GameObject != null ? GameObject.GetComponent<Collider>() : null;
        public float Height { get; set; } = 2f;
        public IReadOnlyList<OreLayer> VisualLayers => State.Layers;
        public float VisualTotalFill => State.TotalFill;

        // Call records
        public int InitializeCallCount { get; private set; }
        public int SetSelectionHighlightCallCount { get; private set; }
        public int AnimateCompletionCallCount { get; private set; }
        public int UpdateVisualsFromStateCallCount { get; private set; }
        public int PlaySettleBounceCallCount { get; private set; }
        public MoldVisualConfig LastVisualConfigOverride { get; private set; }

        public FakeMoldView(MoldState state)
        {
            State = state;
        }

        public void Initialize(IRendererService rendererService,
            IMoldValidator validator, IAnimationService animationService,
            List<OreLayer> initialLayers,
            MoldVisualConfig visualConfigOverride = null,
            ITweenService tweenService = null)
        {
            InitializeCallCount++;
            LastVisualConfigOverride = visualConfigOverride;
            State.Clear();
            if (initialLayers != null)
            {
                foreach (var l in initialLayers)
                    State.AddLayer(l);
            }
        }

        public void SetSelectionHighlight(bool active)
        {
            SetSelectionHighlightCallCount++;
        }

        public void AnimateCompletion()
        {
            AnimateCompletionCallCount++;
        }

        public void UpdateVisualsFromState()
        {
            UpdateVisualsFromStateCallCount++;
        }

        public void SetVisualState(IReadOnlyList<OreLayer> layers, float totalFill) { }
        public void SetVisualCastProgress(LayerSnapshot startLayers, float t, bool isSource, OreLayer CastedLayer) { }
        public void PlaySettleBounce() { PlaySettleBounceCallCount++; }
        public void AddWobbleImpulse(Vector3 direction, float strength) { }
    }
}
