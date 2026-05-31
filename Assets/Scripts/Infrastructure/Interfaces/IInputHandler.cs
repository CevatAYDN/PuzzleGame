using UnityEngine;

namespace BottleShaders.Infrastructure.Interfaces
{
    public interface IInputHandler
    {
        bool GetPointerDown(out Vector2 position);
        bool Raycast(Vector2 screenPos, LayerMask mask, out RaycastHit hit);
    }
}
