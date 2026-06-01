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
using System.Collections.Generic;

namespace PuzzleGame.Editor
{
    /// <summary>
    /// Static helper — sahnede game object'ler (ışık, zemin, kamera, şişe, kazan) oluşturur.
    /// </summary>
    public static class SceneBuilder
    {
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

        private const float GroundScale  = 8f;
        private const float BottleHeight = 2.4f;
        private const float BottleRadius = 0.35f;
        private const float FogDensity   = 0.015f;

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

        public static void Build(BuildOptions opts)
        {
            if (opts.newScene)
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }

            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("PuzzleGame Scene Build");

            if (opts.lighting)   SetupLighting();
            if (opts.ground)     SetupGround();
            if (opts.camera)     SetupCamera();
            if (opts.postProcessing) SetupPostProcessing();
            if (opts.cauldron)   CreateCauldron();
            if (opts.bottles)    CreateBottlesInGridLayout();
            if (opts.gameManager) CreateGameManager();

            Undo.CollapseUndoOperations(undoGroup);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("[SceneBuilder] Scene build complete. Ctrl+Z to undo.");
        }

        // ── Lighting ────────────────────────────────────────────────────────

        private static void SetupLighting()
        {
            RenderSettings.ambientMode  = AmbientMode.Flat;
            RenderSettings.ambientLight = AmbientColor;
            RenderSettings.fog          = true;
            RenderSettings.fogMode     = FogMode.Exponential;
            RenderSettings.fogColor    = FogColor;
            RenderSettings.fogDensity  = FogDensity;

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

        // ── Bottles ─────────────────────────────────────────────────────────

        private static void CreateBottlesInGridLayout()
        {
            var renderer = new RendererService();
            var validator = new BottleValidationService();

            Color red    = new Color(0.95f, 0.15f, 0.25f);
            Color blue   = new Color(0.1f,  0.65f, 1.0f);
            Color green  = new Color(0.15f, 0.85f, 0.35f);
            Color yellow = new Color(1.0f,  0.85f, 0.05f);
            Color purple = new Color(0.7f,  0.25f, 0.95f);
            Color orange = new Color(0.98f, 0.45f, 0.1f);

            var bottles = new (Vector3 pos, Color[] colors)[]
            {
                (new Vector3(-2.4f, 4.5f, 0), new[] { yellow, purple, blue,   blue   }),
                (new Vector3(-1.2f, 4.5f, 0), new[] { purple, blue,   orange, green  }),
                (new Vector3( 0.0f, 4.5f, 0), new[] { red,    blue,   green,  yellow }),
                (new Vector3( 1.2f, 4.5f, 0), new[] { red,    green,  orange, blue   }),
                (new Vector3( 2.4f, 4.5f, 0), new[] { yellow, red,    purple, red    }),
                (new Vector3(-2.4f, 1.5f, 0), new[] { green,  yellow, red,    blue   }),
                (new Vector3(-1.2f, 1.5f, 0), new[] { orange, red,    green,  yellow }),
                (new Vector3( 0.0f, 1.5f, 0), new[] { red,    yellow, purple, orange }),
                (new Vector3( 1.2f, 1.5f, 0), new Color[0]                               ),
                (new Vector3( 2.4f, 1.5f, 0), new Color[0]                               ),
                (new Vector3(-2.4f,-1.5f, 0), new[] { red,    purple, yellow, green  }),
                (new Vector3(-1.2f,-1.5f, 0), new[] { orange, yellow, blue,   purple }),
                (new Vector3( 0.0f,-1.5f, 0), new[] { yellow, green,  red,    blue   }),
                (new Vector3( 1.2f,-1.5f, 0), new[] { green,  orange, red,    blue   }),
                (new Vector3( 2.4f,-1.5f, 0), new[] { red,    blue,   green,  yellow }),
                (new Vector3(-2.4f,-4.5f, 0), new[] { red,    blue,   green,  yellow }),
                (new Vector3(-1.2f,-4.5f, 0), new[] { yellow, orange, yellow, blue   }),
                (new Vector3( 0.0f,-4.5f, 0), new[] { yellow, red,    green,  orange }),
                (new Vector3( 1.2f,-4.5f, 0), new[] { purple, red,    blue,   green  }),
                (new Vector3( 2.4f,-4.5f, 0), new[] { blue,   green,  purple, orange }),
            };

            for (int i = 0; i < bottles.Length; i++)
                CreateSingleBottle(bottles[i].pos, $"Bottle_{i:D2}", bottles[i].colors, renderer, validator);
        }

        private static void CreateSingleBottle(Vector3 position, string name, Color[] colors,
            IRendererService renderer, IBottleValidator validator)
        {
            var go = new GameObject(name) { transform = { position = position } };
            var col = go.AddComponent<CapsuleCollider>();
            col.radius = 0.4f;
            col.height = BottleHeight;
            col.center = new Vector3(0f, BottleHeight * 0.5f, 0f);

            go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>();

            var glassShader  = FindShader("PremiumBottleGlass") ?? Shader.Find("Universal Render Pipeline/Lit");
            var liquidShader = FindShader("PremiumLayeredLiquid") ?? Shader.Find("Universal Render Pipeline/Unlit");

            var glassMat  = new Material(glassShader)  { name = $"{name}_Glass"  };
            var liquidMat = new Material(liquidShader) { name = $"{name}_Liquid" };

            glassMat.SetColor("_Color", new Color(0.95f, 0.97f, 1.0f, 0.12f));
            glassMat.SetFloat("_Smoothness", 0.98f);
            glassMat.SetFloat("_Thickness", 0.04f);
            glassMat.SetFloat("_RefractionIntensity", 0.06f);
            glassMat.SetFloat("_IndexOfRefraction", 1.45f);
            glassMat.SetFloat("_FresnelPower", 4.5f);
            glassMat.SetFloat("_FresnelIntensity", 2.0f);
            glassMat.SetColor("_FresnelColor", new Color(1f, 1f, 1f, 0.8f));
            glassMat.SetColor("_ThicknessColor", new Color(0.6f, 0.8f, 1.0f, 0.5f));
            glassMat.SetFloat("_ThicknessPower", 2.0f);

            liquidMat.SetFloat("_Transparency", 0.12f);
            liquidMat.SetFloat("_EdgeDarken", 0.35f);
            liquidMat.SetFloat("_EdgeWidth", 0.22f);
            liquidMat.SetFloat("_SpecularIntensity", 1.5f);
            liquidMat.SetFloat("_SpecularSmoothness", 0.8f);
            liquidMat.SetFloat("_LayerBoundaryWidth", 0.015f);
            liquidMat.SetFloat("_LayerBoundaryDarken", 0.3f);
            liquidMat.SetColor("_FoamColor", new Color(1.0f, 1.0f, 1.0f, 1.0f));
            liquidMat.SetFloat("_FoamWidth", 0.015f);
            liquidMat.SetFloat("_FoamIntensity", 1.2f);
            liquidMat.SetColor("_RimColor", new Color(1.0f, 1.0f, 1.0f, 1.0f));
            liquidMat.SetFloat("_RimPower", 3.0f);
            liquidMat.SetFloat("_RimIntensity", 0.4f);
            liquidMat.SetFloat("_SparkleIntensity", 0.5f);
            liquidMat.SetFloat("_SparkleSize", 16f);

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
            ctrl.Initialize(renderer, validator, BuildLayers(colors));
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

        // ── GameManager ─────────────────────────────────────────────────────

        private static void CreateGameManager()
        {
            new GameObject("GameManager").AddComponent<GameManager>();
        }

        // ── Helpers ─────────────────────────────────────────────────────────

        private static Material CreateLitMaterial(Color color, float metallic, float smoothness)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Metallic", metallic);
            mat.SetFloat("_Smoothness", smoothness);
            return mat;
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
