using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using BottleShaders.Domain.Models;
using BottleShaders.Domain.Services;
using BottleShaders.Domain.Interfaces;
using BottleShaders.Infrastructure.Interfaces;
using BottleShaders.Infrastructure.Implementations;
using BottleShaders.Logging;

namespace BottleShaders.Editor
{
    public static class CreateBottleTestScene
    {
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
        private static readonly Vector3 FillLightPos  = new Vector3(-8f, 5f, 5f);

        private static readonly Color RimLightColor = new Color(0.8f, 0.6f, 0.3f);
        private static readonly Vector3 RimLightPos  = new Vector3(10f, 8f, -5f);

        private static readonly Color CauldronColor = new Color(0.15f, 0.1f, 0.1f);
        private static readonly Color CauldronGlow  = new Color(1f, 0.4f, 0.6f);
        private static readonly Vector3 CauldronPos  = new Vector3(0f, -1f, 9f);

        private static readonly Color DustTint = new Color(1f, 0.9f, 0.7f);

        [MenuItem("Tools/Bottle Shader/Create Test Scene")]
        public static void CreateScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            IRendererService renderer = new RendererService();
            IBottleValidator validator = new BottleValidationService();

            SetupLighting();
            SetupGround();
            SetupCamera();
            SetupPostProcessing();
            CreateCauldron();
            CreateBottlesInGridLayout(renderer, validator);
            CreateGameManager();

            EditorSceneManager.MarkSceneDirty(scene);
            BottleLogger.LogInfo("Enhanced test scene created.");
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

            Light mainLight = CreateDirectionalLight(
                "MainLight", MainLightColor, 1.2f, Quaternion.Euler(50f, -30f, 0f));

            CreatePointLight("FillLight", FillLightColor, 0.3f, 30f, FillLightPos);
            CreatePointLight("RimLight", RimLightColor, 0.5f, 25f, RimLightPos);
        }

        private static Light CreateDirectionalLight(string name, Color color,
            float intensity, Quaternion rotation)
        {
            GameObject go = new GameObject(name);
            Light light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = color;
            light.intensity = intensity;
            light.transform.rotation = rotation;
            return light;
        }

        private static Light CreatePointLight(string name, Color color,
            float intensity, float range, Vector3 position)
        {
            GameObject go = new GameObject(name);
            Light light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.transform.position = position;
            return light;
        }

        // ── Ground ──────────────────────────────────────────────────────────

        private static void SetupGround()
        {
            GameObject ground = CreatePrimitive("Ground", PrimitiveType.Plane);
            ground.transform.localScale = new Vector3(GroundScale, 1f, GroundScale);
            ground.transform.position = new Vector3(0f, -5f, 0f); // Kazan ve şişelerin altına indir

            Material groundMat = CreateLitMaterial(GroundColor, 0.2f, 0.5f);
            ground.GetComponent<MeshRenderer>().sharedMaterial = groundMat;

            // Arka duvar yerine koyu gökyüzü (Solid Color Camera ile)
            
            for (int i = 0; i < 20; i++)
            {
                Vector3 pos = new Vector3(
                    Random.Range(-8f, 8f),
                    Random.Range(-4f, 8f),
                    Random.Range(-2f, 8f)
                );
                CreateDustParticle(pos);
            }
        }

        private static void CreateDustParticle(Vector3 position)
        {
            GameObject go = CreatePrimitive("DustParticle", PrimitiveType.Sphere);
            float alpha = Random.Range(0.02f, 0.08f);
            float scale = Random.Range(0.02f, 0.08f);

            Material mat = CreateUnlitMaterial(new Color(DustTint.r, DustTint.g, DustTint.b, alpha));
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            go.transform.localScale = Vector3.one * scale;
            go.transform.position = position;
            Object.DestroyImmediate(go.GetComponent<Collider>());
        }

        // ── Camera ──────────────────────────────────────────────────────────

        private static void SetupCamera()
        {
            GameObject go = new GameObject("Main Camera");
            go.tag = "MainCamera";

            Camera cam = go.AddComponent<Camera>();
            cam.backgroundColor = CamBackground;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.fieldOfView = 55f; // Geniş açılı perspektif

            // Hafif yukarıdan aşağıya doğru bakan açı
            go.transform.position = new Vector3(0f, 3f, -14f);
            go.transform.LookAt(new Vector3(0f, 0f, 0f));
        }

        // ── Post-Processing ─────────────────────────────────────────────────

        private static void SetupPostProcessing()
        {
            GameObject go = new GameObject("PostProcessing");
            Volume volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 1f;

            VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();

            Bloom bloom = profile.Add<Bloom>(overrides: true);
            bloom.intensity.overrideState = true;
            bloom.intensity.value = 0.3f;
            bloom.threshold.overrideState = true;
            bloom.threshold.value = 0.8f;

            Vignette vignette = profile.Add<Vignette>(overrides: true);
            vignette.intensity.overrideState = true;
            vignette.intensity.value = 0.35f;
            vignette.smoothness.overrideState = true;
            vignette.smoothness.value = 0.6f;

            volume.profile = profile;
        }

        // ── Cauldron ───────────────────────────────────────────────────────-

        private static void CreateCauldron()
        {
            GameObject cauldron = new GameObject("Cauldron");
            cauldron.transform.position = CauldronPos;

            Material cauldronMat = CreateLitMaterial(CauldronColor, 0.8f, 0.4f);

            // Body
            GameObject body = CreatePrimitive("CauldronBody", PrimitiveType.Sphere);
            body.transform.SetParent(cauldron.transform);
            body.transform.localPosition = Vector3.zero;
            body.transform.localScale = new Vector3(3.5f, 2.5f, 3.5f);
            body.GetComponent<MeshRenderer>().sharedMaterial = cauldronMat;

            // Rim: use a cylinder flattened as a ring
            GameObject rim = CreatePrimitive("CauldronRim", PrimitiveType.Cylinder);
            rim.transform.SetParent(cauldron.transform);
            rim.transform.localPosition = new Vector3(0f, 1.1f, 0f);
            rim.transform.localScale = new Vector3(3.5f, 0.08f, 3.5f);
            rim.GetComponent<MeshRenderer>().sharedMaterial = cauldronMat;

            // Inner glow
            GameObject innerMatObj = CreatePrimitive("CauldronInner", PrimitiveType.Cylinder);
            innerMatObj.transform.SetParent(cauldron.transform);
            innerMatObj.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            innerMatObj.transform.localScale = new Vector3(2.8f, 0.1f, 2.8f);

            Material innerMat = CreateUnlitMaterial(new Color(1f, 0.4f, 0.6f, 0.6f));
            innerMatObj.GetComponent<MeshRenderer>().sharedMaterial = innerMat;

            // Glow light
            Light glow = CreatePointLight("CauldronGlow", CauldronGlow, 3f, 8f,
                new Vector3(0f, 1.5f, 0f));
            glow.transform.SetParent(cauldron.transform);

            CreateFireParticles(cauldron.transform);
        }

        // ── Fire Particles ─────────────────────────────────────────────────

        private static void CreateFireParticles(Transform parent)
        {
            GameObject go = new GameObject("FireEffect");
            go.transform.SetParent(parent);
            go.transform.localPosition = new Vector3(0f, 1.2f, 0f);

            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            
            // Render component and material fix (Magenta issue)
            ParticleSystemRenderer psRenderer = go.GetComponent<ParticleSystemRenderer>();
            psRenderer.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));

            ParticleSystem.MainModule main = ps.main;
            main.startLifetime = 1.5f;
            main.startSpeed = 1.5f;
            main.startSize = 0.3f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 50;

            ParticleSystem.ColorOverLifetimeModule colorModule = ps.colorOverLifetime;
            colorModule.enabled = true;

            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[] {
                    new GradientColorKey(new Color(1f, 0.8f, 0.2f), 0f),
                    new GradientColorKey(new Color(1f, 0.3f, 0.1f), 0.5f),
                    new GradientColorKey(new Color(0.3f, 0.1f, 0.1f), 1f)
                },
                new[] {
                    new GradientAlphaKey(0.8f, 0f),
                    new GradientAlphaKey(0.5f, 0.5f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            colorModule.color = gradient;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 15f;

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 30f;
            shape.radius = 0.5f;
        }

        // ── Bottles ─────────────────────────────────────────────────────────

        private static void CreateBottlesInGridLayout(
            IRendererService renderer, IBottleValidator validator)
        {
            Color red    = new Color(0.9f, 0.3f, 0.4f); // Yumuşak Kırmızı
            Color blue   = new Color(0.3f, 0.7f, 1.0f); // Yumuşak Mavi
            Color green  = new Color(0.4f, 0.8f, 0.5f); // Yumuşak Yeşil
            Color yellow = new Color(0.95f, 0.85f, 0.2f); // Pastel Sarı
            Color purple = new Color(0.7f, 0.4f, 0.9f); // Mor
            Color orange = new Color(0.9f, 0.6f, 0.2f); // Turuncu

            (Vector3 position, Color[] colors)[] bottles =
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

                (new Vector3(-2.4f, -1.5f, 0), new[] { red,    purple, yellow, green  }),
                (new Vector3(-1.2f, -1.5f, 0), new[] { orange, yellow, blue,   purple }),
                (new Vector3( 0.0f, -1.5f, 0), new[] { yellow, green,  red,    blue   }),
                (new Vector3( 1.2f, -1.5f, 0), new[] { green,  orange, red,    blue   }),
                (new Vector3( 2.4f, -1.5f, 0), new[] { red,    blue,   green,  yellow }),

                (new Vector3(-2.4f, -4.5f, 0), new[] { red,    blue,   green,  yellow }),
                (new Vector3(-1.2f, -4.5f, 0), new[] { yellow, orange, yellow, blue   }),
                (new Vector3( 0.0f, -4.5f, 0), new[] { yellow, red,    green,  orange }),
                (new Vector3( 1.2f, -4.5f, 0), new[] { purple, red,    blue,   green  }),
                (new Vector3( 2.4f, -4.5f, 0), new[] { blue,   green,  purple, orange }),
            };

            for (int i = 0; i < bottles.Length; i++)
            {
                var (pos, colors) = bottles[i];
                CreateBottle(pos, $"Bottle_{i:D2}", colors, renderer, validator);
            }
        }

        private static void CreateBottle(Vector3 position, string name,
            Color[] colors, IRendererService renderer, IBottleValidator validator)
        {
            GameObject go = new GameObject(name);
            go.transform.position = position;

            CapsuleCollider col = go.AddComponent<CapsuleCollider>();
            col.radius = 0.4f;
            col.height = BottleHeight;
            col.center = new Vector3(0f, BottleHeight * 0.5f, 0f);

            go.AddComponent<MeshFilter>();
            MeshRenderer mr = go.AddComponent<MeshRenderer>();

            Shader glassShader  = FindShader("PremiumBottleGlass")  ?? Shader.Find("Universal Render Pipeline/Lit");
            Shader liquidShader = FindShader("PremiumLayeredLiquid") ?? Shader.Find("Universal Render Pipeline/Unlit");

            Material glassMat  = new Material(glassShader)  { name = $"{name}_Glass"  };
            Material liquidMat = new Material(liquidShader) { name = $"{name}_Liquid" };

            glassMat.SetColor("_Color", new Color(1f, 1f, 1f, 0.08f));
            glassMat.SetFloat("_Smoothness", 0.95f);

            BottleMeshGenerator meshGen = go.AddComponent<BottleMeshGenerator>();
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

            BottleController ctrl = go.AddComponent<BottleController>();
            ctrl.glassMaterial = glassMat;
            ctrl.liquidMaterial = liquidMat;

            List<LiquidLayer> layers = BuildLayers(colors);
            ctrl.Initialize(renderer, validator, layers);
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
                    layers.Add(new LiquidLayer(colors[i], amount));
                }
            }

            return layers;
        }

        // ── GameManager ─────────────────────────────────────────────────────

        private static void CreateGameManager()
        {
            new GameObject("GameManager").AddComponent<GameManager>();
        }

        // ── Material helpers ────────────────────────────────────────────────

        private static Material CreateLitMaterial(Color color, float metallic, float smoothness)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Metallic", metallic);
            mat.SetFloat("_Smoothness", smoothness);
            return mat;
        }

        private static Material CreateUnlitMaterial(Color color)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            mat.SetColor("_BaseColor", color);
            return mat;
        }

        // ── Primitive helper ─────────────────────────────────────────────────

        private static GameObject CreatePrimitive(string name, PrimitiveType type)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            return go;
        }

        // ── Shader lookup ───────────────────────────────────────────────────

        private static Shader FindShader(string name)
        {
            string[] guids = AssetDatabase.FindAssets($"t:Shader {name}");
            if (guids.Length == 0) return null;

            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<Shader>(path);
        }
    }
}