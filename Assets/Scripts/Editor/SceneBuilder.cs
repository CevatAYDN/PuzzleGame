using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using PuzzleGame.Domain.Models;
using PuzzleGame.Domain.Services;
using PuzzleGame.Domain.Interfaces;
using PuzzleGame.Infrastructure.Interfaces;
using PuzzleGame.Infrastructure.Implementations;
using PuzzleGame.Infrastructure;

namespace PuzzleGame.Editor
{
    /// <summary>
    /// Esnek sahne oluşturma. Hem "full preset" (çevre + şişeler) hem de
    /// tek tek şişe ("Quick Add") API sağlar.
    /// </summary>
    public static class SceneBuilder
    {
        // ── Build preset options ─────────────────────────────────────────────

        public struct BuildOptions
        {
            public bool lighting;
            public bool ground;
            public bool camera;
            public bool postProcessing;
            public bool cauldron;
            public bool bottles;
            public bool gameManager;
            public bool newScene;
        }

        public static readonly BuildOptions All = new BuildOptions
        {
            lighting = true, ground = true, camera = true,
            postProcessing = true, cauldron = true,
            bottles = true, gameManager = true, newScene = true
        };

        public static readonly BuildOptions Minimal = new BuildOptions
        {
            lighting = false, ground = false, camera = true,
            postProcessing = false, cauldron = false,
            bottles = true, gameManager = true, newScene = true
        };

        // ── Bottle config (Quick Add ve preset için) ────────────────────────

        public enum BottleLayout { Line, Grid, Circle, Manual }
        public enum ShaderVariant { Standard, Premium }

        public struct BottleConfig
        {
            public Vector3 position;
            public Color[] colors; // boş = boş şişe
            public ShaderVariant shader;
            public string namePrefix;

            public static BottleConfig Empty(Vector3 pos, ShaderVariant shader = ShaderVariant.Standard) =>
                new BottleConfig { position = pos, colors = System.Array.Empty<Color>(), shader = shader, namePrefix = "Bottle" };

            public static BottleConfig WithColors(Vector3 pos, Color[] colors,
                ShaderVariant shader = ShaderVariant.Standard, string prefix = "Bottle") =>
                new BottleConfig { position = pos, colors = colors, shader = shader, namePrefix = prefix };
        }

        private const float GroundScale   = 8f;
        private const float BottleHeight  = 2.4f;
        private const float BottleRadius  = 0.35f;
        private const float FogDensity    = 0.015f;
        private const float BottleSpacing = 1.3f;

        private static readonly Color AmbientColor   = new Color(0.12f, 0.10f, 0.20f);
        private static readonly Color FogColor       = new Color(0.08f, 0.05f, 0.15f);
        private static readonly Color GroundColor    = new Color(0.05f, 0.03f, 0.10f);
        private static readonly Color WallColor      = new Color(0.03f, 0.02f, 0.08f);
        private static readonly Color CamBackground  = new Color(0.08f, 0.05f, 0.15f, 1f);
        private static readonly Color MainLightColor = new Color(1.0f, 0.95f, 0.90f);
        private static readonly Color FillLightColor = new Color(0.4f, 0.5f, 0.9f);
        private static readonly Color RimLightColor  = new Color(0.8f, 0.6f, 0.3f);
        private static readonly Color CauldronColor  = new Color(0.15f, 0.1f, 0.1f);
        private static readonly Color CauldronGlow   = new Color(1f, 0.4f, 0.6f);
        private static readonly Color DustTint       = new Color(1f, 0.9f, 0.7f);

        private static readonly Vector3 FillLightPos = new Vector3(-8f, 5f, 5f);
        private static readonly Vector3 RimLightPos  = new Vector3(10f, 8f, -5f);
        private static readonly Vector3 CauldronPos  = new Vector3(0f, -1f, 9f);

        // Default palette — Quick Add dropdown'unda da kullanılır
        public static readonly Color[] DefaultPalette =
        {
            new Color(0.95f, 0.15f, 0.25f), // red
            new Color(0.10f, 0.65f, 1.00f), // blue
            new Color(0.15f, 0.85f, 0.35f), // green
            new Color(1.00f, 0.85f, 0.05f), // yellow
            new Color(0.70f, 0.25f, 0.95f), // purple
            new Color(0.98f, 0.45f, 0.10f), // orange
        };

        // ── Full preset build ────────────────────────────────────────────────

        public static void Build(BuildOptions opts)
        {
            if (opts.newScene)
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("PuzzleGame Scene Build");

            if (opts.lighting)   SetupLighting();
            if (opts.ground)     SetupGround();
            if (opts.camera)     SetupCamera();
            if (opts.postProcessing) SetupPostProcessing();
            if (opts.cauldron)   CreateCauldron();
            if (opts.gameManager) CreateGameManager();
            if (opts.bottles)    CreateDefaultBottleSet();

            Undo.CollapseUndoOperations(undoGroup);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("[SceneBuilder] Scene build complete. Ctrl+Z to undo.");
        }

        // ── Bottle set helpers ────────────────────────────────────────────────

        public static void CreateDefaultBottleSet()
        {
            int count = 20; // classic 4×5 grid
            var positions = ComputePositions(BottleLayout.Grid, count, Vector3.zero);
            Color[][] contents = GenerateMixedContents(count);
            for (int i = 0; i < count; i++)
                CreateBottle(BottleConfig.WithColors(positions[i], contents[i], ShaderVariant.Premium, $"Bottle_{i:D2}"));
        }

        public static GameObject CreateBottle(BottleConfig cfg)
        {
            var renderer = new RendererService();
            var validator = new BottleValidationService();

            string uniqueName = GetUniqueName(cfg.namePrefix);
            var go = new GameObject(uniqueName) { transform = { position = cfg.position } };

            var col = go.AddComponent<CapsuleCollider>();
            col.radius = 0.4f;
            col.height = BottleHeight;
            col.center = new Vector3(0f, BottleHeight * 0.5f, 0f);

            go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>();

            // Shader seçimi
            string glassName = cfg.shader == ShaderVariant.Premium
                ? "PremiumBottleGlass" : "Custom/BottleGlass";
            string liquidName = cfg.shader == ShaderVariant.Premium
                ? "PremiumLayeredLiquid" : "Custom/LayeredLiquid";

            var glassShader  = FindShader(glassName) ?? Shader.Find("Universal Render Pipeline/Lit");
            var liquidShader = FindShader(liquidName) ?? Shader.Find("Universal Render Pipeline/Unlit");

            var glassMat  = new Material(glassShader)  { name = $"{uniqueName}_Glass"  };
            var liquidMat = new Material(liquidShader) { name = $"{uniqueName}_Liquid" };

            if (cfg.shader == ShaderVariant.Premium)
                ApplyPremiumGlassProperties(glassMat);
            else
                ApplyStandardGlassProperties(glassMat);

            if (cfg.shader == ShaderVariant.Premium)
                ApplyPremiumLiquidProperties(liquidMat);
            else
                ApplyStandardLiquidProperties(liquidMat);

            var meshGen = go.AddComponent<BottleMeshGenerator>();
            meshGen.height = BottleHeight;
            meshGen.bodyRadius = BottleRadius;
            meshGen.neckRadius = 0.15f;
            meshGen.neckHeight = 0.4f;
            meshGen.capRadius = 0.17f;
            meshGen.capHeight = 0.1f;
            meshGen.glassMaterial = glassMat;
            meshGen.liquidMaterial = liquidMat;
            meshGen.BuildMesh();

            mr.sharedMaterials = new[] { glassMat, liquidMat };

            var ctrl = go.AddComponent<BottleController>();
            ctrl.glassMaterial = glassMat;
            ctrl.liquidMaterial = liquidMat;
            ctrl.Initialize(renderer, validator, BuildLayers(cfg.colors ?? System.Array.Empty<Color>()));

            Undo.RegisterCreatedObjectUndo(go, $"Create {uniqueName}");
            return go;
        }

        public static void RemoveBottles()
        {
            var bottles = Object.FindObjectsByType<BottleController>(FindObjectsInactive.Include);
            foreach (var b in bottles)
            {
                Undo.DestroyObjectImmediate(b.gameObject);
            }
        }

        public static int CountBottles() =>
            Object.FindObjectsByType<BottleController>(FindObjectsInactive.Include).Length;

        // ── Position layouts ────────────────────────────────────────────────

        public static Vector3[] ComputePositions(BottleLayout layout, int count, Vector3 center)
        {
            if (count <= 0) return System.Array.Empty<Vector3>();
            var arr = new Vector3[count];

            switch (layout)
            {
                case BottleLayout.Line:
                    for (int i = 0; i < count; i++)
                        arr[i] = center + new Vector3((i - (count - 1) * 0.5f) * BottleSpacing, 0f, 0f);
                    break;

                case BottleLayout.Grid:
                    int cols = Mathf.CeilToInt(Mathf.Sqrt(count));
                    int rows = Mathf.CeilToInt(count / (float)cols);
                    for (int i = 0; i < count; i++)
                    {
                        int r = i / cols;
                        int c = i % cols;
                        arr[i] = center + new Vector3(
                            (c - (cols - 1) * 0.5f) * BottleSpacing,
                            0f,
                            (r - (rows - 1) * 0.5f) * BottleSpacing);
                    }
                    break;

                case BottleLayout.Circle:
                    float radius = count * BottleSpacing * 0.35f;
                    for (int i = 0; i < count; i++)
                    {
                        float angle = (i / (float)count) * Mathf.PI * 2f;
                        arr[i] = center + new Vector3(
                            Mathf.Cos(angle) * radius,
                            0f,
                            Mathf.Sin(angle) * radius);
                    }
                    break;

                case BottleLayout.Manual:
                    // Hepsi aynı pozisyon — kullanıcı sahneye ekledikten sonra taşır
                    for (int i = 0; i < count; i++)
                        arr[i] = center;
                    break;
            }
            return arr;
        }

        // ── Bottle content generators ───────────────────────────────────────

        /// <summary>
        /// Default 20 şişe için karışık layer seti.
        /// Sıralı palet, her 5. şişe boş.
        /// </summary>
        public static Color[][] GenerateMixedContents(int bottleCount)
        {
            var result = new Color[bottleCount][];
            for (int i = 0; i < bottleCount; i++)
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

        // ── Lighting ─────────────────────────────────────────────────────────

        private static void SetupLighting()
        {
            RenderSettings.ambientMode  = AmbientMode.Flat;
            RenderSettings.ambientLight = AmbientColor;
            RenderSettings.fog          = true;
            RenderSettings.fogMode      = FogMode.Exponential;
            RenderSettings.fogColor     = FogColor;
            RenderSettings.fogDensity   = FogDensity;

            CreateDirectionalLight("MainLight", MainLightColor, 1.2f, Quaternion.Euler(50f, -30f, 0f));
            CreatePointLight("FillLight", FillLightColor, 0.3f, 30f, FillLightPos);
            CreatePointLight("RimLight", RimLightColor, 0.5f, 25f, RimLightPos);
        }

        private static Light CreateDirectionalLight(string name, Color color, float intensity, Quaternion rotation)
        {
            var go = new GameObject(name);
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = color;
            light.intensity = intensity;
            light.transform.rotation = rotation;
            return light;
        }

        private static Light CreatePointLight(string name, Color color, float intensity, float range, Vector3 pos)
        {
            var go = new GameObject(name);
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.transform.position = pos;
            return light;
        }

        // ── Ground ──────────────────────────────────────────────────────────

        private static void SetupGround()
        {
            var ground = CreatePrimitive("Ground", PrimitiveType.Plane);
            ground.transform.localScale = new Vector3(GroundScale, 1f, GroundScale);
            ground.transform.position = new Vector3(0f, -5f, 0f);
            ground.GetComponent<MeshRenderer>().sharedMaterial = CreateLitMaterial(GroundColor, 0.2f, 0.5f);

            var wall = CreatePrimitive("BackWall", PrimitiveType.Plane);
            wall.transform.localScale = new Vector3(GroundScale, 1f, GroundScale * 1.5f);
            wall.transform.position = new Vector3(0f, 0f, 5f);
            wall.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
            wall.GetComponent<MeshRenderer>().sharedMaterial = CreateLitMaterial(WallColor, 0.1f, 0.2f);

            for (int i = 0; i < 40; i++)
                CreateDustParticle(new Vector3(
                    Random.Range(-8f, 8f),
                    Random.Range(-4f, 8f),
                    Random.Range(-2f, 4f)));
        }

        private static void CreateDustParticle(Vector3 position)
        {
            var go = CreatePrimitive("DustParticle", PrimitiveType.Sphere);
            float alpha = Random.Range(0.08f, 0.25f);
            float scale = Random.Range(0.03f, 0.1f);
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.SetColor("_BaseColor", new Color(DustTint.r, DustTint.g, DustTint.b, alpha));
            mat.SetColor("_EmissionColor", DustTint * alpha * 3.0f);
            mat.EnableKeyword("_EMISSION");
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            go.transform.localScale = Vector3.one * scale;
            go.transform.position = position;
            Object.DestroyImmediate(go.GetComponent<Collider>());
        }

        // ── Camera ──────────────────────────────────────────────────────────

        private static void SetupCamera()
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = go.AddComponent<Camera>();
            cam.backgroundColor = CamBackground;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.fieldOfView = 55f;
            go.transform.position = new Vector3(0f, 3f, -14f);
            go.transform.LookAt(Vector3.zero);
        }

        // ── Post-Processing ─────────────────────────────────────────────────

        private static void SetupPostProcessing()
        {
            var go = new GameObject("PostProcessing");
            var volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 1f;

            var profile = ScriptableObject.CreateInstance<VolumeProfile>();

            var bloom = profile.Add<Bloom>(overrides: true);
            bloom.intensity.overrideState = true;
            bloom.intensity.value = 1.0f;
            bloom.threshold.overrideState = true;
            bloom.threshold.value = 0.7f;
            bloom.scatter.overrideState = true;
            bloom.scatter.value = 0.6f;

            var vignette = profile.Add<Vignette>(overrides: true);
            vignette.intensity.overrideState = true;
            vignette.intensity.value = 0.45f;
            vignette.smoothness.overrideState = true;
            vignette.smoothness.value = 0.7f;

            volume.profile = profile;
        }

        // ── Cauldron ────────────────────────────────────────────────────────

        private static void CreateCauldron()
        {
            var cauldron = new GameObject("Cauldron") { transform = { position = CauldronPos } };
            var cauldronMat = CreateLitMaterial(CauldronColor, 0.8f, 0.4f);

            var body = CreatePrimitive("CauldronBody", PrimitiveType.Sphere);
            body.transform.SetParent(cauldron.transform);
            body.transform.localPosition = Vector3.zero;
            body.transform.localScale = new Vector3(3.5f, 2.5f, 3.5f);
            body.GetComponent<MeshRenderer>().sharedMaterial = cauldronMat;

            var rim = CreatePrimitive("CauldronRim", PrimitiveType.Cylinder);
            rim.transform.SetParent(cauldron.transform);
            rim.transform.localPosition = new Vector3(0f, 1.1f, 0f);
            rim.transform.localScale = new Vector3(3.5f, 0.08f, 3.5f);
            rim.GetComponent<MeshRenderer>().sharedMaterial = cauldronMat;

            var inner = CreatePrimitive("CauldronInner", PrimitiveType.Cylinder);
            inner.transform.SetParent(cauldron.transform);
            inner.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            inner.transform.localScale = new Vector3(2.8f, 0.1f, 2.8f);
            var innerMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            innerMat.SetColor("_BaseColor", new Color(0.1f, 0f, 0.02f, 1f));
            innerMat.SetColor("_EmissionColor", new Color(2.5f, 0.4f, 0.8f) * 2.0f);
            innerMat.EnableKeyword("_EMISSION");
            inner.GetComponent<MeshRenderer>().sharedMaterial = innerMat;

            var glow = CreatePointLight("CauldronGlow", CauldronGlow, 3f, 8f, new Vector3(0f, 1.5f, 0f));
            glow.transform.SetParent(cauldron.transform);

            CreateFireParticles(cauldron.transform);
        }

        private static void CreateFireParticles(Transform parent)
        {
            var go = new GameObject("FireEffect");
            go.transform.SetParent(parent);
            go.transform.localPosition = new Vector3(0f, 1.2f, 0f);

            var ps = go.AddComponent<ParticleSystem>();
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial =
                new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));

            var main = ps.main;
            main.startLifetime = 1.5f;
            main.startSpeed = 1.5f;
            main.startSize = 0.3f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 50;

            var colorModule = ps.colorOverLifetime;
            colorModule.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(new Color(1f, 0.8f, 0.2f), 0f), new GradientColorKey(new Color(1f, 0.3f, 0.1f), 0.5f), new GradientColorKey(new Color(0.3f, 0.1f, 0.1f), 1f) },
                new[] { new GradientAlphaKey(0.8f, 0f), new GradientAlphaKey(0.5f, 0.5f), new GradientAlphaKey(0f, 1f) });
            colorModule.color = grad;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 15f;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 30f;
            shape.radius = 0.5f;
        }

        // ── GameManager ─────────────────────────────────────────────────────

        private static void CreateGameManager()
        {
            if (Object.FindAnyObjectByType<GameManager>() != null) return;
            new GameObject("GameManager").AddComponent<GameManager>();
        }

        // ── Material presets ────────────────────────────────────────────────

        private static void ApplyStandardGlassProperties(Material mat)
        {
            mat.SetColor("_Color", new Color(0.95f, 0.97f, 1.0f, 0.18f));
            mat.SetFloat("_Smoothness", 0.95f);
            mat.SetFloat("_FresnelPower", 5f);
            mat.SetFloat("_FresnelIntensity", 1.5f);
            mat.SetColor("_FresnelColor", new Color(1f, 1f, 1f, 0.5f));
            mat.SetColor("_SpecularColor", new Color(1f, 1f, 1f, 1f));
            mat.SetFloat("_SpecularIntensity", 2f);
        }

        private static void ApplyPremiumGlassProperties(Material mat)
        {
            mat.SetColor("_Color", new Color(0.95f, 0.97f, 1.0f, 0.12f));
            mat.SetFloat("_Smoothness", 0.98f);
            mat.SetFloat("_Thickness", 0.04f);
            mat.SetFloat("_RefractionIntensity", 0.06f);
            mat.SetFloat("_IndexOfRefraction", 1.45f);
            mat.SetFloat("_FresnelPower", 4.5f);
            mat.SetFloat("_FresnelIntensity", 2.0f);
            mat.SetColor("_FresnelColor", new Color(1f, 1f, 1f, 0.8f));
            mat.SetColor("_ThicknessColor", new Color(0.6f, 0.8f, 1.0f, 0.5f));
            mat.SetFloat("_ThicknessPower", 2.0f);
            mat.SetColor("_SpecularColor", new Color(1f, 1f, 1f, 1f));
            mat.SetFloat("_SpecularIntensity", 2.0f);
            mat.SetFloat("_SpecularSecondary", 0.5f);
        }

        private static void ApplyStandardLiquidProperties(Material mat)
        {
            mat.SetFloat("_Transparency", 0.08f);
            mat.SetFloat("_EdgeDarken", 0.25f);
            mat.SetFloat("_EdgeWidth", 0.18f);
            mat.SetFloat("_SpecularIntensity", 0.7f);
            mat.SetFloat("_SpecularSmoothness", 0.6f);
            mat.SetFloat("_LayerBoundaryWidth", 0.025f);
            mat.SetFloat("_LayerBoundaryDarken", 0.4f);
        }

        private static void ApplyPremiumLiquidProperties(Material mat)
        {
            mat.SetFloat("_Transparency", 0.12f);
            mat.SetFloat("_EdgeDarken", 0.35f);
            mat.SetFloat("_EdgeWidth", 0.22f);
            mat.SetFloat("_SpecularIntensity", 1.5f);
            mat.SetFloat("_SpecularSmoothness", 0.8f);
            mat.SetFloat("_LayerBoundaryWidth", 0.015f);
            mat.SetFloat("_LayerBoundaryDarken", 0.3f);
            mat.SetColor("_FoamColor", new Color(1.0f, 1.0f, 1.0f, 1.0f));
            mat.SetFloat("_FoamWidth", 0.015f);
            mat.SetFloat("_FoamIntensity", 1.2f);
            mat.SetColor("_RimColor", new Color(1.0f, 1.0f, 1.0f, 1.0f));
            mat.SetFloat("_RimPower", 3.0f);
            mat.SetFloat("_RimIntensity", 0.4f);
            mat.SetFloat("_SparkleIntensity", 0.5f);
            mat.SetFloat("_SparkleSize", 16f);
        }

        private static Material CreateLitMaterial(Color color, float metallic, float smoothness)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Metallic", metallic);
            mat.SetFloat("_Smoothness", smoothness);
            return mat;
        }

        private static List<LiquidLayer> BuildLayers(Color[] colors)
        {
            var layers = new List<LiquidLayer>();
            float[] heights = { 0.25f, 0.50f, 0.75f, 1.0f };
            for (int i = 0; i < colors.Length && i < 4; i++)
            {
                if (colors[i].a > 0.01f)
                {
                    float amount = i == 0 ? heights[0] : heights[i] - heights[i - 1];
                    layers.Add(new LiquidLayer(ColorAdapter.FromUnity(colors[i]), amount));
                }
            }
            return layers;
        }

        // ── Helpers ─────────────────────────────────────────────────────────

        private static string GetUniqueName(string baseName)
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

        private static GameObject CreatePrimitive(string name, PrimitiveType type)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            return go;
        }

        private static Shader FindShader(string name)
        {
            var guids = AssetDatabase.FindAssets($"t:Shader {name}");
            if (guids.Length == 0) return null;
            return AssetDatabase.LoadAssetAtPath<Shader>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }
    }
}
