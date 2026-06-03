using PuzzleGame.Application.Logging;
using UnityEngine;

namespace PuzzleGame.Application.Services
{
    /// <summary>
    /// Static factory for runtime particle system prefabs (splash, bubble).
    /// Extracted from AnimationService for SRP. Prefabs are created once at
    /// service init and destroyed on dispose. Tries to load from Resources first
    /// to avoid GC allocation; falls back to runtime creation if asset missing.
    /// </summary>
    internal static class ParticlePrefabFactory
    {
        private const string SplashPrefabResourcePath = "Particles/SplashParticle";
        private const string BubblePrefabResourcePath = "Particles/BubbleParticle";

        public static ParticleSystem CreateSplash()
        {
            var loaded = Resources.Load<ParticleSystem>(SplashPrefabResourcePath);
            if (loaded != null) return loaded;

            BottleLogger.LogWarning($"Splash particle prefab not found at Resources/{SplashPrefabResourcePath}. Creating fallback at runtime.");

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
            renderer.sharedMaterial = ParticleMaterialFactory.GetSplashMaterial(TextureGenerator.GetSolidCircleTex());

            return ps;
        }

        public static ParticleSystem CreateBubble()
        {
            var loaded = Resources.Load<ParticleSystem>(BubblePrefabResourcePath);
            if (loaded != null) return loaded;

            BottleLogger.LogWarning($"Bubble particle prefab not found at Resources/{BubblePrefabResourcePath}. Creating fallback at runtime.");

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
            renderer.sharedMaterial = ParticleMaterialFactory.GetBubbleMaterial(TextureGenerator.GetBubbleTex());

            return ps;
        }
    }
}
