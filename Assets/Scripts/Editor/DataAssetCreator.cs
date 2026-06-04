using UnityEditor;
using UnityEngine;
using PuzzleGame.Application.Configuration;

namespace PuzzleGame.Editor
{
    /// <summary>
    /// Static helper — ScriptableObject asset'lerini oluşturur/günceller.
    /// Her asset için ayrı ayrı overrideExisting toggle ile çağrılabilir.
    /// </summary>
    public static class DataAssetCreator
    {
        public const string DataPath = "Assets/Resources/Data";

        public struct AssetResult
        {
            public string name;
            public bool created;
            public bool overwritten;
        }

        /// <summary>
        /// Tek bir SO oluştur/güncelle.
        /// overrideExisting=true → mevcut değerleri default'a resetle.
        /// overrideExisting=false → sadece null ise oluştur.
        /// </summary>
        public static AssetResult Ensure<T>(string fileName, System.Action<T> setup, bool overrideExisting)
            where T : ScriptableObject
        {
            if (!System.IO.Directory.Exists(DataPath))
                System.IO.Directory.CreateDirectory(DataPath);

            string fullPath = $"{DataPath}/{fileName}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<T>(fullPath);

            if (existing != null)
            {
                if (overrideExisting)
                {
                    if (setup != null)
                    {
                        setup(existing);
                    }
                    EditorUtility.SetDirty(existing);
                    return new AssetResult { name = fileName, overwritten = true, created = false };
                }
                return new AssetResult { name = fileName, created = false, overwritten = false };
            }

            var asset = ScriptableObject.CreateInstance<T>();
            if (asset == null)
            {
                Debug.LogError($"[DataAssetCreator] Failed to create ScriptableObject instance of type {typeof(T).Name}");
                return new AssetResult { name = fileName, created = false, overwritten = false };
            }

            if (setup != null)
            {
                setup(asset);
            }
            AssetDatabase.CreateAsset(asset, fullPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return new AssetResult { name = fileName, created = true, overwritten = false };
        }

        /// <summary>
        /// Tüm varsayılan SO'ları oluştur.
        /// overridePerAsset: her bir asset için ayrı karar.
        /// </summary>
        public static AssetResult[] CreateAllDefaults(System.Func<string, bool> overridePerAsset)
        {
            var results = new AssetResult[]
            {
                Ensure<GameConfig>("GameConfig", c => {
                    c.MoldLayerMask = ~0;
                    c.colorMatchTolerance = 0.05f;
                    c.maxLayersPerMold = 4;
                    c.saturationBoost = 1.35f;
                    c.brightnessBoost = 1.2f;
                }, overridePerAsset("GameConfig")),

                Ensure<AnimationConfig>("AnimationConfig", a => {
                    a.liftHeight = 1.0f;
                    a.liftDuration = 0.4f;
                    a.CastDuration = 0.6f;
                    a.returnDuration = 0.4f;
                    a.hoverAmplitude = 0.08f;
                    a.hoverFrequency = 3.5f;
                    a.streamWidth = 0.08f;
                    a.shakeDuration = 0.25f;
                    a.shakeAngle = 8f;
                    a.successShakeAmplitude = 0.05f;
                    a.successShakeDuration = 0.1f;
                    a.errorShakeAmplitude = 0.15f;
                    a.errorShakeDuration = 0.3f;
                    a.cameraShakeFrequency = 10f;
                    a.tiltPhasePortion = 0.25f;
                    a.flowPhasePortion = 0.50f;
                    a.returnPhasePortion = 0.25f;
                }, overridePerAsset("AnimationConfig")),

                Ensure<MoldVisualConfig>("MoldVisualConfig", b => {
                    b.saturationBoost = 1.35f;
                    b.brightnessBoost = 1.2f;
                    b.maxLayers = 4;
                    b.castImpulseStrength = 2.0f;
                    b.sparkleIntensity = 0.1f;
                    b.sparkleSize = 12f;
                    b.layerBoundaryWidth = 0.012f;
                }, overridePerAsset("MoldVisualConfig")),

                Ensure<WobbleConfig>("WobbleConfig", w => {
                    w.maxWobble = 0.05f;
                    w.wobbleSpeed = 6.0f;
                    w.recoveryRate = 1.5f;
                    w.movementMultiplier = 1.0f;
                    w.rotationMultiplier = 0.15f;
                    w.updateInterval = 0.05f;
                }, overridePerAsset("WobbleConfig")),

                Ensure<LevelConfig>("LevelConfig", l => {
                    l.autoGenerateLevel = true;
                    l.emptyMoldCount = 2;
                    l.randomSeed = 0;
                    l.palette = new Color[]
                    {
                        new Color(0.95f, 0.20f, 0.25f),
                        new Color(0.20f, 0.55f, 0.95f),
                        new Color(0.30f, 0.85f, 0.35f),
                        new Color(0.98f, 0.80f, 0.15f),
                        new Color(0.70f, 0.30f, 0.90f),
                        new Color(0.95f, 0.50f, 0.15f),
                    };
                }, overridePerAsset("LevelConfig")),

                Ensure<AudioConfig>("AudioConfig", a => {
                    a.masterVolume = 1.0f;
                    a.musicVolume = 0.6f;
                    a.sfxVolume = 0.8f;
                    a.sfxPoolSize = 8;
                    a.musicPoolSize = 2;
                    a.spatialBlend3D = false;
                }, overridePerAsset("AudioConfig")),

                Ensure<StreamVFXConfig>("StreamVFXConfig", s => {
                    s.flowIntensity = 0.8f;
                    s.colorIntensityBoost = 1.5f;
                    s.streamWidthMultiplier = 1f;
                    s.particleCapacity = 128;
                    s.boundsRadius = 3f;
                    s.scaleBoundsWithDistance = true;
                    s.boundsScalePerUnit = 1.5f;
                    s.useDistanceScaledParticles = true;
                    s.particlesPerUnitDistance = 40f;
                    s.enableTrail = true;
                    s.trailAlpha = 0.15f;
                    s.trailSampleCount = 8;
                    s.trailFadeDuration = 0.5f;
                }, overridePerAsset("StreamVFXConfig")),

                Ensure<PourConfig>("PourConfig", p => {
                    p.pourDuration = 1.2f;
                    p.distanceSpeedMultiplier = 1.5f;
                    p.minFlowIntensity = 0.3f;
                    p.maxFlowIntensity = 1.5f;
                    p.overrideVFX = false;
                    p.flowIntensity = 0.8f;
                    p.streamWidth = 0.08f;
                    p.streamArcHeight = 0.1f;
                    p.showPourPreview = true;
                    p.showAffectedBoundaries = false;
                }, overridePerAsset("PourConfig")),
            };

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return results;
        }

        /// <summary>
        /// Belirtilen SO tipinin asset path'de var olup olmadığını döndürür.
        /// </summary>
        public static bool Exists<T>(string fileName) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>($"{DataPath}/{fileName}.asset");
            return existing != null;
        }

        /// <summary>
        /// Tüm ayarlanabilir SO asset'lerinin varlık durumunu döndürür.
        /// </summary>
        public static System.Collections.Generic.Dictionary<string, bool> CheckAllExist()
        {
            return new System.Collections.Generic.Dictionary<string, bool>
            {
                ["GameConfig"]         = Exists<GameConfig>("GameConfig"),
                ["AnimationConfig"]    = Exists<AnimationConfig>("AnimationConfig"),
                ["MoldVisualConfig"] = Exists<MoldVisualConfig>("MoldVisualConfig"),
                ["WobbleConfig"]       = Exists<WobbleConfig>("WobbleConfig"),
                ["LevelConfig"]        = Exists<LevelConfig>("LevelConfig"),
                ["AudioConfig"]        = Exists<AudioConfig>("AudioConfig"),
                ["StreamVFXConfig"]   = Exists<StreamVFXConfig>("StreamVFXConfig"),
                ["PourConfig"]        = Exists<PourConfig>("PourConfig"),
            };
        }
    }
}
