using UnityEngine;

namespace PuzzleGame.Infrastructure.Interfaces
{
    /// <summary>
    /// Abstracts Unity's input system for raycast and pointer detection.
    /// <para>
    /// The <see cref="Raycast(Vector2,LayerMask,out RaycastHit,out Collider)"/> overload
    /// exposes the resolved <see cref="Collider"/> directly so callers never need
    /// reflection-based private-field reads on <see cref="RaycastHit"/>.
    /// </para>
    /// </summary>
    public interface IInputHandler
    {
        bool GetPointerDown(out Vector2 position);

        /// <summary>Legacy overload — prefer the four-arg version.</summary>
        bool Raycast(Vector2 screenPos, LayerMask mask, out RaycastHit hit);

        /// <summary>
        /// Preferred overload: resolves and returns the collider directly,
        /// avoiding any reflection on RaycastHit internals.
        /// </summary>
        bool Raycast(Vector2 screenPos, LayerMask mask, out RaycastHit hit, out Collider hitCollider);
    }
}
