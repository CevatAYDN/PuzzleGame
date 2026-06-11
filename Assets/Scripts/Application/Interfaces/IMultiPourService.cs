using System.Collections.Generic;
using PuzzleGame.Domain.Models;
using UnityEngine;

namespace PuzzleGame.Application.Interfaces
{
    public interface IMultiPourService
    {
        bool TryMultiPour(IReadOnlyList<MoldState> sourceStates, IMoldView target, Vector3 selectedOriginalPos, IMoldView[] activeMolds);
    }
}