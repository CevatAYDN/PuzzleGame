using System;
using System.Collections.Generic;
using PuzzleGame.Application.Events;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Domain.Models;

namespace PuzzleGame.Infrastructure.Implementations
{
    public sealed class PourSimulator : IPourSimulator
    {
        private readonly Func<IMoldView[]> _moldsProvider;
        private readonly ICastService _castService;
        private readonly IEventAggregator _eventAggregator;

        public PourSimulator(Func<IMoldView[]> moldsProvider, ICastService castService, IEventAggregator eventAggregator)
        {
            _moldsProvider = moldsProvider ?? throw new ArgumentNullException(nameof(moldsProvider));
            _castService = castService ?? throw new ArgumentNullException(nameof(castService));
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
        }

        private IMoldView[] GetMolds()
        {
            var molds = _moldsProvider();
            if (molds == null)
                throw new InvalidOperationException("Molds not set. Call SetMolds() first.");
            return molds;
        }

        private void ThrowIfMoldIndexInvalid(IMoldView[] molds, int index)
        {
            if (index < 0 || index >= molds.Length)
                throw new ArgumentOutOfRangeException(nameof(index), index,
                    $"Mold index {index} out of range [0, {molds.Length - 1}].");
        }

        public PourPreviewResult PreviewPour(int sourceIndex, int targetIndex)
        {
            var molds = GetMolds();
            ThrowIfMoldIndexInvalid(molds, sourceIndex);
            ThrowIfMoldIndexInvalid(molds, targetIndex);

            var source = molds[sourceIndex];
            var target = molds[targetIndex];
            if (source == null || target == null)
                return PourPreviewResult.Rejected("mold_null");

            var sourceBefore = CopyLayers(source.State.Layers);
            var targetBefore = CopyLayers(target.State.Layers);

            int sourceCount = sourceBefore.Length;
            if (sourceCount == 0)
                return PourPreviewResult.Rejected("source_empty");

            var topColor = sourceBefore[sourceCount - 1].Color;
            int layersToTransfer = 0;

            for (int i = sourceCount - 1; i >= 0; i--)
            {
                if (sourceBefore[i].Color == topColor)
                    layersToTransfer++;
                else
                    break;
            }

            int targetCount = targetBefore.Length;
            if (targetCount + layersToTransfer > target.State.MaxLayers)
                return PourPreviewResult.Rejected("target_full");

            if (targetCount > 0 && targetBefore[targetCount - 1].Color != topColor)
                return PourPreviewResult.Rejected("validator_rejected");

            var sourceAfter = new OreLayer[sourceCount - layersToTransfer];
            for (int i = 0; i < sourceAfter.Length; i++)
                sourceAfter[i] = sourceBefore[i];

            var targetAfterList = new List<OreLayer>(targetBefore);
            for (int i = sourceBefore.Length - layersToTransfer; i < sourceBefore.Length; i++)
                targetAfterList.Add(sourceBefore[i]);

            var targetAfter = targetAfterList.ToArray();

            return new PourPreviewResult(
                true, null, layersToTransfer,
                sourceBefore, targetBefore, sourceAfter, targetAfter);
        }

        public bool ExecuteInstantPour(int sourceIndex, int targetIndex)
        {
            var molds = GetMolds();
            ThrowIfMoldIndexInvalid(molds, sourceIndex);
            ThrowIfMoldIndexInvalid(molds, targetIndex);

            var source = molds[sourceIndex];
            var target = molds[targetIndex];
            if (source == null || target == null)
            {
                _eventAggregator.Publish(new PourErrorEvent(sourceIndex, targetIndex, "mold_null",
                    "One or both molds are null."));
                return false;
            }

            var preview = PreviewPour(sourceIndex, targetIndex);
            if (!preview.IsValid)
            {
                _eventAggregator.Publish(new CastRejectedEvent(sourceIndex, targetIndex, preview.RejectionReason));
                _eventAggregator.Publish(new PourErrorEvent(sourceIndex, targetIndex, preview.RejectionReason,
                    $"Cast rejected: {preview.RejectionReason}"));
                return false;
            }

            var stateSource = source.State;
            var stateTarget = target.State;
            int count = preview.LayersToTransfer;

            for (int i = 0; i < count; i++)
            {
                var transferred = stateSource.PopTopLayer();
                stateTarget.AddLayer(transferred);
            }

            source.UpdateVisualsFromState();
            target.UpdateVisualsFromState();

            _eventAggregator.Publish(new CastCompletedEvent(stateSource, stateTarget));
            PublishMoldMutated(source, sourceIndex);
            PublishMoldMutated(target, targetIndex);

            return true;
        }

        private OreLayer[] CopyLayers(IReadOnlyList<OreLayer> layers)
        {
            var result = new OreLayer[layers.Count];
            for (int i = 0; i < layers.Count; i++)
                result[i] = layers[i];
            return result;
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
    }
}
