using UnityEngine;
using PuzzleGame.Application.Interfaces;

namespace PuzzleGame.Tests.Fakes
{
    /// <summary>
    /// Controllable fake for IInputHandler. All input is programmatically controlled.
    /// </summary>
    public class FakeInputHandler : IInputHandler
    {
        public bool GetPointerDownResult { get; set; }
        public Vector2 PointerPosition { get; set; }

        public bool RaycastResult { get; set; } = true;
        public RaycastHit RaycastHitResult { get; set; }
        public Collider RaycastColliderResult { get; set; }

        public int GetPointerDownCallCount { get; private set; }
        public int RaycastCallCount { get; private set; }

        public Vector2 LastRaycastScreenPos { get; private set; }
        public LayerMask LastRaycastLayerMask { get; private set; }

        public bool GetPointerDown(out Vector2 screenPos)
        {
            GetPointerDownCallCount++;
            screenPos = PointerPosition;
            return GetPointerDownResult;
        }

        public bool Raycast(Vector2 screenPos, LayerMask layerMask, out RaycastHit hit)
        {
            RaycastCallCount++;
            LastRaycastScreenPos = screenPos;
            LastRaycastLayerMask = layerMask;
            hit = RaycastHitResult;
            return RaycastResult;
        }

        public bool Raycast(Vector2 screenPos, LayerMask layerMask, out RaycastHit hit, out Collider hitCollider)
        {
            bool result = Raycast(screenPos, layerMask, out hit);
            hitCollider = RaycastColliderResult != null ? RaycastColliderResult : (result && RaycastHitResult.collider != null ? RaycastHitResult.collider : null);
            return result;
        }

        public void SimulateClick(Vector2 pos, bool raycastSuccess = true, RaycastHit hit = default, Collider collider = null)
        {
            GetPointerDownResult = true;
            PointerPosition = pos;
            RaycastResult = raycastSuccess;
            RaycastHitResult = hit;
            RaycastColliderResult = collider;
        }

        public void Reset()
        {
            GetPointerDownResult = false;
            RaycastResult = true;
            RaycastColliderResult = null;
            GetPointerDownCallCount = 0;
            RaycastCallCount = 0;
        }
    }
}
