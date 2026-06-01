using UnityEngine;

namespace PuzzleGame.Application.Services
{
    /// <summary>
    /// Particle sistemleri için paylaşılan material'ları yönetir.
    /// Lazy-init + cache, çift yaratımı engeller.
    /// </summary>
    public static class ParticleMaterialFactory
    {
        private static Material _splashMaterial;
        private static Material _bubbleMaterial;

        public static Material GetSplashMaterial(Texture2D tex)
        {
            if (_splashMaterial == null)
            {
                _splashMaterial = CreateParticleMaterial(tex);
            }
            return _splashMaterial;
        }

        public static Material GetBubbleMaterial(Texture2D tex)
        {
            if (_bubbleMaterial == null)
            {
                _bubbleMaterial = CreateParticleMaterial(tex);
            }
            return _bubbleMaterial;
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
    }
}
