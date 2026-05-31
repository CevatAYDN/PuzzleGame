using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using BottleShaders.Domain.Models;
using BottleShaders.Domain.Services;
using BottleShaders.Domain.Interfaces;
using BottleShaders.Infrastructure.Interfaces;
using BottleShaders.Infrastructure.Implementations;

namespace BottleShaders.Editor
{
    /// <summary>
    /// Editor utility that builds a ready-to-play test scene from scratch.
    /// Run via  Tools → Bottle Shader → Create Test Scene.
    /// </summary>
    public static class CreateBottleTestScene
    {
        [MenuItem("Tools/Bottle Shader/Create Test Scene")]
        public static void CreateScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // ── Infrastructure ───────────────────────────────────────────────
            IRendererService rendererService = new RendererService();
            IBottleValidator validator       = new BottleValidationService();

            // ── GameManager ──────────────────────────────────────────────────
            new GameObject("GameManager").AddComponent<GameManager>();

            // ── Camera ───────────────────────────────────────────────────────
            var camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            var cam = camObj.AddComponent<Camera>();
            cam.backgroundColor = new Color(0.12f, 0.05f, 0.35f);
            cam.clearFlags      = CameraClearFlags.SolidColor;
            cam.fieldOfView     = 60f;
            camObj.transform.position = new Vector3(0f, 0f, -14f);
            camObj.transform.LookAt(new Vector3(0f, 1f, 0f));

            // ── Directional Light ────────────────────────────────────────────
            var lightObj = new GameObject("Directional Light");
            var light    = lightObj.AddComponent<Light>();
            light.type      = LightType.Directional;
            light.color     = Color.white;
            light.intensity = 1f;
            lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            RenderSettings.ambientMode  = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.4f, 0.4f, 0.6f);

            // ── Cauldron ─────────────────────────────────────────────────────
            CreateCauldron();

            // ── Bottles ──────────────────────────────────────────────────────
            Color cRed    = new Color(0.9f, 0.2f, 0.2f);
            Color cBlue   = new Color(0.2f, 0.6f, 1.0f);
            Color cGreen  = new Color(0.5f, 0.9f, 0.1f);
            Color cYellow = new Color(0.9f, 0.9f, 0.1f);

            // Row 1
            float y1 = 4.5f;
            CreateBottle(new Vector3(-2.4f, y1, 0), "Bottle1_1", new[] { cYellow, cRed,   cBlue,  cBlue  }, rendererService, validator);
            CreateBottle(new Vector3(-1.2f, y1, 0), "Bottle1_2", new[] { cRed,   cBlue,  cYellow, cGreen }, rendererService, validator);
            CreateBottle(new Vector3( 0.0f, y1, 0), "Bottle1_3", new[] { cRed,   cBlue,  cGreen,  cYellow}, rendererService, validator);
            CreateBottle(new Vector3( 1.2f, y1, 0), "Bottle1_4", new[] { cRed,   cBlue,  cGreen,  cBlue  }, rendererService, validator);
            CreateBottle(new Vector3( 2.4f, y1, 0), "Bottle1_5", new[] { cYellow, cRed,  cBlue,   cRed   }, rendererService, validator);

            // Row 2
            float y2 = 1.5f;
            CreateBottle(new Vector3(-2.4f, y2, 0), "Bottle2_1", new[] { cGreen, cYellow, cRed,   cBlue  }, rendererService, validator);
            CreateBottle(new Vector3(-1.2f, y2, 0), "Bottle2_2", new[] { cGreen, cRed,   cGreen,  cYellow}, rendererService, validator);
            CreateBottle(new Vector3( 0.0f, y2, 0), "Bottle2_3", new[] { cRed,   cYellow, cYellow, cBlue  }, rendererService, validator);
            CreateBottle(new Vector3( 1.2f, y2, 0), "Bottle2_4", new Color[0],                              rendererService, validator); // empty
            CreateBottle(new Vector3( 2.4f, y2, 0), "Bottle2_5", new Color[0],                              rendererService, validator); // empty

            // Row 3
            float y3 = -1.5f;
            CreateBottle(new Vector3(-2.4f, y3, 0), "Bottle3_1", new[] { cRed,   cBlue,  cYellow, cGreen }, rendererService, validator);
            CreateBottle(new Vector3(-1.2f, y3, 0), "Bottle3_2", new[] { cRed,   cYellow, cBlue,  cYellow}, rendererService, validator);
            CreateBottle(new Vector3( 0.0f, y3, 0), "Bottle3_3", new[] { cYellow, cGreen, cRed,   cBlue  }, rendererService, validator);
            CreateBottle(new Vector3( 1.2f, y3, 0), "Bottle3_4", new[] { cGreen, cYellow, cRed,   cBlue  }, rendererService, validator);
            CreateBottle(new Vector3( 2.4f, y3, 0), "Bottle3_5", new[] { cRed,   cBlue,  cGreen,  cYellow}, rendererService, validator);

            // Row 4
            float y4 = -4.5f;
            CreateBottle(new Vector3(-2.4f, y4, 0), "Bottle4_1", new[] { cRed,   cBlue,  cGreen,  cYellow}, rendererService, validator);
            CreateBottle(new Vector3(-1.2f, y4, 0), "Bottle4_2", new[] { cYellow, cRed,  cYellow, cBlue  }, rendererService, validator);
            CreateBottle(new Vector3( 0.0f, y4, 0), "Bottle4_3", new[] { cYellow, cRed,  cGreen,  cBlue  }, rendererService, validator);
            CreateBottle(new Vector3( 1.2f, y4, 0), "Bottle4_4", new[] { cYellow, cRed,  cBlue,   cGreen }, rendererService, validator);
            CreateBottle(new Vector3( 2.4f, y4, 0), "Bottle4_5", new[] { cBlue,  cGreen, cYellow, cRed   }, rendererService, validator);

            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("[Bottle Shader] Test scene created.");
        }

        // ── Private helpers ──────────────────────────────────────────────────

        private static void CreateCauldron()
        {
            var cauldron = new GameObject("Cauldron");
            cauldron.transform.position = new Vector3(0f, -7.5f, 0f);

            var cauldronMat = new Material(Shader.Find("Universal Render Pipeline/Lit"))
            {
                color = new Color(0.8f, 0.2f, 0.8f)
            };

            // Body
            var body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            body.transform.SetParent(cauldron.transform);
            body.transform.localPosition = Vector3.zero;
            body.transform.localScale    = new Vector3(3f, 2f, 3f);
            body.GetComponent<MeshRenderer>().sharedMaterial = cauldronMat;

            // Rim
            var rim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rim.transform.SetParent(cauldron.transform);
            rim.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            rim.transform.localScale    = new Vector3(2.8f, 0.1f, 2.8f);
            rim.GetComponent<MeshRenderer>().sharedMaterial = cauldronMat;

            // Liquid surface
            var liquidMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            liquidMat.SetColor("_BaseColor", new Color(1f, 0.5f, 1f, 0.8f));

            var liquid = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            liquid.transform.SetParent(cauldron.transform);
            liquid.transform.localPosition = new Vector3(0f, 0.8f, 0f);
            liquid.transform.localScale    = new Vector3(2.6f, 0.05f, 2.6f);
            liquid.GetComponent<MeshRenderer>().sharedMaterial = liquidMat;

            // Glow light
            var glowObj = new GameObject("CauldronLight");
            glowObj.transform.SetParent(cauldron.transform);
            glowObj.transform.localPosition = new Vector3(0f, 2f, 0f);
            var l = glowObj.AddComponent<Light>();
            l.type      = LightType.Point;
            l.color     = new Color(1f, 0.5f, 1f);
            l.range     = 5f;
            l.intensity = 2f;
        }

        private static Shader FindCustomShader(string name)
        {
            string[] guids = AssetDatabase.FindAssets("t:Shader " + name);
            if (guids.Length == 0) return null;
            return AssetDatabase.LoadAssetAtPath<Shader>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        private static void CreateBottle(Vector3 position, string bottleName,
                                         Color[] colors,
                                         IRendererService rendererService,
                                         IBottleValidator validator)
        {
            var obj = new GameObject(bottleName);
            obj.transform.position = position;

            // Collider
            var col    = obj.AddComponent<CapsuleCollider>();
            col.radius = 0.4f;
            col.height = 2.4f;
            col.center = new Vector3(0f, 1.2f, 0f);

            // Renderer components
            obj.AddComponent<MeshFilter>();
            obj.AddComponent<MeshRenderer>();

            // Materials — prefer custom shaders, fall back to URP built-ins
            Shader glassShader  = FindCustomShader("BottleGlass")  ?? Shader.Find("Universal Render Pipeline/Lit");
            Shader liquidShader = FindCustomShader("LayeredLiquid") ?? Shader.Find("Universal Render Pipeline/Unlit");

            var glassMat  = new Material(glassShader)  { name = bottleName + "_Glass"  };
            var liquidMat = new Material(liquidShader) { name = bottleName + "_Liquid" };

            // Mesh
            var meshGen          = obj.AddComponent<BottleMeshGenerator>();
            meshGen.height       = 2.4f;
            meshGen.bodyRadius   = 0.35f;
            meshGen.neckRadius   = 0.15f;
            meshGen.neckHeight   = 0.4f;
            meshGen.capRadius    = 0.17f;
            meshGen.capHeight    = 0.1f;
            meshGen.glassMaterial  = glassMat;
            meshGen.liquidMaterial = liquidMat;
            meshGen.BuildMesh();

            obj.GetComponent<MeshRenderer>().sharedMaterials = new[] { glassMat, liquidMat };

            // BottleController — inject both services
            var ctrl          = obj.AddComponent<BottleController>();
            ctrl.glassMaterial  = glassMat;
            ctrl.liquidMaterial = liquidMat;

            // Build initial layers
            var layers      = new List<LiquidLayer>();
            float[] heights = { 0.25f, 0.50f, 0.75f, 1.0f };

            for (int i = 0; i < colors.Length && i < 4; i++)
            {
                if (colors[i].a > 0.01f)
                {
                    float amount = (i == 0) ? heights[0] : heights[i] - heights[i - 1];
                    layers.Add(new LiquidLayer(colors[i], amount));
                }
            }

            ctrl.Initialize(rendererService, validator, layers);
        }
    }
}
