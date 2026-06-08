using System;
using System.Collections.Generic;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Domain.Models;
using UnityEngine;

namespace PuzzleGame.Infrastructure.Implementations
{
    /// <summary>
    /// O(1) collider→mold and O(n) state→mold lookup. Pure data structure
    /// with no dependencies on input / animation / state machine.
    /// See <see cref="IMoldLookupCache"/> for the contract.
    /// </summary>
    public sealed class MoldLookupCache : IMoldLookupCache
    {
        private IMoldView[] _molds;
        // EntityId is Unity 6's stable per-object identifier (replaces the deprecated
        // int GetInstanceID()). It is a value type with proper IEquatable<EntityId>/
        // GetHashCode, so it's safe as a Dictionary key.
        private readonly Dictionary<EntityId, IMoldView> _byColliderId = new Dictionary<EntityId, IMoldView>();

        public void SetMolds(IMoldView[] molds)
        {
            _molds = molds;
            _byColliderId.Clear();
            if (molds == null) return;
            for (int i = 0; i < molds.Length; i++)
            {
                var view = molds[i];
                if (view?.GameObject == null) continue;
                // Use cached Collider from IMoldView (Fix #2)
                var col = view.Collider;
                if (col != null)
                    _byColliderId[col.GetEntityId()] = view;
            }
        }

        public IMoldView FindByCollider(Collider collider)
        {
            if (collider == null) return null;
            // Fast path: O(1) EntityId lookup
            if (_byColliderId.TryGetValue(collider.GetEntityId(), out var view))
                return view;
            
            // Fallback: Linear search for test environments where EntityId may not match
            // (e.g., when Collider is set directly via test setup without proper EntityId)
            if (_molds == null) return null;
            for (int i = 0; i < _molds.Length; i++)
            {
                var mold = _molds[i];
                if (mold?.Collider != null && ReferenceEquals(mold.Collider, collider))
                    return mold;
            }
            return null;
        }

        public IMoldView FindByState(MoldState state)
        {
            if (state == null || _molds == null) return null;
            for (int i = 0; i < _molds.Length; i++)
            {
                var b = _molds[i];
                if (b != null && b.State == state) return b;
            }
            return null;
        }

        public void RemoveMold(IMoldView mold)
        {
            if (mold == null || _molds == null) return;

            // Drop from the array (replace with null so FindByState short-circuits).
            for (int i = 0; i < _molds.Length; i++)
            {
                if (ReferenceEquals(_molds[i], mold))
                {
                    _molds[i] = null;
                    break;
                }
            }

            // Drop from the collider dictionary so a stale EntityId does not
            // resolve to a destroyed mold.
            var col = mold.Collider;
            if (col != null)
            {
                _byColliderId.Remove(col.GetEntityId());
            }
        }
    }
}
