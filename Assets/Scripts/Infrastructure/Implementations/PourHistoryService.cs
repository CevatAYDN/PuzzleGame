using System;
using System.Collections.Generic;
using PuzzleGame.Application.Events;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Domain.Models;

namespace PuzzleGame.Infrastructure.Implementations
{
    public sealed class PourHistoryService : IPourHistoryService, IDisposable
    {
        private readonly Func<IMoldView[]> _moldsProvider;
        private readonly IEventAggregator _eventAggregator;
        private readonly Queue<List<OreLayer>[]> _snapshots = new Queue<List<OreLayer>[]>(32);

        public PourHistoryService(Func<IMoldView[]> moldsProvider, IEventAggregator eventAggregator)
        {
            _moldsProvider = moldsProvider ?? throw new ArgumentNullException(nameof(moldsProvider));
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
        }

        private IMoldView[] GetMolds()
        {
            var molds = _moldsProvider();
            return molds;
        }

        public void SnapshotAllMolds()
        {
            var molds = GetMolds();
            if (molds == null) return;

            var snap = new List<OreLayer>[molds.Length];
            for (int i = 0; i < molds.Length; i++)
            {
                var mold = molds[i];
                snap[i] = mold != null
                    ? new List<OreLayer>(mold.State.Layers)
                    : new List<OreLayer>(0);
            }
            _snapshots.Enqueue(snap);

            while (_snapshots.Count > 32)
            {
                _snapshots.Dequeue();
            }
        }

        public void RestoreSnapshot()
        {
            var molds = GetMolds();
            if (_snapshots.Count == 0 || molds == null) return;
            var snap = _snapshots.Dequeue();

            for (int i = 0; i < snap.Length && i < molds.Length; i++)
            {
                var mold = molds[i];
                if (mold == null) continue;

                var state = mold.State;
                state.Clear();
                foreach (var layer in snap[i])
                    state.AddLayer(layer);
                mold.UpdateVisualsFromState();
                PublishMoldMutated(mold, i);
            }
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
            _snapshots.Clear();
        }
    }
}
