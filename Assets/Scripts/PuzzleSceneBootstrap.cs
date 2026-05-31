using System;
using System.Collections.Generic;
using UnityEngine;

namespace BottleShaders
{
    /// <summary>
    /// Creates a playable puzzle board automatically when the scene is empty.
    /// This makes the project run out-of-the-box without manual editor tools.
    /// </summary>
    public static class PuzzleSceneBootstrap
    {
        private const int BottleCapacity = 4;
        private const int ColorCount = 8;
        private const int EmptyBottleCount = 2;
        private const int ShuffleMoves = 220;

        private static readonly Color[] Palette =
        {
            new Color(0.95f, 0.34f, 0.34f, 1f), // red
            new Color(0.23f, 0.63f, 0.98f, 1f), // blue
            new Color(0.28f, 0.82f, 0.45f, 1f), // green
            new Color(0.95f, 0.77f, 0.23f, 1f), // yellow
            new Color(0.81f, 0.43f, 0.93f, 1f), // purple
            new Color(0.98f, 0.55f, 0.22f, 1f), // orange
            new Color(0.28f, 0.9f, 0.84f, 1f),  // cyan
            new Color(0.98f, 0.46f, 0.72f, 1f)  // pink
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void BootstrapAfterSceneLoad()
        {
            if (!Application.isPlaying) return;

            EnsureGameManager();
            EnsureCameraAndLighting();
            EnsureEnvironment();
            EnsurePuzzleBoard();
        }

        private static void EnsureGameManager()
        {
            if (FindAny<GameManager>() != null) return;

            GameObject gm = new GameObject("GameManager");
            gm.AddComponent<GameManager>();
        }

        private static void EnsureCameraAndLighting()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                cam = FindAny<Camera>();
                if (cam == null)
                {
                    GameObject camObj = new GameObject("Main Camera");
                    cam = camObj.AddComponent<Camera>();
                    camObj.tag = "MainCamera";
                }
            }

            Transform camTf = cam.transform;
            camTf.position = new Vector3(0f, 0.5f, -14f);
            camTf.LookAt(new Vector3(0f, 0.5f, 0f));
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.05f, 0.16f, 1f);
            cam.fieldOfView = 52f;

            Light dir = null;
            Light[] lights = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < lights.Length; i++)
            {
                if (lights[i] != null && lights[i].type == LightType.Directional)
                {
                    dir = lights[i];
                    break;
                }
            }

            if (dir == null)
            {
                GameObject lightObj = new GameObject("Directional Light");
                dir = lightObj.AddComponent<Light>();
                dir.type = LightType.Directional;
            }

            dir.intensity = 1.35f;
            dir.color = new Color(1f, 0.98f, 0.94f, 1f);
            dir.transform.rotation = Quaternion.Euler(46f, -28f, 0f);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.25f, 0.20f, 0.35f, 1f);
            RenderSettings.ambientEquatorColor = new Color(0.16f, 0.13f, 0.22f, 1f);
            RenderSettings.ambientGroundColor = new Color(0.07f, 0.05f, 0.10f, 1f);
            RenderSettings.reflectionIntensity = 0.7f;
        }

        private static void EnsureEnvironment()
        {
            if (GameObject.Find("RuntimeEnvironment") != null) return;

            GameObject envRoot = new GameObject("RuntimeEnvironment");

            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            Shader unlit = Shader.Find("Universal Render Pipeline/Unlit");
            if (lit == null) lit = Shader.Find("Standard");
            if (unlit == null) unlit = Shader.Find("Unlit/Color");

            Material tableMat = new Material(lit);
            SetMaterialColor(tableMat, new Color(0.16f, 0.12f, 0.24f, 1f));
            SetMaterialFloat(tableMat, 0.35f, "_Smoothness", "_Glossiness");

            Material accentMat = new Material(lit);
            SetMaterialColor(accentMat, new Color(0.40f, 0.16f, 0.54f, 1f));
            SetMaterialFloat(accentMat, 0.55f, "_Smoothness", "_Glossiness");

            Material backdropMat = new Material(unlit);
            SetMaterialColor(backdropMat, new Color(0.11f, 0.07f, 0.2f, 1f));

            // Floor platform
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            floor.name = "TableBase";
            floor.transform.SetParent(envRoot.transform, false);
            floor.transform.position = new Vector3(0f, -5.35f, 0f);
            floor.transform.localScale = new Vector3(8.6f, 0.55f, 5.6f);
            floor.GetComponent<MeshRenderer>().sharedMaterial = tableMat;

            // Backdrop wall
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Quad);
            wall.name = "Backdrop";
            wall.transform.SetParent(envRoot.transform, false);
            wall.transform.position = new Vector3(0f, -0.1f, 8f);
            wall.transform.localScale = new Vector3(24f, 16f, 1f);
            wall.GetComponent<MeshRenderer>().sharedMaterial = backdropMat;
            UnityEngine.Object.Destroy(wall.GetComponent<Collider>());

            // Decorative cauldron
            GameObject cauldron = new GameObject("Cauldron");
            cauldron.transform.SetParent(envRoot.transform, false);
            cauldron.transform.position = new Vector3(0f, -7.3f, 0f);

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            body.transform.SetParent(cauldron.transform, false);
            body.transform.localScale = new Vector3(3.2f, 1.9f, 3.2f);
            body.GetComponent<MeshRenderer>().sharedMaterial = accentMat;

            GameObject rim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rim.transform.SetParent(cauldron.transform, false);
            rim.transform.localPosition = new Vector3(0f, 0.85f, 0f);
            rim.transform.localScale = new Vector3(2.9f, 0.09f, 2.9f);
            rim.GetComponent<MeshRenderer>().sharedMaterial = tableMat;

            GameObject liquid = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            liquid.transform.SetParent(cauldron.transform, false);
            liquid.transform.localPosition = new Vector3(0f, 0.78f, 0f);
            liquid.transform.localScale = new Vector3(2.6f, 0.045f, 2.6f);

            Material liquidMat = new Material(unlit);
            SetMaterialColor(liquidMat, new Color(1f, 0.55f, 0.95f, 0.95f));
            liquid.GetComponent<MeshRenderer>().sharedMaterial = liquidMat;

            GameObject glowObj = new GameObject("CauldronGlow");
            glowObj.transform.SetParent(cauldron.transform, false);
            glowObj.transform.localPosition = new Vector3(0f, 1.85f, 0f);
            Light glow = glowObj.AddComponent<Light>();
            glow.type = LightType.Point;
            glow.color = new Color(1f, 0.52f, 0.95f, 1f);
            glow.range = 7f;
            glow.intensity = 3.3f;
        }

        private static void EnsurePuzzleBoard()
        {
            if (UnityEngine.Object.FindObjectsByType<BottleController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length > 0)
                return;

            GameObject boardRoot = new GameObject("BottleBoard");

            Shader glassShader = Shader.Find("Custom/BottleGlass");
            Shader liquidShader = Shader.Find("Custom/LayeredLiquid");

            if (glassShader == null) glassShader = Shader.Find("Universal Render Pipeline/Lit");
            if (liquidShader == null) liquidShader = Shader.Find("Universal Render Pipeline/Unlit");

            Material sharedGlass = new Material(glassShader);
            if (sharedGlass.HasProperty("_Color"))
                sharedGlass.SetColor("_Color", new Color(1f, 1f, 1f, 0.22f));
            if (sharedGlass.HasProperty("_Smoothness"))
                sharedGlass.SetFloat("_Smoothness", 0.95f);

            Material sharedLiquid = new Material(liquidShader);
            if (sharedLiquid.HasProperty("_Transparency"))
                sharedLiquid.SetFloat("_Transparency", 0.12f);
            if (sharedLiquid.HasProperty("_LayerBoundaryDarken"))
                sharedLiquid.SetFloat("_LayerBoundaryDarken", 0.26f);

            List<List<int>> board = GenerateSolvableBoard();

            const int maxPerRow = 5;
            const float spacingX = 1.45f;
            const float spacingY = 3.1f;
            const float startY = 3.0f;

            for (int i = 0; i < board.Count; i++)
            {
                int row = i / maxPerRow;
                int col = i % maxPerRow;
                int rowCount = Mathf.Min(maxPerRow, board.Count - row * maxPerRow);

                float x = (col - (rowCount - 1) * 0.5f) * spacingX;
                float y = startY - row * spacingY;

                CreateBottle(new Vector3(x, y, 0f), $"Bottle_{i + 1}", board[i], sharedGlass, sharedLiquid, boardRoot.transform);
            }
        }

        private static void CreateBottle(Vector3 position, string name, List<int> units, Material glassMat, Material liquidMat, Transform parent)
        {
            GameObject bottleObj = new GameObject(name);
            bottleObj.transform.SetParent(parent, false);
            bottleObj.transform.position = position;

            CapsuleCollider collider = bottleObj.AddComponent<CapsuleCollider>();
            collider.radius = 0.42f;
            collider.height = 2.45f;
            collider.center = new Vector3(0f, 1.2f, 0f);

            MeshFilter meshFilter = bottleObj.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = bottleObj.AddComponent<MeshRenderer>();

            BottleMeshGenerator meshGen = bottleObj.AddComponent<BottleMeshGenerator>();
            meshGen.height = 2.4f;
            meshGen.bodyRadius = 0.35f;
            meshGen.neckRadius = 0.15f;
            meshGen.neckHeight = 0.4f;
            meshGen.capRadius = 0.18f;
            meshGen.capHeight = 0.1f;
            meshGen.glassMaterial = glassMat;
            meshGen.liquidMaterial = liquidMat;
            meshGen.BuildMesh();

            if (meshFilter.sharedMesh == null)
            {
                // Safety fallback (BuildMesh early exit)
                meshGen.BuildMesh();
            }

            meshRenderer.sharedMaterials = new[] { glassMat, liquidMat };

            BottleController bottleCtrl = bottleObj.AddComponent<BottleController>();
            bottleCtrl.glassMaterial = glassMat;
            bottleCtrl.liquidMaterial = liquidMat;

            Color[] colors = new Color[BottleCapacity];
            float[] fills = new float[BottleCapacity];

            int count = Mathf.Clamp(units.Count, 0, BottleCapacity);
            for (int i = 0; i < BottleCapacity; i++)
            {
                if (i < count)
                {
                    int colorIndex = Mathf.Clamp(units[i], 0, Palette.Length - 1);
                    colors[i] = Palette[colorIndex];
                    fills[i] = (i + 1) / (float)BottleCapacity;
                }
                else
                {
                    colors[i] = Color.clear;
                    fills[i] = count / (float)BottleCapacity;
                }
            }

            bottleCtrl.SetFillLevelsInstant(fills);
            bottleCtrl.SetLayerColors(colors);
        }

        private static List<List<int>> GenerateSolvableBoard()
        {
            System.Random rng = new System.Random((int)DateTime.Now.Ticks);

            List<List<int>> board = new List<List<int>>();
            for (int c = 0; c < ColorCount; c++)
            {
                List<int> bottle = new List<int>(BottleCapacity);
                for (int i = 0; i < BottleCapacity; i++)
                    bottle.Add(c);
                board.Add(bottle);
            }

            for (int i = 0; i < EmptyBottleCount; i++)
                board.Add(new List<int>(BottleCapacity));

            int lastFrom = -1;
            int lastTo = -1;
            int appliedMoves = 0;
            int maxAttempts = ShuffleMoves * 20;

            for (int attempt = 0; attempt < maxAttempts && appliedMoves < ShuffleMoves; attempt++)
            {
                int from = rng.Next(board.Count);
                int to = rng.Next(board.Count);

                if (from == to) continue;
                if (from == lastTo && to == lastFrom) continue; // avoid immediate undo

                List<int> src = board[from];
                List<int> dst = board[to];

                if (src.Count == 0 || dst.Count >= BottleCapacity) continue;

                int color = src[src.Count - 1];
                if (dst.Count > 0 && dst[dst.Count - 1] != color) continue;

                int sameBlock = 1;
                for (int i = src.Count - 2; i >= 0; i--)
                {
                    if (src[i] != color) break;
                    sameBlock++;
                }

                int space = BottleCapacity - dst.Count;
                int transfer = Math.Min(sameBlock, space);
                if (transfer <= 0) continue;

                for (int i = 0; i < transfer; i++)
                {
                    dst.Add(color);
                    src.RemoveAt(src.Count - 1);
                }

                appliedMoves++;
                lastFrom = from;
                lastTo = to;
            }

            if (IsSolved(board))
            {
                // guaranteed unsolved fallback
                return GenerateSolvableBoard();
            }

            return board;
        }

        private static bool IsSolved(List<List<int>> board)
        {
            for (int i = 0; i < board.Count; i++)
            {
                List<int> b = board[i];
                if (b.Count == 0) continue;
                if (b.Count != BottleCapacity) return false;

                int color = b[0];
                for (int j = 1; j < b.Count; j++)
                {
                    if (b[j] != color)
                        return false;
                }
            }

            return true;
        }

        private static void SetMaterialColor(Material material, Color color)
        {
            if (material == null) return;

            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            else if (material.HasProperty("_Color"))
                material.SetColor("_Color", color);
        }

        private static void SetMaterialFloat(Material material, float value, params string[] propertyNames)
        {
            if (material == null || propertyNames == null) return;

            for (int i = 0; i < propertyNames.Length; i++)
            {
                if (material.HasProperty(propertyNames[i]))
                {
                    material.SetFloat(propertyNames[i], value);
                    return;
                }
            }
        }

        private static T FindAny<T>() where T : UnityEngine.Object
        {
            T[] objects = UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            return objects != null && objects.Length > 0 ? objects[0] : null;
        }
    }
}
