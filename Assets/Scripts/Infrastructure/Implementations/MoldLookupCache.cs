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
                var col = view.GameObject.GetComponent<Collider>();
                if (col != null)
                    _byColliderId[col.GetEntityId()] = view;
            }
        }

        public IMoldView FindByCollider(Collider collider)
        {
            if (collider == null) return null;
            _byColliderId.TryGetValue(collider.GetEntityId(), out var view);
            return view;
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
    }
}
