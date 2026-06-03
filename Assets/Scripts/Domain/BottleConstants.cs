// =====================================================================
// BottleConstants — Single source of truth for all compile-time magic
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
    public static class BottleConstants
    {
        // ── Bottle Capacity ─────────────────────────────────────────────
        /// <summary>Hard upper bound for liquid layers a single bottle can hold.</summary>
        public const int MaxLayers = 8;
        /// <summary>Default starting capacity for a brand-new bottle.</summary>
        public const int DefaultLayerCapacity = 4;
        /// <summary>Minimum bottle count in any level.</summary>
        public const int MinBottlesPerLevel = 2;
        /// <summary>Maximum bottle count in any level.</summary>
        public const int MaxBottlesPerLevel = 16;
        /// <summary>Minimum unique-color count in any level.</summary>
        public const int MinColorsPerLevel = 1;
        /// <summary>Maximum unique-color count (mirrors LiquidColor enum size).</summary>
        public const int MaxColorsPerLevel = 12;
        /// <summary>Minimum empty bottle count (for solvability).</summary>
        public const int MinEmptyBottles = 1;

        // ── Numerical Tolerances ────────────────────────────────────────
        /// <summary>Per-channel tolerance for color matching (used by BottleValidationService, LiquidSortSolver).</summary>
        public const float ColorMatchEpsilon = 0.05f;
        /// <summary>Threshold below which a layer is considered empty.</summary>
        public const float LayerAmountEpsilon = 0.001f;
        /// <summary>Threshold below which a bottle's total fill is considered zero.</summary>
        public const float TotalFillEpsilon = 0.0001f;
        /// <summary>Alpha below which a DomainColor is considered transparent.</summary>
        public const float TransparencyAlphaEpsilon = 0.01f;
        /// <summary>Per-channel tolerance for DomainColor equality / hash.</summary>
        public const float DomainColorHashEpsilon = 0.001f;
        /// <summary>Tolerance for LiquidColor.FromDomainColor conversion.</summary>
        public const float LiquidColorMatchEpsilon = 0.1f;

        // ── Solver Limits ───────────────────────────────────────────────
        /// <summary>BFS visit budget for the LiquidSortSolver (overflow guard).</summary>
        public const int SolverMaxVisitedStates = 10000;
        /// <summary>Initial BFS depth (replaces the int.MaxValue sentinel).</summary>
        public const int SolverInitialDepth = 0;

        // ── Cork (Bottle Cap) Geometry ──────────────────────────────────
        public const float CorkRadius = 0.15f;
        public const float CorkHeight = 0.25f;
        public const int   CorkSegments = 16;
        public const float CorkYOffset = 0.05f;

        // ── Cork Material Color ─────────────────────────────────────────
        public const float CorkWoodR = 0.45f;
        public const float CorkWoodG = 0.28f;
        public const float CorkWoodB = 0.16f;

        // ── Animation Defaults (compile-time — config can override) ────
        public const float WobbleBaseImpulse = 2.0f;
        public const float WobbleTargetMultiplier = 0.8f;
        public const float HighlightActiveFresnel = 4.0f;
        public const float HighlightInactiveFresnel = 1.5f;
        public const float CompletionFlashIntensity = 4.0f;
        public const float CompletionFlashDuration = 0.6f;
        public const float SettleBounceDuration = 0.6f;

        // ── Bottle Visual Geometry ──────────────────────────────────────
        public const float DefaultBottleHeight = 2.4f;
        public const int   DefaultGlassMaterialIndex = 0;
        public const int   DefaultLiquidMaterialIndex = 1;

        // ── Glass Color (when bottle is empty) ──────────────────────────
        public const float GlassEmptyR = 1.0f;
        public const float GlassEmptyG = 1.0f;
        public const float GlassEmptyB = 1.0f;
        public const float GlassEmptyA = 0.18f;

        // ── Glass Tint (when bottle has liquid) ─────────────────────────
        /// <summary>Base luminance added to glass when tinted with liquid color.</summary>
        public const float GlassTintBase = 0.85f;
        /// <summary>How much of the liquid color tints the glass (0=pure white, 1=pure liquid).</summary>
        public const float GlassTintMultiplier = 0.15f;
        /// <summary>Alpha applied to the glass tint.</summary>
        public const float GlassTintAlpha = 0.25f;

        // ── Camera Background Default ───────────────────────────────────
        public const float CameraBackgroundR = 0.08f;
        public const float CameraBackgroundG = 0.05f;
        public const float CameraBackgroundB = 0.16f;
        public const float CameraBackgroundA = 1.0f;

        // ── Win Condition ───────────────────────────────────────────────
        public const float WinCheckDelaySeconds = 0.5f;

        // ── Visual Boost Defaults (overridable by config) ───────────────
        public const float DefaultSaturationBoost = 1.35f;
        public const float DefaultBrightnessBoost = 1.2f;

        // ── Pour Impulse (overridable by config) ────────────────────────
        public const float DefaultPourImpulseStrength = 2.0f;
    }
}
