using UnityEngine;
using UnityEngine.VFX;
using PuzzleGame.Application.Interfaces;

namespace PuzzleGame.Tests.Fakes
{
    public class FakeStreamRenderer : IStreamRenderer
    {
        public VisualEffect EnsureEffect(GameObject owner)
        {
            var vfx = owner.GetComponent<VisualEffect>();
            if (vfx == null)
            {
                vfx = owner.AddComponent<VisualEffect>();
            }
            return vfx;
        }

        public void SetColor(VisualEffect vfx, Color color) { }

        public void Update(VisualEffect vfx, IMoldView source, IMoldView target,
                           Transform sourceT, Transform targetT, float t, PuzzleGame.Application.Configuration.AnimationConfig config) { }
    }
}
