using UnityEngine;
using UnityEngine.VFX;

namespace PuzzleGame.Application.Interfaces
{
    /// <summary>
    /// Abstraction for VisualEffect-based stream rendering.
    /// Converted from LineRenderer to VFX Graph for AAA Magma Flow.
    /// </summary>
    public interface IStreamRenderer
    {
        VisualEffect EnsureEffect(GameObject owner);
        void SetColor(VisualEffect vfx, Color color);
        void Update(VisualEffect vfx, IMoldView source, IMoldView target,
                    Transform sourceT, Transform targetT, float t, Configuration.AnimationConfig config);
    }
}
