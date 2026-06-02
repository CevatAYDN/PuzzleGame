#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace PuzzleGame.Editor
{
    /// <summary>
    /// Build-time shader variant stripper for mobile optimization.
    /// Removes GPU-inexpensive variants that will never be used:
    /// - Fog variants (we use screen-space fog post-process)
    /// - Lightmap variants (unlit shaders)
    /// - Stereo/VR variants (targeting mobile, not VR)
    /// - High LOD variants (we use LOD 100)
    ///
    /// This reduces build size and shader compilation time significantly on mobile.
    /// Registered automatically via InitializeOnLoad.
    /// </summary>
    [InitializeOnLoad]
    public class ShaderVariantStripper : IPreprocessShaders
    {
        static ShaderVariantStripper()
        {
            Debug.Log("[ShaderVariantStripper] Initialized build-time shader variant stripping.");
        }

        public int callbackOrder => 0;

        public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
        {
            // Only process PuzzleGame and Custom/ shaders
            if (!shader.name.StartsWith("PuzzleGame/") && !shader.name.StartsWith("Custom/"))
                return;

            int strippedCount = 0;
            for (int i = data.Count - 1; i >= 0; i--)
            {
                var d = data[i];
                bool shouldStrip = false;

                // Strip fog variants — we handle fog in post-process
                if (d.shaderKeywordSet.IsEnabled(new ShaderKeyword("FOG_LINEAR")) ||
                    d.shaderKeywordSet.IsEnabled(new ShaderKeyword("FOG_EXP")) ||
                    d.shaderKeywordSet.IsEnabled(new ShaderKeyword("FOG_EXP2")))
                    shouldStrip = true;

                // Strip lightmap variants — our mobile shaders are unlit
                if (d.shaderKeywordSet.IsEnabled(new ShaderKeyword("LIGHTMAP_ON")) ||
                    d.shaderKeywordSet.IsEnabled(new ShaderKeyword("DIRLIGHTMAP_COMBINED")) ||
                    d.shaderKeywordSet.IsEnabled(new ShaderKeyword("DIRLIGHTMAP_SEPARATE")) ||
                    d.shaderKeywordSet.IsEnabled(new ShaderKeyword("DYNAMICLIGHTMAP_ON")))
                    shouldStrip = true;

                // Strip stereo/VR variants
                if (d.shaderKeywordSet.IsEnabled(new ShaderKeyword("STEREO_INSTANCING_ON")) ||
                    d.shaderKeywordSet.IsEnabled(new ShaderKeyword("STEREO_MULTIVIEW_ON")))
                    shouldStrip = true;

                // Strip shadow collector variants
                if (d.shaderKeywordSet.IsEnabled(new ShaderKeyword("SHADOWS_SCREEN")) ||
                    d.shaderKeywordSet.IsEnabled(new ShaderKeyword("_MAIN_LIGHT_SHADOWS_CASCADE")))
                    shouldStrip = true;

                if (shouldStrip)
                {
                    data.RemoveAt(i);
                    strippedCount++;
                }
            }

            if (strippedCount > 0)
                Debug.Log($"[ShaderVariantStripper] {shader.name}: stripped {strippedCount}/{data.Count + strippedCount} variants");
        }
    }
}
#endif
