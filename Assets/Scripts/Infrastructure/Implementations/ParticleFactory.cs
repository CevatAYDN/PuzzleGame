using System;
using UnityEngine;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Logging;
using PuzzleGame.Infrastructure.Providers;
#if ENABLE_ADDRESSABLES
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
#endif

namespace PuzzleGame.Infrastructure.Implementations
{
    /// <summary>
    /// Concrete implementation of IParticleFactory inside the Infrastructure layer.
    /// Exposes methods to create Splash and Bubble particles, while maintaining material
    /// and texture caches as instance state to avoid global static singletons.
    /// </summary>
    public class ParticleFactory : IParticleFactory
    {
        private const string SplashPrefabResourcePath = "Particles/SplashParticle";
        private const string BubblePrefabResourcePath = "Particles/BubbleParticle";
        private const string SplashPrefabAddress = "Particles/SplashParticle";
        private const string BubblePrefabAddress = "Particles/BubbleParticle";

        private readonly IAssetProvider _assetProvider;
        private ParticleSystem _cachedSplashPrefab;
        private ParticleSystem _cachedBubblePrefab;

        private Material _splashMaterial;
        private Material _bubbleMaterial;
        private Texture2D _solidCircleTex;
        private Texture2D _bubbleTex;

        public ParticleFactory(IAssetProvider assetProvider = null)
        {
            // IAssetProvider is optional — when null, LoadPrefabSync falls back to Resources.
            // This keeps non-Addressables builds working without requiring a stub provider.
            _assetProvider = assetProvider;

            if (_assetProvider != null)
            {
                _assetProvider.LoadAssetAsync<ParticleSystem>(SplashPrefabAddress, prefab =>
                {
                    _cachedSplashPrefab = prefab;
                    if (prefab != null)
                    {
                        MoldLogger.LogInfo("Splash particle prefab preloaded asynchronously via IAssetProvider.");
                    }
                });

                _assetProvider.LoadAssetAsync<ParticleSystem>(BubblePrefabAddress, prefab =>
                {
                    _cachedBubblePrefab = prefab;
                    if (prefab != null)
                    {
                        MoldLogger.LogInfo("Bubble particle prefab preloaded asynchronously via IAssetProvider.");
                    }
                });
            }
        }

        public ParticleSystem CreateSplash()
        {
            var loaded = LoadPrefabSync(SplashPrefabAddress, SplashPrefabResourcePath);
            if (loaded != null) return loaded;

            MoldLogger.LogWarning($"Splash particle prefab not found at Addressable '{SplashPrefabAddress}' or Resources/{SplashPrefabResourcePath}. Creating fallback at runtime.");

            var go = new GameObject("SplashParticle_Prefab", typeof(ParticleSystem));
            go.SetActive(false);
            UnityEngine.Object.DontDestroyOnLoad(go);
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
            renderer.sharedMaterial = GetSplashMaterial(GetSolidCircleTex());

            return ps;
        }

        public ParticleSystem CreateBubble()
        {
            var loaded = LoadPrefabSync(BubblePrefabAddress, BubblePrefabResourcePath);
            if (loaded != null) return loaded;

            MoldLogger.LogWarning($"Bubble particle prefab not found at Addressable '{BubblePrefabAddress}' or Resources/{BubblePrefabResourcePath}. Creating fallback at runtime.");

            var go = new GameObject("BubbleParticle_Prefab", typeof(ParticleSystem));
            go.SetActive(false);
            UnityEngine.Object.DontDestroyOnLoad(go);
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
            renderer.sharedMaterial = GetBubbleMaterial(GetBubbleTex());

            return ps;
        }

            private ParticleSystem LoadPrefabSync(string address, string resourcePath)
            {
                if (address == SplashPrefabAddress && _cachedSplashPrefab != null)
                {
                    return _cachedSplashPrefab;
                }
                if (address == BubblePrefabAddress && _cachedBubblePrefab != null)
                {
                    return _cachedBubblePrefab;
                }

                var fallback = Resources.Load<ParticleSystem>(resourcePath);
                if (address == SplashPrefabAddress) _cachedSplashPrefab = fallback;
                if (address == BubblePrefabAddress) _cachedBubblePrefab = fallback;
                return fallback;
            }

        // ── Material caching logic (from ParticleMaterialFactory) ─────────────────

        private Material GetSplashMaterial(Texture2D tex)
        {
            if (_splashMaterial == null)
                _splashMaterial = CreateParticleMaterial(tex);
            return _splashMaterial;
        }

        private Material GetBubbleMaterial(Texture2D tex)
        {
            if (_bubbleMaterial == null)
                _bubbleMaterial = CreateParticleMaterial(tex);
            return _bubbleMaterial;
        }

        private Material CreateParticleMaterial(Texture2D tex)
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

        // ── Texture generation logic (from TextureGenerator) ─────────────────────

        private Texture2D GetSolidCircleTex()
        {
            if (_solidCircleTex == null)
                _solidCircleTex = CreateCircleTexture(true);
            return _solidCircleTex;
        }

        private Texture2D GetBubbleTex()
        {
            if (_bubbleTex == null)
                _bubbleTex = CreateCircleTexture(false);
            return _bubbleTex;
        }

        private Texture2D CreateCircleTexture(bool solid)
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
