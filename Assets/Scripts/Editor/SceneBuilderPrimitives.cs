using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using PuzzleGame.Application.Configuration;
using static PuzzleGame.Editor.SceneBuilderModel;

namespace PuzzleGame.Editor
{
    /// <summary>
    /// Scene primitive creation extracted from SceneBuilder. Builds lighting, ground,
    /// camera, post-processing, cauldron, and dust/fire particle effects. Pure side
    /// effects on the active scene — no orchestration.
    /// </summary>
    internal static class SceneBuilderPrimitives
    {
        // ── Lighting ─────────────────────────────────────────────────────────

        public static void SetupLighting()
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

        public static Light CreateDirectionalLight(string name, Color color, float intensity, Quaternion rotation)
        {
            var go = new GameObject(name);
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = color;
            light.intensity = intensity;
            light.transform.rotation = rotation;
            return light;
        }

        public static Light CreatePointLight(string name, Color color, float intensity, float range, Vector3 pos)
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

        public static void SetupGround()
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

        public static void CreateDustParticle(Vector3 position)
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

        public static void SetupCamera()
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

        public static void SetupPostProcessing()
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

        public static void CreateCauldron()
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

        public static void CreateFireParticles(Transform parent)
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

        // ── Material presets ────────────────────────────────────────────────

        public static void ApplyStandardGlassProperties(Material mat)
        {
            mat.SetColor("_Color", new Color(0.2f, 0.2f, 0.22f, 0.35f)); // Obsidian dark glass tint
            mat.SetFloat("_Smoothness", 0.85f);
            mat.SetFloat("_FresnelPower", 5f);
            mat.SetFloat("_FresnelIntensity", 1.5f);
            mat.SetColor("_FresnelColor", new Color(0.4f, 0.4f, 0.45f, 0.5f)); // Darker rim
            mat.SetColor("_SpecularColor", new Color(1f, 1f, 1f, 1f));
            mat.SetFloat("_SpecularIntensity", 1.5f);
        }

        public static void ApplyPremiumGlassProperties(Material mat)
        {
            mat.SetColor("_Color", new Color(0.2f, 0.2f, 0.22f, 0.25f)); // Dark obsidian transparent base
            mat.SetColor("_OutlineColor", new Color(0.4f, 0.4f, 0.45f, 1.0f)); // Steel outline instead of blue
            mat.SetColor("_InnerLineColor", new Color(0.35f, 0.35f, 0.38f, 1.0f)); // Subtle metallic inner lines
            mat.SetFloat("_Smoothness", 0.9f);
            mat.SetFloat("_Thickness", 0.05f);
            mat.SetFloat("_RefractionIntensity", 0.04f);
            mat.SetFloat("_IndexOfRefraction", 1.4f);
            mat.SetFloat("_FresnelPower", 4.5f);
            mat.SetFloat("_FresnelIntensity", 1.5f);
            mat.SetColor("_FresnelColor", new Color(0.4f, 0.4f, 0.45f, 0.6f));
            mat.SetColor("_ThicknessColor", new Color(0.3f, 0.3f, 0.35f, 0.4f)); // Steel rim thickness
            mat.SetFloat("_ThicknessPower", 2.0f);
            mat.SetColor("_SpecularColor", new Color(1f, 1f, 1f, 1f));
            mat.SetFloat("_SpecularIntensity", 1.5f);
            mat.SetFloat("_SpecularSecondary", 0.3f);
        }

        public static void ApplyStandardOreProperties(Material mat)
        {
            mat.SetFloat("_Transparency", 0.08f);
            mat.SetFloat("_EdgeDarken", 0.25f);
            mat.SetFloat("_EdgeWidth", 0.18f);
            mat.SetFloat("_SpecularIntensity", 0.7f);
            mat.SetFloat("_SpecularSmoothness", 0.6f);
            mat.SetFloat("_LayerBoundaryWidth", 0.025f);
            mat.SetFloat("_LayerBoundaryDarken", 0.4f);
        }

        public static void ApplyPremiumOreProperties(Material mat)
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

        public static Material CreateLitMaterial(Color color, float metallic, float smoothness)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Metallic", metallic);
            mat.SetFloat("_Smoothness", smoothness);
            return mat;
        }

        // ── Helpers ─────────────────────────────────────────────────────────

        public static GameObject CreatePrimitive(string name, PrimitiveType type)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            return go;
        }

        public static Shader FindShader(string name)
        {
            var guids = UnityEditor.AssetDatabase.FindAssets($"t:Shader {name}");
            if (guids.Length == 0)
            {
                Debug.LogError($"[SceneBuilderPrimitives] Shader '{name}' is missing! Material will fall back to default.");
                return null;
            }
            return UnityEditor.AssetDatabase.LoadAssetAtPath<Shader>(UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]));
        }
    }
}
