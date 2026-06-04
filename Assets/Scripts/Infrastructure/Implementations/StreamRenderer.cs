using PuzzleGame.Application.Interfaces;
using UnityEngine;
using UnityEngine.VFX;

namespace PuzzleGame.Infrastructure.Implementations
{
    /// <summary>
    /// VFX Graph manager for the Casting Ore stream.
    /// Converted from LineRenderer to VisualEffect for AAA Magma Flow.
    /// </summary>
    public class StreamRenderer : IStreamRenderer
    {
        public VisualEffect EnsureEffect(GameObject owner)
        {
            var vfx = owner.GetComponent<VisualEffect>();
            if (vfx == null)
            {
                vfx = owner.AddComponent<VisualEffect>();
                // Attempt to load the MagmaFlow VFX from Resources folder.
                // The user is responsible for creating Assets/Resources/MagmaFlow.vfx
                var asset = Resources.Load<VisualEffectAsset>("MagmaFlow");
                if (asset != null)
                {
                    vfx.visualEffectAsset = asset;
                }
                else
                {
                    Debug.LogWarning("[StreamRenderer] MagmaFlow.vfx not found in Resources folder. Please create it.");
                }
            }
            return vfx;
        }

        public void SetColor(VisualEffect vfx, Color color)
        {
            if (vfx == null) return;
            if (vfx.HasVector4("OreColor"))
            {
                // Multiply color intensity or adjust HDR if necessary here
                vfx.SetVector4("OreColor", color);
            }
        }

        public void Update(VisualEffect vfx, IMoldView source, IMoldView target,
                           Transform sourceT, Transform targetT, float t, PuzzleGame.Application.Configuration.AnimationConfig config)
        {
            if (vfx == null) return;

            // Calculate start and end positions
            Vector3 sourceMouth = sourceT.TransformPoint(new Vector3(0f, source.Height, 0f));
            Vector3 landingPoint = targetT.position + Vector3.up * (target.Height * target.VisualTotalFill);

            if (vfx.HasVector3("StartPos")) vfx.SetVector3("StartPos", sourceMouth);
            if (vfx.HasVector3("EndPos")) vfx.SetVector3("EndPos", landingPoint);

            // Flow intensity is a bell curve based on time t (smooth fade in/out)
            float scaleFactor = Mathf.SmoothStep(0f, 1f, t < 0.1f ? t / 0.1f : (t > 0.9f ? (1f - t) / 0.1f : 1f));
            float baseIntensity = config != null ? config.streamWidth * scaleFactor : 1.0f * scaleFactor;
            
            if (vfx.HasFloat("FlowIntensity")) vfx.SetFloat("FlowIntensity", baseIntensity);
        }
    }
}
