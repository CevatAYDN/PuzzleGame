using UnityEngine;

namespace PuzzleGame.Application.Interfaces
{
    /// <summary>
    /// Abstraction for LineRenderer-based stream rendering.
    /// Converted from static StreamRenderer to instance class in Infrastructure.
    /// </summary>
    public interface IStreamRenderer
    {
        int TotalSegments { get; }
        LineRenderer EnsureLineRenderer(GameObject owner);
        void SetColor(LineRenderer lr, Color color);
        void Update(LineRenderer lr, IBottleView source, IBottleView target,
                    Transform sourceT, Transform targetT, float t, Configuration.AnimationConfig config);
    }
}
