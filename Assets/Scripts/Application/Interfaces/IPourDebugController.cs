using System;
using System.Collections.Generic;
using PuzzleGame.Application.Configuration;
using PuzzleGame.Domain.Models;

namespace PuzzleGame.Application.Interfaces
{
    /// <summary>
    /// Developer-tool surface: direct mold state mutation, runtime config
    /// overrides, debug queries, and control flags. NOT consumed by
    /// gameplay code — used by the in-game debug overlay and editor tools
    /// (Pouring Lab, level designer utilities).
    /// </summary>
    public interface IPourDebugController
    {
        // ── Mold State Mutation ─────────────────────────────────────────────

        /// <summary>
        /// Replaces a mold's layers and fill level at runtime.
        /// Animations are skipped — this is a direct state mutation for tooling.
        /// </summary>
        void SetMoldLayers(int moldIndex, IReadOnlyList<OreLayer> layers);

        /// <summary>Sets a single layer's color at runtime.</summary>
        void SetMoldColor(int moldIndex, int layerIndex, DomainColor color);

        /// <summary>Adjusts the visual fill amount of a mold without changing layers.</summary>
        void SetMoldFillAmount(int moldIndex, float fillAmount);

        // ── Config Overrides ────────────────────────────────────────────────

        /// <summary>Applies temporary overrides to the active AnimationConfig.</summary>
        void OverrideAnimationConfig(Action<AnimationConfig> apply);

        /// <summary>Applies temporary overrides to the active MoldVisualConfig.</summary>
        void OverrideMoldVisualConfig(Action<MoldVisualConfig> apply);

        /// <summary>Reverts all runtime config overrides.</summary>
        void ClearAllOverrides();

        // ── Debug Queries ───────────────────────────────────────────────────

        /// <summary>Returns debug snapshots for all active molds.</summary>
        IReadOnlyList<MoldDebugState> GetAllMoldDebugStates();

        // ── Control Flags ───────────────────────────────────────────────────

        /// <summary>Enables/disables debug mode — extra logging + visual helpers.</summary>
        bool IsDebugModeEnabled { get; set; }

        /// <summary>When true, all pour animations are skipped (instant state change).</summary>
        bool IsAnimationDisabled { get; set; }
    }
}
