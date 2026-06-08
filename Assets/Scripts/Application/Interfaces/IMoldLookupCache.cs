using PuzzleGame.Domain.Models;
using UnityEngine;

namespace PuzzleGame.Application.Interfaces
{
    /// <summary>
    /// O(1) lookup cache for active molds. Maps both Collider EntityId
    /// (for raycast hits) and <see cref="MoldState"/> (for selection lookup)
    /// to the corresponding <see cref="IMoldView"/>.
    ///
    /// <para>
    /// Zero dependencies — pure data structure. Decoupled from input handling
    /// so it can be reused by any other system that needs mold lookup
    /// (debug overlay, level analytics, replay system).
    /// </para>
    /// </summary>
    public interface IMoldLookupCache
    {
        /// <summary>
        /// Replaces the cached mold array. Rebuilds the collider EntityId
        /// dictionary. Safe to call with null (clears the cache).
        /// </summary>
        void SetMolds(IMoldView[] molds);

        /// <summary>
        /// Removes a single mold from the cache. Call this from
        /// <c>MoldController.OnDestroy</c> (or whenever a mold is destroyed
        /// outside of a full <see cref="SetMolds"/> rebuild) so that
        /// <see cref="FindByCollider"/> does not return a destroyed instance.
        /// </summary>
        void RemoveMold(IMoldView mold);

        /// <summary>
        /// Resolves a collider to its owning <see cref="IMoldView"/> via
        /// the EntityId cache built in <see cref="SetMolds"/>.
        /// Returns null if the collider is not a registered mold.
        /// </summary>
        IMoldView FindByCollider(Collider collider);

        /// <summary>
        /// Resolves a <see cref="MoldState"/> to its owning <see cref="IMoldView"/>
        /// via linear scan. Used by selection deselect/lower.
        /// Returns null if no mold holds the state.
        /// </summary>
        IMoldView FindByState(MoldState state);
    }
}
