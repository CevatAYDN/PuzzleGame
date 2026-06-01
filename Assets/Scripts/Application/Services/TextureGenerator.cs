using UnityEngine;

namespace PuzzleGame.Application.Services
{
    /// <summary>
    /// Statik texture oluşturucu — splash ve bubble partikülleri için daire/doku üretir.
    /// </summary>
    public static class TextureGenerator
    {
        private static Texture2D _solidCircleTex;
        private static Texture2D _bubbleTex;

        public static Texture2D GetSolidCircleTex()
        {
            if (_solidCircleTex == null)
                _solidCircleTex = CreateCircleTexture(true);
            return _solidCircleTex;
        }

        public static Texture2D GetBubbleTex()
        {
            if (_bubbleTex == null)
                _bubbleTex = CreateCircleTexture(false);
            return _bubbleTex;
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
