using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using PuzzleGame.Application.Configuration;
using PuzzleGame.Domain.Models;
using PuzzleGame.Domain.Services;
using PuzzleGame.Infrastructure;
using PuzzleGame.Infrastructure.Implementations;
using static PuzzleGame.Editor.SceneBuilderModel;

namespace PuzzleGame.Editor
{
    /// <summary>
    /// Mold-specific operations extracted from SceneBuilder. Builds the visual + logic
    /// stack for a single mold (collider, mesh, materials, MoldController), generates
    /// default mold sets, computes layout positions, and produces mixed color contents.
    /// </summary>
    internal static class SceneBuilderMoldFactory
    {
        // ── Mold set helpers ────────────────────────────────────────────────

        public static void CreateDefaultMoldSet()
        {
            int count = 20; // classic 4×5 grid
            var positions = ComputePositions(MoldLayout.Grid, count, Vector3.zero);
            Color[][] contents = GenerateMixedContents(count);
            for (int i = 0; i < count; i++)
                CreateMold(MoldConfig.WithColors(positions[i], contents[i], ShaderVariant.Premium, $"Mold_{i:D2}"));
        }

        public static GameObject CreateMold(MoldConfig cfg)
        {
            var renderer = new RendererService();
            var validator = new MoldValidationService();

            string uniqueName = GetUniqueName(cfg.namePrefix);
            var go = new GameObject(uniqueName) { transform = { position = cfg.position } };

            var col = go.AddComponent<CapsuleCollider>();
            col.radius = 0.4f;
            col.height = MoldHeight;
            col.center = new Vector3(0f, MoldHeight * 0.5f, 0f);

            go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>();

            // Shader seçimi
            string glassName = cfg.shader == ShaderVariant.Premium
                ? "PremiumMoldGlass" : "Custom/MoldGlass";
            string OreName = cfg.shader == ShaderVariant.Premium
                ? "PremiumLayeredOre" : "Custom/LayeredOre";

            var glassShader  = SceneBuilderPrimitives.FindShader(glassName) ?? Shader.Find("Universal Render Pipeline/Lit");
            var OreShader = SceneBuilderPrimitives.FindShader(OreName) ?? Shader.Find("Universal Render Pipeline/Unlit");

            var glassMat  = new Material(glassShader)  { name = $"{uniqueName}_Glass"  };
            var OreMat = new Material(OreShader) { name = $"{uniqueName}_Ore" };

            if (cfg.shader == ShaderVariant.Premium)
                SceneBuilderPrimitives.ApplyPremiumGlassProperties(glassMat);
            else
                SceneBuilderPrimitives.ApplyStandardGlassProperties(glassMat);

            if (cfg.shader == ShaderVariant.Premium)
                SceneBuilderPrimitives.ApplyPremiumOreProperties(OreMat);
            else
                SceneBuilderPrimitives.ApplyStandardOreProperties(OreMat);

            var meshGen = go.AddComponent<MoldMeshGenerator>();
            meshGen.height = MoldHeight;
            meshGen.bodyRadius = MoldRadius;
            meshGen.neckRadius = MoldRadius * 1.05f;
            meshGen.neckHeight = 0f;
            meshGen.capRadius = MoldRadius * 1.05f;
            meshGen.capHeight = 0f;
            meshGen.glassMaterial = glassMat;
            meshGen.OreMaterial = OreMat;
            meshGen.BuildMesh();

            mr.sharedMaterials = new[] { glassMat, OreMat };

            var ctrl = go.AddComponent<MoldController>();
            ctrl.glassMaterial = glassMat;
            ctrl.OreMaterial = OreMat;
            
            // Editor aracı olduğu için config'i bulup enjekte etmek zorundayız
            var visualConfig = Resources.Load<MoldVisualConfig>("Data/MoldVisualConfig");
            if (visualConfig != null) ctrl.visualConfig = visualConfig;

            var initial = (cfg.initialLayers != null)
                ? cfg.initialLayers
                : BuildLayers(cfg.colors ?? System.Array.Empty<Color>());

            ctrl.Initialize(renderer, validator, animationService: null, initial, visualConfigOverride: visualConfig);

            Undo.RegisterCreatedObjectUndo(go, $"Create {uniqueName}");
            return go;
        }

        public static void RemoveMolds()
        {
            var Molds = Object.FindObjectsByType<MoldController>(FindObjectsInactive.Include);
            foreach (var b in Molds)
            {
                Undo.DestroyObjectImmediate(b.gameObject);
            }
        }

        public static int CountMolds() =>
            Object.FindObjectsByType<MoldController>(FindObjectsInactive.Include).Length;

        // ── Position layouts ────────────────────────────────────────────────

        public static Vector3[] ComputePositions(MoldLayout layout, int count, Vector3 center)
        {
            if (count <= 0) return System.Array.Empty<Vector3>();
            var arr = new Vector3[count];

            switch (layout)
            {
                case MoldLayout.Line:
                    for (int i = 0; i < count; i++)
                        arr[i] = center + new Vector3((i - (count - 1) * 0.5f) * MoldSpacing, 0f, 0f);
                    break;

                case MoldLayout.Grid:
                    int cols = Mathf.CeilToInt(Mathf.Sqrt(count));
                    int rows = Mathf.CeilToInt(count / (float)cols);
                    for (int i = 0; i < count; i++)
                    {
                        int r = i / cols;
                        int c = i % cols;
                        arr[i] = center + new Vector3(
                            (c - (cols - 1) * 0.5f) * MoldSpacing,
                            0f,
                            (r - (rows - 1) * 0.5f) * MoldSpacing);
                    }
                    break;

                case MoldLayout.Circle:
                    float radius = count * MoldSpacing * 0.35f;
                    for (int i = 0; i < count; i++)
                    {
                        float angle = (i / (float)count) * Mathf.PI * 2f;
                        arr[i] = center + new Vector3(
                            Mathf.Cos(angle) * radius,
                            0f,
                            Mathf.Sin(angle) * radius);
                    }
                    break;

                case MoldLayout.Manual:
                    // Hepsi aynı pozisyon — kullanıcı sahneye ekledikten sonra taşır
                    for (int i = 0; i < count; i++)
                        arr[i] = center;
                    break;
            }
            return arr;
        }

        // ── Mold content generators ───────────────────────────────────────

        /// <summary>
        /// Default 20 şişe için karışık layer seti.
        /// Sıralı palet, her 5. şişe boş.
        /// </summary>
        public static Color[][] GenerateMixedContents(int MoldCount)
        {
            var result = new Color[MoldCount][];
            for (int i = 0; i < MoldCount; i++)
            {
                if ((i + 1) % 5 == 0) // her 5. boş
                {
                    result[i] = System.Array.Empty<Color>();
                }
                else
                {
                    var c = DefaultPalette[(i / 4) % DefaultPalette.Length];
                    int layers = 2 + (i % 3); // 2, 3, 4 layer
                    var arr = new Color[layers];
                    for (int k = 0; k < layers; k++) arr[k] = c;
                    result[i] = arr;
                }
            }
            return result;
        }

        public static List<OreLayer> BuildLayers(Color[] colors)
        {
            var layers = new List<OreLayer>();
            float[] heights = { 0.25f, 0.50f, 0.75f, 1.0f };
            for (int i = 0; i < colors.Length && i < 4; i++)
            {
                if (colors[i].a > 0.01f)
                {
                    float amount = i == 0 ? heights[0] : heights[i] - heights[i - 1];
                    layers.Add(new OreLayer(ColorAdapter.FromUnityStatic(colors[i]), amount));
                }
            }
            return layers;
        }

        // ── Helpers ─────────────────────────────────────────────────────────

        public static string GetUniqueName(string baseName)
        {
            int counter = 0;
            string candidate = baseName;
            while (GameObject.Find(candidate) != null)
            {
                counter++;
                candidate = $"{baseName}_{counter}";
            }
            return candidate;
        }
    }
}
