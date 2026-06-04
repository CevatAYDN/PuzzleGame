using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Configuration;
using PuzzleGame.Application.Events;
using PuzzleGame.Application.Logging;
using UnityEngine;
using UnityEngine.VFX;

namespace PuzzleGame.Infrastructure.Implementations
{
    /// <summary>
    /// VFX Graph manager for the Casting Ore stream.
    /// Uses StreamVFXConfig for all visual parameters — no hardcoded defaults.
    /// Publishes VFXStatusEvent for the debug overlay.
    /// </summary>
    public class StreamRenderer : IStreamRenderer
    {
        private readonly StreamVFXConfig _config;
        private readonly IEventAggregator _eventAggregator;
        private int _frameCounter;

        public StreamRenderer(StreamVFXConfig config, IEventAggregator eventAggregator)
        {
            _config = config ?? throw new System.ArgumentNullException(nameof(config));
            _eventAggregator = eventAggregator;
        }

        public VisualEffect EnsureEffect(GameObject owner)
        {
            var vfx = owner.GetComponent<VisualEffect>();
            if (vfx == null)
            {
                vfx = owner.AddComponent<VisualEffect>();
                var asset = Resources.Load<VisualEffectAsset>("MagmaFlow");
                if (asset != null)
                {
                    vfx.visualEffectAsset = asset;
                    MoldLogger.LogDebug("MagmaFlow.vfx loaded successfully.");
                }
                else
                {
                    MoldLogger.LogError(
                        "[StreamRenderer] MagmaFlow.vfx not found in Resources folder. " +
                        "Stream rendering will not work. Please create or import MagmaFlow.vfx.",
                        owner);
                    _eventAggregator?.Publish(VFXStatusEvent.Missing(vfx.GetEntityId()));
                }
            }
            return vfx;
        }

        public void SetColor(VisualEffect vfx, Color color)
        {
            if (vfx == null)
            {
                MoldLogger.LogError("StreamRenderer.SetColor called with null VFX reference.");
                return;
            }

            if (vfx.HasVector4("OreColor"))
            {
                // Apply HDR color boost for premium visual quality
                Color boosted = new Color(
                    color.r * _config.colorIntensityBoost,
                    color.g * _config.colorIntensityBoost,
                    color.b * _config.colorIntensityBoost,
                    color.a);
                vfx.SetVector4("OreColor", boosted);
            }
        }

        public void Update(VisualEffect vfx, IMoldView source, IMoldView target,
                           Transform sourceT, Transform targetT, float t, AnimationConfig config)
        {
            if (vfx == null)
            {
                MoldLogger.LogError("StreamRenderer.Update called with null VFX — stream not rendering.");
                return;
            }

            if (sourceT == null || targetT == null)
            {
                MoldLogger.LogWarning("StreamRenderer.Update: source or target transform is null.");
                return;
            }

            // Calculate start and end positions
            Vector3 sourceMouth = sourceT.TransformPoint
                (new Vector3(0f, source.Height, 0f));
            Vector3 targetMouth = targetT.position + Vector3.up * (target.Height + 0.15f);

            // Set positions in VFX Graph
            if (vfx.HasVector3("StartPos"))
                vfx.SetVector3("StartPos", sourceMouth);
            if (vfx.HasVector3("EndPos"))
                vfx.SetVector3("EndPos", targetMouth);

            // Bell-curve intensity: peaks at t=0.5
            float intensity = _config.flowIntensity * (1f - Mathf.Abs(t - 0.5f) * 2f);
            intensity *= _config.streamWidthMultiplier;
            if (vfx.HasFloat("FlowIntensity"))
                vfx.SetFloat("FlowIntensity", intensity);

            // Dynamic bounds based on source-target distance
            float distance = Vector3.Distance(sourceMouth, targetMouth);
            float boundsSize = _config.scaleBoundsWithDistance
                ? _config.boundsRadius + distance * _config.boundsScalePerUnit
                : _config.boundsRadius;

            Vector3 boundsCenter = (sourceMouth + targetMouth) * 0.5f;
            if (vfx.HasVector3("BoundsCenter"))
                vfx.SetVector3("BoundsCenter", boundsCenter);
            if (vfx.HasFloat("BoundsSize"))
                vfx.SetFloat("BoundsSize", boundsSize);

            // Distance-scaled particle count
            if (_config.useDistanceScaledParticles && vfx.HasInt("ParticleCount"))
            {
                int particleCount = Mathf.Max(
                    _config.particleCapacity,
                    Mathf.RoundToInt(distance * _config.particlesPerUnitDistance));
                vfx.SetInt("ParticleCount", particleCount);
            }

            // Throttled status reporting for debug overlay
            _frameCounter++;
            if (_frameCounter % 30 == 0)
            {
                int particleCount = vfx.aliveParticleCount;
                _eventAggregator?.Publish(
                    VFXStatusEvent.Active(vfx.GetEntityId(), intensity, particleCount));
            }
        }
    }
}
