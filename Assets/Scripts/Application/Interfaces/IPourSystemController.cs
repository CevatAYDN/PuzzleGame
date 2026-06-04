using System;
using System.Collections.Generic;
using PuzzleGame.Domain.Models;
using PuzzleGame.Application.Configuration;

namespace PuzzleGame.Application.Interfaces
{
    /// <summary>
    /// Developer-facing control API for the pour system.
    /// Provides runtime mold manipulation, pour simulation, config overrides,
    /// state snapshots, and debug mode flags.
    ///
    /// All methods are safe to call from editor tools (Pouring Lab) and
    /// the in-game debug overlay. Implementation lives in Infrastructure layer.
    /// </summary>
    public interface IPourSystemController
    {
        // ── Mold State Manipulation ──────────────────────────────────────────

        /// <summary>
        /// Replaces a mold's layers and fill level at runtime.
        /// Animations are skipped — this is a direct state mutation for tooling.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">If moldIndex is invalid.</exception>
        void SetMoldLayers(int moldIndex, IReadOnlyList<OreLayer> layers);

        /// <summary>
        /// Sets a single layer's color at runtime.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">If indices are invalid.</exception>
        void SetMoldColor(int moldIndex, int layerIndex, DomainColor color);

        /// <summary>
        /// Adjusts the visual fill amount of a mold without changing layers.
        /// Primarily for testing visual edge cases.
        /// </summary>
        void SetMoldFillAmount(int moldIndex, float fillAmount);

        // ── Pour Simulation ──────────────────────────────────────────────────

        /// <summary>
        /// Computes what would happen if a pour were executed between two molds.
        /// Does NOT mutate state. Safe to call from editor preview.
        /// </summary>
        PourPreviewResult PreviewPour(int sourceIndex, int targetIndex);

        /// <summary>
        /// Executes a pour instantly — no tween, no VFX, no audio.
        /// State is mutated directly. Fires standard events (CastCompletedEvent / CastRejectedEvent).
        /// Returns true if the pour was valid and executed.
        /// </summary>
        bool ExecuteInstantPour(int sourceIndex, int targetIndex);

        // ── Config Overrides ─────────────────────────────────────────────────

        /// <summary>
        /// Applies temporary overrides to the active AnimationConfig.
        /// Overrides are discarded when ClearAllOverrides() is called or scene reloads.
        /// </summary>
        void OverrideAnimationConfig(Action<AnimationConfig> apply);

        /// <summary>
        /// Applies temporary overrides to the active MoldVisualConfig.
        /// Overrides are discarded when ClearAllOverrides() is called or scene reloads.
        /// </summary>
        void OverrideMoldVisualConfig(Action<MoldVisualConfig> apply);

        /// <summary>Reverts all runtime config overrides.</summary>
        void ClearAllOverrides();

        // ── State History ────────────────────────────────────────────────────

        /// <summary>Takes a deep snapshot of all active molds. Stored on a stack (max 32).</summary>
        void SnapshotAllMolds();

        /// <summary>Restores the most recent snapshot. No-op if stack is empty.</summary>
        void RestoreSnapshot();

        // ── Debug Queries ────────────────────────────────────────────────────

        /// <summary>Returns debug snapshots for all active molds.</summary>
        IReadOnlyList<MoldDebugState> GetAllMoldDebugStates();

        // ── Control Flags ────────────────────────────────────────────────────

        /// <summary>Enables/disables debug mode — toggles extra logging and visual helpers.</summary>
        bool IsDebugModeEnabled { get; set; }

        /// <summary>When true, all pour animations are skipped (instant state change).</summary>
        bool IsAnimationDisabled { get; set; }
    }
}
