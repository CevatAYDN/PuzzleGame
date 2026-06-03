using System.Collections.Generic;
using UnityEngine;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Domain.Interfaces;
using PuzzleGame.Domain.Models;
// IRendererService now in PuzzleGame.Application.Interfaces

namespace PuzzleGame.Tests.Fakes
{
    /// <summary>
    /// Reusable fake IBottleView for unit tests.
    /// Takes a BottleState in constructor, all visual methods are no-ops.
    /// </summary>
    public class FakeBottleView : IBottleView
    {
        public BottleState State { get; }
        public bool IsEmpty => State.IsEmpty;
        public bool IsCapped { get; set; }
        public int BottleIndex { get; set; }
        public Transform Transform { get; set; }
        public GameObject GameObject { get; set; }
        public float Height { get; set; } = 2f;
        public IReadOnlyList<LiquidLayer> VisualLayers => State.Layers;
        public float VisualTotalFill => State.TotalFill;

        // Call records
        public int InitializeCallCount { get; private set; }
        public int TryPourToCallCount { get; private set; }
        public int SetSelectionHighlightCallCount { get; private set; }
        public int AnimateCompletionCallCount { get; private set; }
        public int UpdateVisualsFromStateCallCount { get; private set; }
        public int PlaySettleBounceCallCount { get; private set; }

        public FakeBottleView(BottleState state)
        {
            State = state;
        }

        public void Initialize(IRendererService rendererService,
            IBottleValidator validator, IAnimationService animationService,
            List<LiquidLayer> initialLayers)
        {
            InitializeCallCount++;
            State.Clear();
            if (initialLayers != null)
            {
                foreach (var l in initialLayers)
                    State.AddLayer(l);
            }
        }

        public bool TryPourTo(IBottleView target)
        {
            TryPourToCallCount++;
            return false;
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

        public void SetVisualState(IReadOnlyList<LiquidLayer> layers, float totalFill) { }
        public void SetVisualPourProgress(LayerSnapshot startLayers, float t, bool isSource, LiquidLayer pouredLayer) { }
        public void PlaySettleBounce() { PlaySettleBounceCallCount++; }
        public void AddWobbleImpulse(Vector3 direction, float strength) { }
    }
}
