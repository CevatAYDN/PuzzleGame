using PuzzleGame.Application.Interfaces;
using UnityEngine;

namespace PuzzleGame.Application.Services
{
    /// <summary>
    /// Pour stream'i için LineRenderer yönetimi.
    /// Dış curve + iç straight line segmentlerini çizer.
    /// </summary>
    public static class StreamRenderer
    {
        public const int ExternalSegments = 12;
        public const int InternalSegments = 6;
        public const int TotalSegments = ExternalSegments + InternalSegments;

        public static LineRenderer EnsureLineRenderer(GameObject owner)
        {
            var lr = owner.GetComponent<LineRenderer>();
            if (lr == null)
            {
                lr = owner.AddComponent<LineRenderer>();
                lr.useWorldSpace = true;
                lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                lr.receiveShadows = false;
                var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
                if (shader != null) lr.material = new Material(shader);
            }
            return lr;
        }

        public static void SetColor(LineRenderer lr, Color color)
        {
            if (lr == null) return;
            lr.startColor = color;
            lr.endColor = color;
            if (lr.material != null)
            {
                lr.material.color = color;
                lr.material.SetColor("_BaseColor", color);
            }
        }

        public static void Update(LineRenderer lr, IBottleView source, IBottleView target,
                                  Transform sourceT, Transform targetT, float t, Configuration.AnimationConfig config)
        {
            if (lr == null) return;

            lr.positionCount = TotalSegments;

            // Bell curve stream width
            float scaleFactor = Mathf.SmoothStep(0f, 1f, t < 0.1f ? t / 0.1f : (t > 0.9f ? (1f - t) / 0.1f : 1f));
            float baseWidth = config != null ? config.streamWidth * scaleFactor : 0.08f * scaleFactor;
            lr.startWidth = baseWidth;
            lr.endWidth = baseWidth;

            Vector3 sourceMouth = sourceT.TransformPoint(new Vector3(0f, source.Height, 0f));
            Vector3 targetMouth = targetT.position + Vector3.up * target.Height;
            Vector3 landingPoint = targetT.position + Vector3.up * (target.Height * target.VisualTotalFill);

            // External curve (parabolic gravity drop)
            for (int i = 0; i < ExternalSegments; i++)
            {
                float segT = (float)i / (ExternalSegments - 1);
                float distH = Vector3.Distance(
                    new Vector3(sourceMouth.x, 0f, sourceMouth.z),
                    new Vector3(targetMouth.x, 0f, targetMouth.z));
                float gravityArc = Mathf.Sin(segT * Mathf.PI) * (0.05f + distH * 0.12f);
                Vector3 pos = Vector3.Lerp(sourceMouth, targetMouth, segT);
                pos.y -= gravityArc;
                lr.SetPosition(i, pos);
            }

            // Internal straight vertical line
            for (int i = 0; i < InternalSegments; i++)
            {
                float segT = (float)i / (InternalSegments - 1);
                Vector3 pos = Vector3.Lerp(targetMouth, landingPoint, segT);
                lr.SetPosition(ExternalSegments + i, pos);
            }
        }
    }
}
