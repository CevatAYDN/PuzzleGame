using UnityEngine;
using PuzzleGame.Application.Interfaces;

namespace PuzzleGame.Tests.Fakes
{
    public class FakeStreamTrailController : IStreamTrailController
    {
        public bool IsTrailActive { get; private set; }
        public int UpdateTrailCallCount { get; private set; }
        public Vector3 LastPosition { get; private set; }
        public Color LastColor { get; private set; }

        public float LastAlpha { get; private set; }
        public float TrailFadeDuration => 0.5f;
        public float TrailAlpha => 0.15f;

        public LineRenderer EnsureLineRenderer(GameObject owner)
        {
            return owner.GetComponent<LineRenderer>() ?? owner.AddComponent<LineRenderer>();
        }

        public void SetAlpha(float alpha)
        {
            LastAlpha = alpha;
        }

        public void BeginTrail(Vector3 startPosition, Color color)
        {
            IsTrailActive = true;
            LastPosition = startPosition;
            LastColor = color;
        }

        public void UpdateTrail(Vector3 worldPosition)
        {
            UpdateTrailCallCount++;
            LastPosition = worldPosition;
        }

        public void EndTrail()
        {
            IsTrailActive = false;
        }

        public void Cleanup()
        {
            IsTrailActive = false;
        }
    }
}
