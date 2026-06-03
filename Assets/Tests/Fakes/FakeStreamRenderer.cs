using UnityEngine;
using PuzzleGame.Application.Interfaces;

namespace PuzzleGame.Tests.Fakes
{
    public class FakeStreamRenderer : IStreamRenderer
    {
        public int TotalSegments => 18;

        public LineRenderer EnsureLineRenderer(GameObject owner)
        {
            var lr = owner.GetComponent<LineRenderer>();
            if (lr == null)
            {
                lr = owner.AddComponent<LineRenderer>();
            }
            return lr;
        }

        public void SetColor(LineRenderer lr, Color color) { }

        public void Update(LineRenderer lr, IBottleView source, IBottleView target,
                           Transform sourceT, Transform targetT, float t, PuzzleGame.Application.Configuration.AnimationConfig config) { }
    }
}
