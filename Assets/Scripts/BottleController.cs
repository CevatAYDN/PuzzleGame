using UnityEngine;
using System;
using System.Collections.Generic;
using BottleShaders.Logging;

namespace BottleShaders
{
    /// <summary>
    /// Manages a single bottle's liquid layers, colors, and fill levels.
    /// Works with the LayeredLiquid shader to display up to 4 color layers.
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    [RequireComponent(typeof(Collider))]
    public class BottleController : MonoBehaviour
    {
        #region Inspector Properties

        [Header("Liquid Layers")]
        [Tooltip("Colors for each liquid layer (max 4). Empty entries are ignored.")]
        [SerializeField] private Color[] layerColors = new Color[]
        {
            new Color(0.2f, 0.6f, 1.0f),   // Blue
            new Color(0.1f, 0.5f, 0.3f),   // Green
            new Color(0.8f, 0.2f, 0.3f),   // Red
            new Color(0.9f, 0.7f, 0.1f)    // Yellow
        };

        [Tooltip("Fill levels for each layer (0-1). Each represents the cumulative fill.")]
        [SerializeField] [Range(0f, 1f)] private float[] fillLevels = new float[]
        {
            0.25f, 0.50f, 0.75f, 1.0f
        };

        [Header("Bottle Settings")]
        [Tooltip("Material that uses the BottleGlass shader")]
        public Material glassMaterial;

        [Tooltip("Material that uses the LayeredLiquid shader")]
        public Material liquidMaterial;

        [Tooltip("Height of the bottle mesh in object space (used by shader for fill normalization)")]
        [SerializeField] private float bottleHeight = 2.0f;

        [Header("Animation Settings")]
        [Tooltip("Speed of liquid surface ripple animation")]
        [SerializeField] [Range(0f, 5f)] private float rippleSpeed = 1.0f;

        [Tooltip("Amplitude of the liquid surface ripple")]
        [SerializeField] [Range(0f, 0.1f)] private float rippleAmplitude = 0.005f;

        [Tooltip("Duration of fill level transition animation in seconds")]
        [SerializeField] [Range(0.1f, 3f)] private float fillTransitionDuration = 0.5f;

        [Header("Interaction")]
        [Tooltip("Can this bottle be selected for pouring?")]
        [SerializeField] private bool isInteractive = true;

        [Tooltip("Maximum number of layers this bottle can hold")]
        [SerializeField] [Range(1, 4)] private int maxLayers = 4;

        #endregion

        #region Private Fields

        private Renderer bottleRenderer;
        private MaterialPropertyBlock glassBlock;
        private MaterialPropertyBlock liquidBlock;
        private float[] targetFillLevels;
        private float[] currentFillLevels;
        private float[] startFillLevels;
        private bool isAnimating = false;
        private float animationProgress = 0f;
        private float animationDuration = 0f;
        private Action onAnimationComplete;

        // Cached property IDs for performance
        private static readonly int Color1ID = Shader.PropertyToID("_Color1");
        private static readonly int Color2ID = Shader.PropertyToID("_Color2");
        private static readonly int Color3ID = Shader.PropertyToID("_Color3");
        private static readonly int Color4ID = Shader.PropertyToID("_Color4");
        private static readonly int Fill1ID = Shader.PropertyToID("_Fill1");
        private static readonly int Fill2ID = Shader.PropertyToID("_Fill2");
        private static readonly int Fill3ID = Shader.PropertyToID("_Fill3");
        private static readonly int Fill4ID = Shader.PropertyToID("_Fill4");
        private static readonly int TimeXID = Shader.PropertyToID("_TimeX");
        private static readonly int SurfaceRippleAmplitudeID = Shader.PropertyToID("_SurfaceRippleAmplitude");
        private static readonly int SurfaceRippleSpeedID = Shader.PropertyToID("_SurfaceRippleSpeed");
        private static readonly int SurfaceHeightID = Shader.PropertyToID("_SurfaceHeight");
        private static readonly int BottleHeightID = Shader.PropertyToID("_BottleHeight");

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            Initialize();
        }

        private void OnEnable()
        {
            // In Editor, OnEnable ensures property blocks exist without Awake
            EnsurePropertyBlocks();
        }

        private void Update()
        {
            // Animate fill level transitions
            if (isAnimating)
            {
                animationProgress += Time.deltaTime / animationDuration;

                if (animationProgress >= 1f)
                {
                    animationProgress = 1f;
                    isAnimating = false;

                    // Snap to target values
                    for (int i = 0; i < currentFillLevels.Length; i++)
                    {
                        currentFillLevels[i] = targetFillLevels[i];
                    }

                    // Update shader with final values
                    UpdateLiquidShader();

                    onAnimationComplete?.Invoke();
                }
                else
                {
                    // Smooth interpolation from snapshotted start toward target
                    float t = EaseOutCubic(animationProgress);

                    for (int i = 0; i < currentFillLevels.Length; i++)
                    {
                        float start = i < startFillLevels.Length ? startFillLevels[i] : 0f;
                        currentFillLevels[i] = Mathf.Lerp(start, targetFillLevels[i], t);
                    }

                    UpdateLiquidShader();
                }
            }
            
            // Update ripple time in shader and re-apply so the surface animates
            // even when fill levels are static. Pass raw time; the shader applies
            // _SurfaceRippleSpeed itself (don't pre-multiply or speed squares).
            if (liquidBlock != null && bottleRenderer != null)
            {
                // Only update ripple effect when needed to improve performance
                float currentTime = Time.time;
                liquidBlock.SetFloat(TimeXID, currentTime);
                liquidBlock.SetFloat(SurfaceRippleAmplitudeID, rippleAmplitude);
                liquidBlock.SetFloat(SurfaceRippleSpeedID, rippleSpeed);

                // Always try to apply the liquid block. Unity silently ignores
                // SetPropertyBlock when materialIndex >= materialCount, so the
                // block will start rendering automatically once BottleMeshGenerator
                // sets up the second material slot.
                bottleRenderer.SetPropertyBlock(liquidBlock, 1);
            }
        }

        #endregion

        #region Initialization

        private void Initialize()
        {
            bottleRenderer = GetComponent<Renderer>();

            EnsurePropertyBlocks();

            // Don't override materials if already set (e.g., by BottleMeshGenerator)
            if (bottleRenderer != null && bottleRenderer.sharedMaterial == null && glassMaterial != null)
            {
                bottleRenderer.sharedMaterial = glassMaterial;
            }

            // Ensure serialized arrays are properly sized to maxLayers before initializing
            if (layerColors.Length != maxLayers)
            {
                Color[] newColors = new Color[maxLayers];
                Array.Copy(layerColors, newColors, Mathf.Min(layerColors.Length, maxLayers));
                layerColors = newColors;
            }
            if (fillLevels.Length != maxLayers)
            {
                float[] newFills = new float[maxLayers];
                Array.Copy(fillLevels, newFills, Mathf.Min(fillLevels.Length, maxLayers));
                fillLevels = newFills;
            }

            // Initialize fill level arrays (use full maxLayers size)
            currentFillLevels = new float[maxLayers];
            targetFillLevels = new float[maxLayers];

            for (int i = 0; i < maxLayers; i++)
            {
                currentFillLevels[i] = fillLevels[i];
                targetFillLevels[i] = fillLevels[i];
            }

            // Apply initial shader values
            UpdateGlassShader();
            UpdateLiquidShader();
        }

        /// <summary>
        /// Create property blocks on demand so Edit-mode code paths
        /// (OnValidate, scene creator calls) work without Awake.
        /// Safe to call multiple times.
        /// </summary>
        private void EnsurePropertyBlocks()
        {
            if (bottleRenderer == null)
                bottleRenderer = GetComponent<Renderer>();
            if (glassBlock == null)
                glassBlock = new MaterialPropertyBlock();
            if (liquidBlock == null)
                liquidBlock = new MaterialPropertyBlock();
        }

        #endregion

        #region Public API

        /// <summary>
        /// Set the colors for liquid layers.
        /// </summary>
        /// <param name="colors">Array of colors for each layer</param>
        public void SetLayerColors(Color[] colors)
        {
            if (layerColors.Length != maxLayers)
                layerColors = new Color[maxLayers];
            int copyCount = Mathf.Min(colors.Length, maxLayers);
            Array.Copy(colors, layerColors, copyCount);
            for (int i = copyCount; i < maxLayers; i++)
                layerColors[i] = Color.clear;
            UpdateGlassShader();
            UpdateLiquidShader();
        }

        /// <summary>
        /// Set fill levels for each layer (cumulative, 0-1).
        /// Animate to new values over time.
        /// </summary>
        public void SetFillLevels(float[] levels, float? animationDuration = null, Action onComplete = null)
        {
            int layerCount = maxLayers; // Always use maxLayers size for consistency

            // Snapshot current state as animation start (length must match target)
            startFillLevels = new float[layerCount];
            Array.Copy(currentFillLevels, startFillLevels, Mathf.Min(currentFillLevels.Length, layerCount));

            // Resize currentFillLevels to match so per-index loops stay in bounds
            float[] resizedCurrent = new float[layerCount];
            Array.Copy(startFillLevels, resizedCurrent, layerCount);
            currentFillLevels = resizedCurrent;

            targetFillLevels = new float[layerCount];
            int copyCount = Mathf.Min(levels.Length, layerCount);
            Array.Copy(levels, targetFillLevels, copyCount);
            // Fill remaining with last value or 0
            for (int i = copyCount; i < layerCount; i++)
                targetFillLevels[i] = copyCount > 0 ? targetFillLevels[copyCount - 1] : 0f;

            this.animationDuration = animationDuration ?? this.fillTransitionDuration;
            this.animationProgress = 0f;
            this.isAnimating = true;
            this.onAnimationComplete = onComplete;
        }

        /// <summary>
        /// Instantly set fill levels without animation.
        /// </summary>
        public void SetFillLevelsInstant(float[] levels)
        {
            EnsurePropertyBlocks();

            int layerCount = maxLayers; // Always use maxLayers size for consistency

            // Update runtime arrays
            currentFillLevels = new float[layerCount];
            targetFillLevels = new float[layerCount];
            int copyCount = Mathf.Min(levels.Length, layerCount);
            Array.Copy(levels, currentFillLevels, copyCount);
            Array.Copy(levels, targetFillLevels, copyCount);
            // Fill remaining with last value or 0
            for (int i = copyCount; i < layerCount; i++)
            {
                float val = copyCount > 0 ? targetFillLevels[copyCount - 1] : 0f;
                currentFillLevels[i] = val;
                targetFillLevels[i] = val;
            }

            // Also write serialized fillLevels so values survive into Play mode
            // (Awake → Initialize reads from serialized fillLevels)
            if (fillLevels == null || fillLevels.Length != layerCount)
                fillLevels = new float[layerCount];
            Array.Copy(currentFillLevels, fillLevels, layerCount);

            UpdateLiquidShader();
        }

        /// <summary>
        /// Pour the topmost liquid layer from this bottle into target.
        /// Water-sort rules: only same color pours, pours entire top layer,
        /// fills from bottom up in target.
        /// Returns true if liquid was transferred.
        /// </summary>
        public bool TryPourTo(BottleController target)
        {
            if (!isInteractive || target == null || target == this) 
            {
                if (!isInteractive) BottleLogger.LogDebug("Source bottle is not interactive");
                if (target == null) BottleLogger.LogDebug("Target bottle is null");
                if (target == this) BottleLogger.LogDebug("Cannot pour to self");
                return false;
            }

            int srcTop = GetTopLayerIndex();
            if (srcTop < 0) 
            {
                BottleLogger.LogDebug("Source bottle is empty");
                return false; // source empty
            }

            Color pourColor = layerColors[srcTop];
            float layerHeight = GetLayerHeight(srcTop);
            if (layerHeight < 0.001f) 
            {
                BottleLogger.LogDebug("Source layer height too small to pour");
                return false;
            }

            // Check target compatibility
            int tgtTop = target.GetTopLayerIndex();
            if (tgtTop >= 0 && !ColorsMatch(target.layerColors[tgtTop], pourColor))
            {
                BottleLogger.LogDebug("Target already has different color, cannot pour");
                return false; // color mismatch — can't pour different colors
            }

            // Calculate space in target
            float tgtUsed = target.currentFillLevels[target.currentFillLevels.Length - 1];
            float tgtSpace = 1.0f - tgtUsed;
            if (tgtSpace < 0.01f) 
            {
                BottleLogger.LogDebug("Target bottle is full");
                return false; // target full
            }

            float transfer = Mathf.Min(layerHeight, tgtSpace);
            if (transfer < 0.001f) 
            {
                BottleLogger.LogDebug("Not enough space for transfer");
                return false;
            }

            BottleLogger.LogDebug($"Pouring from bottle {gameObject.name} to {target.gameObject.name}, amount: {transfer}");

            // ── Update source ──
            if (transfer >= layerHeight - 0.001f)
            {
                // Remove entire top layer
                layerColors[srcTop] = Color.clear;
                currentFillLevels[srcTop] = srcTop > 0 ? currentFillLevels[srcTop - 1] : 0f;
            }
            else
            {
                currentFillLevels[srcTop] -= transfer;
            }
            CompactLayers();

            // ── Update target ──
            if (tgtTop < 0 || target.layerColors[tgtTop].a < 0.01f)
            {
                // Target empty — pour into first slot
                target.layerColors[0] = pourColor;
                for (int i = 0; i < target.currentFillLevels.Length; i++) { target.currentFillLevels[i] = transfer; }
            }
            else
            {
                // Merge into existing matching layer (checked above)
                for (int i = tgtTop; i < target.currentFillLevels.Length; i++) { target.currentFillLevels[i] += transfer; }
            }
            target.CompactLayers();

            // Sync serialized fields so values survive re-entering Play mode
            SyncSerializedFields();
            target.SyncSerializedFields();

            // Apply to shaders
            UpdateLiquidShader();
            target.UpdateLiquidShader();

            return true;
        }

        /// <summary>
        /// Get the individual height of a specific layer (not cumulative).
        /// </summary>
        private float GetLayerHeight(int index)
        {
            if (index < 0 || index >= currentFillLevels.Length) return 0f;
            float bottom = index > 0 ? currentFillLevels[index - 1] : 0f;
            return currentFillLevels[index] - bottom;
        }

        /// <summary>
        /// Compact layers by removing empty/gap layers, shifting everything down.
        /// </summary>
        private void CompactLayers()
        {
            // Ensure arrays are sized to maxLayers to avoid out-of-bounds
            if (layerColors.Length != maxLayers)
            {
                Color[] newColors = new Color[maxLayers];
                Array.Copy(layerColors, newColors, Mathf.Min(layerColors.Length, maxLayers));
                layerColors = newColors;
            }
            if (currentFillLevels.Length != maxLayers)
            {
                float[] newFills = new float[maxLayers];
                Array.Copy(currentFillLevels, newFills, Mathf.Min(currentFillLevels.Length, maxLayers));
                currentFillLevels = newFills;
            }

            int write = 0;
            for (int read = 0; read < maxLayers; read++)
            {
                float h = GetLayerHeight(read);
                if (h > 0.001f && layerColors[read].a > 0.01f)
                {
                    layerColors[write] = layerColors[read];
                    currentFillLevels[write] = (write > 0 ? currentFillLevels[write - 1] : 0f) + h;
                    write++;
                }
            }
            // Fill remaining slots as empty
            for (int i = write; i < maxLayers; i++)
            {
                layerColors[i] = Color.clear;
                currentFillLevels[i] = i > 0 ? currentFillLevels[i - 1] : 0f;
            }
        }

        /// <summary>
        /// Copy runtime fill and color data to serialized fields so changes survive
        /// scene save / Play mode transitions.
        /// </summary>
        private void SyncSerializedFields()
        {
            // Ensure serialized arrays are correctly sized
            if (fillLevels == null || fillLevels.Length != maxLayers)
            {
                float[] newFillLevels = new float[maxLayers];
                if (fillLevels != null)
                    Array.Copy(fillLevels, newFillLevels, Mathf.Min(fillLevels.Length, maxLayers));
                fillLevels = newFillLevels;
            }
            if (layerColors == null || layerColors.Length != maxLayers)
            {
                Color[] newLayerColors = new Color[maxLayers];
                if (layerColors != null)
                    Array.Copy(layerColors, newLayerColors, Mathf.Min(layerColors.Length, maxLayers));
                layerColors = newLayerColors;
            }
            
            // Copy current runtime data to serialized fields
            Array.Copy(currentFillLevels, fillLevels, maxLayers);
            // layerColors is already the runtime array being used, so no need to copy to itself
        }


        /// <summary>
        /// Check if a specific layer can accept more liquid.
        /// </summary>
        public bool CanAcceptLiquid(int layerIndex, float amount)
        {
            if (layerIndex < 0 || layerIndex >= currentFillLevels.Length) return false;
            return currentFillLevels[layerIndex] + amount <= 1.0f;
        }

        /// <summary>
        /// Get the color of a specific layer.
        /// </summary>
        public Color GetLayerColor(int layerIndex)
        {
            if (layerIndex < 0 || layerIndex >= layerColors.Length) return Color.clear;
            return layerColors[layerIndex];
        }

        /// <summary>
        /// Get the current fill level of a specific layer.
        /// </summary>
        public float GetFillLevel(int layerIndex)
        {
            if (layerIndex < 0 || layerIndex >= currentFillLevels.Length) return 0f;
            return currentFillLevels[layerIndex];
        }

        /// <summary>
        /// Get all current fill levels.
        /// </summary>
        public float[] GetAllFillLevels()
        {
            float[] result = new float[currentFillLevels.Length];
            Array.Copy(currentFillLevels, result, result.Length);
            return result;
        }

        /// <summary>
        /// Check if this bottle is completely full.
        /// </summary>
        public bool IsFull()
        {
            if (currentFillLevels.Length == 0) return false;
            return currentFillLevels[currentFillLevels.Length - 1] >= 0.99f;
        }

        /// <summary>
        /// Check if this bottle is empty.
        /// </summary>
        public bool IsEmpty()
        {
            return GetTopLayerIndex() < 0;
        }

        /// <summary>
        /// Get the topmost non-empty layer index.
        /// </summary>
        public int GetTopLayerIndex()
        {
            for (int i = currentFillLevels.Length - 1; i >= 0; i--)
            {
                if (GetLayerHeight(i) > 0.001f && layerColors[i].a > 0.01f) return i;
            }
            return -1;
        }

        private bool ColorsMatch(Color a, Color b)
        {
            return Mathf.Abs(a.r - b.r) < 0.05f &&
                   Mathf.Abs(a.g - b.g) < 0.05f &&
                   Mathf.Abs(a.b - b.b) < 0.05f;
        }

        #endregion

        #region Private Methods

        private void UpdateGlassShader()
        {
            EnsurePropertyBlocks();
            if (glassBlock == null || bottleRenderer == null) return;

            // Update glass material properties if needed
            // This could include glass tint based on liquid colors
            if (layerColors.Length > 0 && layerColors[0] != Color.clear)
            {
                // Subtle glass tint based on first liquid color
                glassBlock.SetColor("_Color", new Color(
                    layerColors[0].r * 0.15f + 0.85f,
                    layerColors[0].g * 0.15f + 0.85f,
                    layerColors[0].b * 0.15f + 0.85f,
                    0.25f
                ));
            }

            bottleRenderer.SetPropertyBlock(glassBlock);
        }

        private void UpdateLiquidShader()
        {
            EnsurePropertyBlocks();
            if (liquidBlock == null || bottleRenderer == null) return;

            // Set layer colors with improved visual appearance
            for (int i = 0; i < layerColors.Length && i < 4; i++)
            {
                // Enhance color saturation and brightness for more vibrant look
                Color adjustedColor = layerColors[i];
                if (adjustedColor.a > 0.01f) // Only adjust if not transparent
                {
                    // Increase saturation and brightness
                    float avg = (adjustedColor.r + adjustedColor.g + adjustedColor.b) / 3f;
                    adjustedColor = new Color(
                        Mathf.Clamp01(avg + (adjustedColor.r - avg) * 1.5f), // Boost saturation
                        Mathf.Clamp01(avg + (adjustedColor.g - avg) * 1.5f), // Boost saturation
                        Mathf.Clamp01(avg + (adjustedColor.b - avg) * 1.5f), // Boost saturation
                        adjustedColor.a
                    );
                    // Then brighten slightly
                    adjustedColor = new Color(
                        Mathf.Clamp01(adjustedColor.r * 1.2f),
                        Mathf.Clamp01(adjustedColor.g * 1.2f),
                        Mathf.Clamp01(adjustedColor.b * 1.2f),
                        adjustedColor.a
                    );
                }
                
                switch (i)
                {
                    case 0: liquidBlock.SetColor(Color1ID, adjustedColor); break;
                    case 1: liquidBlock.SetColor(Color2ID, adjustedColor); break;
                    case 2: liquidBlock.SetColor(Color3ID, adjustedColor); break;
                    case 3: liquidBlock.SetColor(Color4ID, adjustedColor); break;
                }
            }

            // Set fill levels (guard against OnValidate calling before Initialize)
            if (currentFillLevels != null)
            {
                for (int i = 0; i < currentFillLevels.Length && i < 4; i++)
                {
                    switch (i)
                    {
                        case 0: liquidBlock.SetFloat(Fill1ID, currentFillLevels[i]); break;
                        case 1: liquidBlock.SetFloat(Fill2ID, currentFillLevels[i]); break;
                        case 2: liquidBlock.SetFloat(Fill3ID, currentFillLevels[i]); break;
                        case 3: liquidBlock.SetFloat(Fill4ID, currentFillLevels[i]); break;
                    }
                }
            }

            // Set surface height to max fill level so liquid is visible
            float topFill = 0f;
            if (currentFillLevels != null && currentFillLevels.Length > 0)
            {
                for (int i = 0; i < currentFillLevels.Length; i++)
                {
                    if (currentFillLevels[i] > topFill) topFill = currentFillLevels[i];
                }
            }
            liquidBlock.SetFloat(SurfaceHeightID, topFill);
            liquidBlock.SetFloat(BottleHeightID, bottleHeight);

            // Apply to second material slot (assumed to be liquid)
            if (bottleRenderer.sharedMaterials.Length > 1)
            {
                bottleRenderer.SetPropertyBlock(liquidBlock, 1);
            }
        }

        private float EaseOutCubic(float t)
        {
            return 1f - Mathf.Pow(1f - t, 3f);
        }

        #endregion

        #region Editor Helpers

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Ensure arrays are properly sized
            if (layerColors.Length > maxLayers)
            {
                Color[] resized = new Color[maxLayers];
                Array.Copy(layerColors, resized, maxLayers);
                layerColors = resized;
            }

            if (fillLevels.Length > maxLayers)
            {
                float[] resized = new float[maxLayers];
                Array.Copy(fillLevels, resized, maxLayers);
                fillLevels = resized;
            }

            // Ensure fill levels are in ascending order
            for (int i = 1; i < fillLevels.Length; i++)
            {
                if (fillLevels[i] < fillLevels[i - 1])
                {
                    fillLevels[i] = fillLevels[i - 1];
                }
            }

            // Apply changes to shader in both Edit and Play modes.
            // In Edit mode, ensure blocks exist first (Awake doesn't run).
            EnsurePropertyBlocks();

            if (glassBlock != null && bottleRenderer != null)
            {
                UpdateGlassShader();
                UpdateLiquidShader();

                // Force the Scene/Game view to repaint so changes appear instantly
                UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
                if (UnityEditor.SceneView.lastActiveSceneView != null)
                    UnityEditor.SceneView.lastActiveSceneView.Repaint();
            }
        }
#endif

        #endregion
    }
}