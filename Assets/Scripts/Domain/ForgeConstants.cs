// =====================================================================
// ForgeConstants — Single source of truth for all compile-time magic
// numbers used across the project.
//
// RULES
// 1. If a value appears in code more than once, it lives here.
// 2. If a value is purely a number/limit (no designer-tunable intent),
//    it lives here as `const`.
// 3. If a designer might want to tweak it from the inspector (per-level,
//    per-device, per-feature), it lives in a ScriptableObject under
//    Configuration/ — NOT here.
// 4. NO `using UnityEngine;` — Domain layer must remain pure C# so the
//    solver, replay, and CI validations can run headless.
// =====================================================================

namespace PuzzleGame.Domain
{
    public static class ForgeConstants
    {
        // ── Mold Capacity ─────────────────────────────────────────────
        /// <summary>Hard upper bound for ore layers a single mold can hold.
        /// Must match _Color1..N / _Fill1..N in Custom/LayeredLiquid shader
        /// (Assets/Shaders/LayeredOre.shader).</summary>
        public const int MaxLayers = 4;
        /// <summary>Default starting capacity for a brand-new mold.</summary>
        public const int DefaultLayerCapacity = 4;
        /// <summary>Minimum mold count in any level.</summary>
        public const int MinMoldsPerLevel = 2;
        /// <summary>Maximum mold count in any level.</summary>
        public const int MaxMoldsPerLevel = 16;
        /// <summary>Minimum unique-color count in any level.</summary>
        public const int MinColorsPerLevel = 1;
        /// <summary>Maximum unique-color count (mirrors OreColor enum size).</summary>
        public const int MaxColorsPerLevel = 12;
        /// <summary>Minimum empty mold count (for solvability).</summary>
        public const int MinEmptyMolds = 1;

        // ── Numerical Tolerances ────────────────────────────────────────
        /// <summary>Per-channel tolerance for color matching.</summary>
        public const float ColorMatchEpsilon = 0.05f;
        /// <summary>Threshold below which a layer is considered empty.</summary>
        public const float LayerAmountEpsilon = 0.001f;
        /// <summary>Threshold below which a mold's total fill is considered zero.</summary>
        public const float TotalFillEpsilon = 0.0001f;
        /// <summary>Alpha below which a DomainColor is considered transparent.</summary>
        public const float TransparencyAlphaEpsilon = 0.01f;
        /// <summary>Per-channel tolerance for DomainColor equality / hash.</summary>
        public const float DomainColorHashEpsilon = 0.001f;
        /// <summary>Tolerance for OreColor.FromDomainColor conversion.</summary>
        public const float OreColorMatchEpsilon = 0.1f;

        // ── Solver Limits ───────────────────────────────────────────────
        /// <summary>BFS visit budget for the OreSortSolver (overflow guard).</summary>
        public const int SolverMaxVisitedStates = 10000;
        /// <summary>Initial BFS depth (replaces the int.MaxValue sentinel).</summary>
        public const int SolverInitialDepth = 0;
    }
}
