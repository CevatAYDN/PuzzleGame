using UnityEngine;
using UnityEditor;
using System.IO;

namespace PuzzleGame.Editor
{
    public static class ParticlePrefabGenerator
    {
        [MenuItem("Tools/PuzzleGame/Generate Missing Particles")]
        public static void GenerateParticles()
        {
            string dir = "Assets/Resources/Particles";
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
                AssetDatabase.Refresh();
            }

            // Create Splash
            string splashPath = dir + "/SplashParticle.prefab";
            if (!File.Exists(splashPath))
            {
                var go = new GameObject("SplashParticle", typeof(ParticleSystem));
                var ps = go.GetComponent<ParticleSystem>();
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

                var main = ps.main;
                main.duration = 1f;
                main.loop = true;
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.45f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3.2f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
                main.gravityModifier = new ParticleSystem.MinMaxCurve(2.0f);
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.playOnAwake = false;

                var emission = ps.emission;
                emission.rateOverTime = new ParticleSystem.MinMaxCurve(45f);

                var shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Cone;
                shape.angle = 25f;
                shape.radius = 0.04f;
                shape.rotation = new Vector3(-90f, 0f, 0f);

                var sizeOverLifetime = ps.sizeOverLifetime;
                sizeOverLifetime.enabled = true;
                var curve = new AnimationCurve();
                curve.AddKey(0f, 1f); curve.AddKey(0.7f, 0.8f); curve.AddKey(1f, 0f);
                sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);

                var renderer = go.GetComponent<ParticleSystemRenderer>();
                renderer.renderMode = ParticleSystemRenderMode.Billboard;
                
                var tex = CreateCircleTexture(true);
                var mat = CreateParticleMaterial(tex);
                // Save material and texture
                AssetDatabase.CreateAsset(tex, dir + "/SplashTex.asset");
                AssetDatabase.CreateAsset(mat, dir + "/SplashMat.mat");
                renderer.sharedMaterial = mat;

                PrefabUtility.SaveAsPrefabAsset(go, splashPath);
                Object.DestroyImmediate(go);
            }

            // Create Bubble
            string bubblePath = dir + "/BubbleParticle.prefab";
            if (!File.Exists(bubblePath))
            {
                var go = new GameObject("BubbleParticle", typeof(ParticleSystem));
                var ps = go.GetComponent<ParticleSystem>();
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

                var main = ps.main;
                main.duration = 1f;
                main.loop = true;
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.2f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.10f);
                main.gravityModifier = new ParticleSystem.MinMaxCurve(-0.06f);
                main.startColor = new Color(1f, 1f, 1f, 0.45f);
                main.simulationSpace = ParticleSystemSimulationSpace.Local;
                main.playOnAwake = false;

                var emission = ps.emission;
                emission.rateOverTime = new ParticleSystem.MinMaxCurve(15f);

                var shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Cone;
                shape.angle = 0f;
                shape.radius = 0.15f;
                shape.length = 0.1f;
                shape.rotation = new Vector3(-90f, 0f, 0f);

                var noise = ps.noise;
                noise.enabled = true;
                noise.frequency = 2.0f;
                noise.strength = new ParticleSystem.MinMaxCurve(0.15f);
                noise.octaveCount = 1;

                var sizeOverLifetime = ps.sizeOverLifetime;
                sizeOverLifetime.enabled = true;
                var curve = new AnimationCurve();
                curve.AddKey(0f, 0.2f); curve.AddKey(0.2f, 1.0f);
                curve.AddKey(0.8f, 1.0f); curve.AddKey(1f, 0f);
                sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);

                var renderer = go.GetComponent<ParticleSystemRenderer>();
                renderer.renderMode = ParticleSystemRenderMode.Billboard;

                var tex = CreateCircleTexture(false);
                var mat = CreateParticleMaterial(tex);
                AssetDatabase.CreateAsset(tex, dir + "/BubbleTex.asset");
                AssetDatabase.CreateAsset(mat, dir + "/BubbleMat.mat");
                renderer.sharedMaterial = mat;

                PrefabUtility.SaveAsPrefabAsset(go, bubblePath);
                Object.DestroyImmediate(go);
            }

            AssetDatabase.Refresh();
            Debug.Log("[ParticlePrefabGenerator] Successfully generated Splash and Bubble particle prefabs in Resources/Particles/");
        }

        private static Material CreateParticleMaterial(Texture2D tex)
        {
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Sprites/Default");
            var mat = new Material(shader);
            if (tex != null)
            {
                mat.mainTexture = tex;
                if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
            }
            mat.color = Color.white;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
            return mat;
        }

        private static Texture2D CreateCircleTexture(bool solid)
        {
            int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            float center = size / 2f;
            float radius = size * 0.45f;
            float innerRadius = size * 0.33f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    if (solid)
                    {
                        float alpha = Mathf.Clamp01((radius - dist) / 1.5f);
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                    }
                    else
                    {
                        float edgeAlpha = Mathf.Clamp01((radius - dist) / 1.5f);
                        float innerAlpha = Mathf.Clamp01((dist - innerRadius) / 1.5f);
                        float alpha = Mathf.Min(edgeAlpha, innerAlpha);

                        float hDx = x - (center - radius * 0.3f);
                        float hDy = y - (center + radius * 0.3f);
                        float hDist = Mathf.Sqrt(hDx * hDx + hDy * hDy);
                        float highlight = Mathf.Clamp01((radius * 0.15f - hDist) / 1.0f) * 0.8f;

                        float inside = (dist < innerRadius) ? 0.05f * (dist / innerRadius) : 0f;
                        float finalAlpha = Mathf.Max(alpha * 0.8f, highlight, inside);
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, finalAlpha));
                    }
                }
            }
            tex.Apply();
            return tex;
        }
    }
}
