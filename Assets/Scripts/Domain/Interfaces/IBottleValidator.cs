using BottleShaders.Domain.Models;
using UnityEngine;

namespace BottleShaders.Domain.Interfaces
{
    /// <summary>
    /// Single source of truth for all bottle-related game rules.
    /// </summary>
    public interface IBottleValidator
    {
        /// <summary>Returns true when the top layer of <paramref name="source"/> can be poured into <paramref name="target"/>.</summary>
        bool CanPour(BottleState source, BottleState target);

        /// <summary>Returns true when the bottle is either empty or completely filled with a single color.</summary>
        bool IsComplete(BottleState bottle);

        /// <summary>Returns true when two colors are considered the same within the configured tolerance.</summary>
        bool ColorsMatch(Color a, Color b);
    }
}
