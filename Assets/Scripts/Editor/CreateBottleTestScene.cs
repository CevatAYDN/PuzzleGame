using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BottleShaders.Editor
{
    public class CreateBottleTestScene
    {
        [MenuItem("Tools/Bottle Shader/Create Test Scene")]
        public static void CreateScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // GameManager
            GameObject gameManagerObj = new GameObject("GameManager");
            gameManagerObj.AddComponent<GameManager>();

            // Cauldron (Kazan) Ekleme
            CreateCauldron();

            // Main Camera (Portrait Mobile)
            GameObject cameraObj = new GameObject("Main Camera");
            Camera camera = cameraObj.AddComponent<Camera>();
            camera.backgroundColor = new Color(0.12f, 0.05f, 0.35f); // Dark purple background like in the image
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.fieldOfView = 60f;
            cameraObj.transform.position = new Vector3(0, 0, -14); // Pulled back to see everything
            cameraObj.transform.LookAt(new Vector3(0, 1, 0));

            // Directional Light
            GameObject lightObj = new GameObject("Directional Light");
            Light light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = Color.white;
            light.intensity = 1.0f;
            lightObj.transform.rotation = Quaternion.Euler(50, -30, 0);

            // Ambient Light settings
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.4f, 0.4f, 0.6f);

            Color cRed = new Color(0.9f, 0.2f, 0.2f);
            Color cBlue = new Color(0.2f, 0.6f, 1.0f);
            Color cGreen = new Color(0.5f, 0.9f, 0.1f);
            Color cYellow = new Color(0.9f, 0.9f, 0.1f);

            float[] fullFills = new float[] { 0.25f, 0.5f, 0.75f, 1.0f };
            float[] emptyFills = new float[] { 0f, 0f, 0f, 0f };
            Color[] emptyColors = new Color[] { Color.clear, Color.clear, Color.clear, Color.clear };

            // Row 1 (Top)
            float yPos1 = 4.5f;
            CreateExampleBottle(new Vector3(-2.4f, yPos1, 0), "Bottle1_1", new Color[] { cYellow, cRed, cBlue, cBlue }, fullFills);
            CreateExampleBottle(new Vector3(-1.2f, yPos1, 0), "Bottle1_2", new Color[] { cRed, cBlue, cYellow, cGreen }, fullFills);
            CreateExampleBottle(new Vector3(0, yPos1, 0), "Bottle1_3", new Color[] { cRed, cBlue, cGreen, cYellow }, fullFills);
            CreateExampleBottle(new Vector3(1.2f, yPos1, 0), "Bottle1_4", new Color[] { cRed, cBlue, cGreen, cBlue }, fullFills);
            CreateExampleBottle(new Vector3(2.4f, yPos1, 0), "Bottle1_5", new Color[] { cYellow, cRed, cBlue, cRed }, fullFills);

            // Row 2
            float yPos2 = 1.5f;
            CreateExampleBottle(new Vector3(-2.4f, yPos2, 0), "Bottle2_1", new Color[] { cGreen, cYellow, cRed, cBlue }, fullFills);
            CreateExampleBottle(new Vector3(-1.2f, yPos2, 0), "Bottle2_2", new Color[] { cGreen, cRed, cGreen, cYellow }, fullFills);
            CreateExampleBottle(new Vector3(0, yPos2, 0), "Bottle2_3", new Color[] { cRed, cYellow, cYellow, cBlue }, fullFills);
            CreateExampleBottle(new Vector3(1.2f, yPos2, 0), "Bottle2_4", emptyColors, emptyFills); // Empty
            CreateExampleBottle(new Vector3(2.4f, yPos2, 0), "Bottle2_5", emptyColors, emptyFills); // Empty

            // Row 3
            float yPos3 = -1.5f;
            CreateExampleBottle(new Vector3(-2.4f, yPos3, 0), "Bottle3_1", new Color[] { cRed, cBlue, cYellow, cGreen }, fullFills);
            CreateExampleBottle(new Vector3(-1.2f, yPos3, 0), "Bottle3_2", new Color[] { cRed, cYellow, cBlue, cYellow }, fullFills);
            CreateExampleBottle(new Vector3(0, yPos3, 0), "Bottle3_3", new Color[] { cYellow, cGreen, cRed, cBlue }, fullFills);
            CreateExampleBottle(new Vector3(1.2f, yPos3, 0), "Bottle3_4", new Color[] { cGreen, cYellow, cRed, cBlue }, fullFills);
            CreateExampleBottle(new Vector3(2.4f, yPos3, 0), "Bottle3_5", new Color[] { cRed, cBlue, cGreen, cYellow }, fullFills);
            
            // Row 4 (Bottom)
            float yPos4 = -4.5f;
            CreateExampleBottle(new Vector3(-2.4f, yPos4, 0), "Bottle4_1", new Color[] { cRed, cBlue, cGreen, cYellow }, fullFills);
            CreateExampleBottle(new Vector3(-1.2f, yPos4, 0), "Bottle4_2", new Color[] { cYellow, cRed, cYellow, cBlue }, fullFills);
            CreateExampleBottle(new Vector3(0, yPos4, 0), "Bottle4_3", new Color[] { cYellow, cRed, cGreen, cBlue }, fullFills);
            CreateExampleBottle(new Vector3(1.2f, yPos4, 0), "Bottle4_4", new Color[] { cYellow, cRed, cBlue, cGreen }, fullFills);
            CreateExampleBottle(new Vector3(2.4f, yPos4, 0), "Bottle4_5", new Color[] { cBlue, cGreen, cYellow, cRed }, fullFills);

            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("[Bottle Shader] Test scene created!");
        }

                private static void CreateCauldron()
        {
            GameObject cauldronObj = new GameObject("Cauldron");
            cauldronObj.transform.position = new Vector3(0, -7.5f, 0);

            // Kazan gövdesi
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            body.transform.SetParent(cauldronObj.transform);
            body.transform.localPosition = Vector3.zero;
            body.transform.localScale = new Vector3(3f, 2f, 3f);
            
            MeshRenderer mr = body.GetComponent<MeshRenderer>();
            Material cauldronMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            cauldronMat.color = new Color(0.8f, 0.2f, 0.8f); // Mor/Pembe tonları
            mr.sharedMaterial = cauldronMat;

            // Kazan ağzı
            GameObject rim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rim.transform.SetParent(cauldronObj.transform);
            rim.transform.localPosition = new Vector3(0, 0.9f, 0);
            rim.transform.localScale = new Vector3(2.8f, 0.1f, 2.8f);
            rim.GetComponent<MeshRenderer>().sharedMaterial = cauldronMat;

            // İçindeki parlak sıvı
            GameObject liquid = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            liquid.transform.SetParent(cauldronObj.transform);
            liquid.transform.localPosition = new Vector3(0, 0.8f, 0);
            liquid.transform.localScale = new Vector3(2.6f, 0.05f, 2.6f);
            
            Material liquidMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            liquidMat.SetColor("_BaseColor", new Color(1f, 0.5f, 1f, 0.8f)); // Parlak pembe sıvı
            liquid.GetComponent<MeshRenderer>().sharedMaterial = liquidMat;
            
            // Kazan ışığı (Glow)
            GameObject lightObj = new GameObject("CauldronLight");
            lightObj.transform.SetParent(cauldronObj.transform);
            lightObj.transform.localPosition = new Vector3(0, 2f, 0);
            Light l = lightObj.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(1f, 0.5f, 1f);
            l.range = 5f;
            l.intensity = 2f;
        }

        private static Shader FindCustomShader(string name)
        {
            string[] guids = AssetDatabase.FindAssets("t:Shader " + name);
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<Shader>(path);
            }
            return null;
        }

        private static void CreateExampleBottle(Vector3 position, string name, Color[] colors, float[] fills)
        {
            GameObject bottleObj = new GameObject(name);
            bottleObj.transform.position = position;

            // 1) Collider FIRST
            CapsuleCollider collider = bottleObj.AddComponent<CapsuleCollider>();
            collider.radius = 0.4f;
            collider.height = 2.4f;
            collider.center = new Vector3(0, 1.2f, 0);

            // 2) MeshFilter + MeshRenderer (needed by BottleMeshGenerator)
            var meshFilter = bottleObj.AddComponent<MeshFilter>();
            var meshRenderer = bottleObj.AddComponent<MeshRenderer>();

            // 3) Find shaders
            Shader glassShader = FindCustomShader("BottleGlass");
            Shader liquidShader = FindCustomShader("LayeredLiquid");

            if (glassShader == null) glassShader = Shader.Find("Universal Render Pipeline/Lit");
            if (liquidShader == null) liquidShader = Shader.Find("Universal Render Pipeline/Unlit");

            Material glassMat = new Material(glassShader);
            glassMat.name = name + "_Glass";

            Material liquidMat = new Material(liquidShader);
            liquidMat.name = name + "_Liquid";

            // Set defaults on liquid material
            liquidMat.SetColor("_Color1", colors.Length > 0 ? colors[0] : Color.clear);
            liquidMat.SetColor("_Color2", colors.Length > 1 ? colors[1] : Color.clear);
            liquidMat.SetColor("_Color3", colors.Length > 2 ? colors[2] : Color.clear);
            liquidMat.SetColor("_Color4", colors.Length > 3 ? colors[3] : Color.clear);
            liquidMat.SetFloat("_Fill1", fills.Length > 0 ? fills[0] : 0f);
            liquidMat.SetFloat("_Fill2", fills.Length > 1 ? fills[1] : 0f);
            liquidMat.SetFloat("_Fill3", fills.Length > 2 ? fills[2] : 0f);
            liquidMat.SetFloat("_Fill4", fills.Length > 3 ? fills[3] : 0f);
            liquidMat.SetFloat("_Transparency", 0.1f);
            liquidMat.SetFloat("_SurfaceHeight", 1.0f);

            // 4) Generate mesh FIRST
            var meshGen = bottleObj.AddComponent<BottleMeshGenerator>();
            meshGen.height = 2.4f;
            meshGen.bodyRadius = 0.35f;
            meshGen.neckRadius = 0.15f;
            meshGen.neckHeight = 0.4f;
            meshGen.capRadius = 0.17f;
            meshGen.capHeight = 0.1f;
            meshGen.glassMaterial = glassMat;
            meshGen.liquidMaterial = liquidMat;

            meshGen.BuildMesh();

            // Apply materials immediately
            if (meshRenderer != null)
                meshRenderer.sharedMaterials = new Material[] { glassMat, liquidMat };

            // 5) BottleController
            BottleController bottleCtrl = bottleObj.AddComponent<BottleController>();
            bottleCtrl.glassMaterial = glassMat;
            bottleCtrl.liquidMaterial = liquidMat;

            // Set serialized fillLevels so values survive into Play mode
            bottleCtrl.SetFillLevelsInstant(fills);
            bottleCtrl.SetLayerColors(colors);
        }
    }
}