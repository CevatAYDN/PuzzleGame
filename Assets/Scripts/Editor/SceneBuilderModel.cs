using System.Collections.Generic;
using UnityEngine;
using PuzzleGame.Domain.Models;

namespace PuzzleGame.Editor
{
    /// <summary>
    /// Data types, color/vector presets, and the public MoldConfig factory API
    /// extracted from SceneBuilder. Pure data — no Unity side effects on construction.
    /// </summary>
    public static class SceneBuilderModel
    {
        // ── Build preset options ─────────────────────────────────────────────

        public struct BuildOptions
        {
            public bool lighting;
            public bool ground;
            public bool camera;
            public bool postProcessing;
            public bool cauldron;
            public bool Molds;
            public bool gameManager;
            public bool newScene;
        }

        public static readonly BuildOptions All = new BuildOptions
        {
            lighting = true, ground = true, camera = true,
            postProcessing = true, cauldron = true,
            Molds = true, gameManager = true, newScene = true
        };

        public static readonly BuildOptions Minimal = new BuildOptions
        {
            lighting = false, ground = false, camera = true,
            postProcessing = false, cauldron = false,
            Molds = true, gameManager = true, newScene = true
        };

        // ── Mold config (Quick Add ve preset için) ────────────────────────

        public enum MoldLayout { Line, Grid, Circle, Manual }
        public enum ShaderVariant { Standard, Premium }

        public struct MoldConfig
        {
            public Vector3 position;
            public Color[] colors; // boş = boş şişe
            public List<OreLayer> initialLayers;
            public ShaderVariant shader;
            public string namePrefix;

            public static MoldConfig Empty(Vector3 pos, ShaderVariant shader = ShaderVariant.Standard) =>
                new MoldConfig { position = pos, colors = System.Array.Empty<Color>(), initialLayers = null, shader = shader, namePrefix = "Mold" };

            public static MoldConfig WithColors(Vector3 pos, Color[] colors,
                ShaderVariant shader = ShaderVariant.Standard, string prefix = "Mold") =>
                new MoldConfig { position = pos, colors = colors, initialLayers = null, shader = shader, namePrefix = prefix };

            public static MoldConfig WithLayers(Vector3 pos, List<OreLayer> layers,
                ShaderVariant shader = ShaderVariant.Standard, string prefix = "Mold") =>
                new MoldConfig { position = pos, colors = null, initialLayers = layers, shader = shader, namePrefix = prefix };
        }

        public const float GroundScale   = 8f;
        public const float MoldHeight  = 2.4f;
        public const float MoldRadius  = 0.35f;
        public const float FogDensity    = 0.015f;
        public const float MoldSpacing = 1.3f;

        public static readonly Color AmbientColor   = new Color(0.12f, 0.10f, 0.20f);
        public static readonly Color FogColor       = new Color(0.08f, 0.05f, 0.15f);
        public static readonly Color GroundColor    = new Color(0.05f, 0.03f, 0.10f);
        public static readonly Color WallColor      = new Color(0.03f, 0.02f, 0.08f);
        public static readonly Color CamBackground  = new Color(0.08f, 0.05f, 0.15f, 1f);
        public static readonly Color MainLightColor = new Color(1.0f, 0.95f, 0.90f);
        public static readonly Color FillLightColor = new Color(0.4f, 0.5f, 0.9f);
        public static readonly Color RimLightColor  = new Color(0.8f, 0.6f, 0.3f);
        public static readonly Color CauldronColor  = new Color(0.15f, 0.1f, 0.1f);
        public static readonly Color CauldronGlow   = new Color(1f, 0.4f, 0.6f);
        public static readonly Color DustTint       = new Color(1f, 0.9f, 0.7f);

        public static readonly Vector3 FillLightPos = new Vector3(-8f, 5f, 5f);
        public static readonly Vector3 RimLightPos  = new Vector3(10f, 8f, -5f);
        public static readonly Vector3 CauldronPos  = new Vector3(0f, -1f, 9f);

        // Default palette — Quick Add dropdown'unda da kullanılır
        public static readonly Color[] DefaultPalette =
        {
            new Color(0.9f,  0.2f,  0.2f),  // red
            new Color(0.2f,  0.6f,  0.9f),  // blue
            new Color(0.2f,  0.8f,  0.2f),  // green
            new Color(0.95f, 0.9f,  0.2f),  // yellow
            new Color(0.7f,  0.2f,  0.9f),  // purple
            new Color(0.9f,  0.5f,  0.2f),  // orange
        };
    }
}
