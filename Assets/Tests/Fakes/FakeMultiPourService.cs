using System.Collections.Generic;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Domain.Models;
using UnityEngine;

namespace PuzzleGame.Tests.Fakes
{
    public class FakeMultiPourService : IMultiPourService
    {
        public bool TryMultiPourResult { get; set; } = true;
        public int TryMultiPourCallCount { get; private set; }

        public bool TryMultiPour(IReadOnlyList<MoldState> sourceStates, IMoldView target, Vector3 selectedOriginalPos, IMoldView[] activeMolds)
        {
            TryMultiPourCallCount++;
            return TryMultiPourResult;
        }
    }
}
