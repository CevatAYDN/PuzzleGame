using UnityEngine;
using System;
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
            new Color(0.2f, 0.6f, 1.0f),
            new Color(0.1f, 0.5f, 0.3f),
            new Color(0.8f, 0.2f, 0.3f),
            new Color(0.9f, 0.7f, 0.1f)
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
        private static readonly int ColorID = Shader.PropertyToID("_Color");

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            Initialize();
        }

        private void OnEnable()
        {
            EnsurePropertyBlocks();
        }

        private void Update()
        {
            if (isAnimating)
            {
                animationProgress += Time.deltaTime / animationDuration;

                if (animationProgress >= 1f)
                {
                    animationProgress = 1f;
                    isAnimating = false;

                    for (int i = 0; i < currentFillLevels.Length; i++)
                    {
                        currentFillLevels[i] = targetFillLevels[i];
                    }

                    UpdateLiquidShader();
                    onAnimationComplete?.Invoke();
                }
                else
                {
                    float t = EaseOutCubic(animationProgress);

                    if (startFillLevels == null || startFillLevels.Length == 0)
                    {
                        for (int i = 0; i < currentFillLevels.Length; i++)
                        {
                            currentFillLevels[i] = targetFillLevels[i];
                        }
                        UpdateLiquidShader();
                        return;
                    }

                    for (int i = 0; i < currentFillLevels.Length; i++)
                    {
                        float start = i < startFillLevels.Length ? startFillLevels[i] : 0f;
                        currentFillLevels[i] = Mathf.Lerp(start, targetFillLevels[i], t);
                    }

                    UpdateLiquidShader();
                }
            }

            if (liquidBlock != null && bottleRenderer != null)
            {
                liquidBlock.SetFloat(TimeXID, Time.time);
                liquidBlock.SetFloat(SurfaceRippleAmplitudeID, rippleAmplitude);
                liquidBlock.SetFloat(SurfaceRippleSpeedID, rippleSpeed);
                bottleRenderer.SetPropertyBlock(liquidBlock, 1);
            }
        }

        #endregion

        #region Initialization

        private void Initialize()
        {
            bottleRenderer = GetComponent<Renderer>();

            EnsurePropertyBlocks();
            EnsureFillArraySizes();

            if (bottleRenderer != null && bottleRenderer.sharedMaterial == null && glassMaterial != null)
            {
                bottleRenderer.sharedMaterial = glassMaterial;
            }

            if (currentFillLevels == null) currentFillLevels = new float[maxLayers];
            if (targetFillLevels == null) targetFillLevels = new float[maxLayers];

            for (int i = 0; i < maxLayers; i++)
            {
                currentFillLevels[i] = fillLevels[i];
                targetFillLevels[i] = fillLevels[i];
            }

            UpdateGlassShader();
            UpdateLiquidShader();
        }

        private void EnsurePropertyBlocks()
        {
            if (bottleRenderer == null)
                bottleRenderer = GetComponent<Renderer>();
            if (glassBlock == null)
                glassBlock = new MaterialPropertyBlock();
            if (liquidBlock == null)
                liquidBlock = new MaterialPropertyBlock();
        }

        private void EnsureArraySize<T>(ref T[] array, int targetSize)
        {
            if (array == null || array.Length != targetSize)
            {
                T[] newArray = new T[targetSize];
                if (array != null)
                    Array.Copy(array, newArray, Mathf.Min(array.Length, targetSize));
                array = newArray;
            }
        }

        private void EnsureFillArraySizes()
        {
            EnsureArraySize(ref layerColors, maxLayers);
            EnsureArraySize(ref fillLevels, maxLayers);
            EnsureArraySize(ref currentFillLevels, maxLayers);
            EnsureArraySize(ref targetFillLevels, maxLayers);
        }

        #endregion

        #region Public API

        /// <summary>
        /// Set the colors for liquid layers.
        /// </summary>
        public void SetLayerColors(Color[] colors)
        {
            EnsureFillArraySizes();
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
            EnsureFillArraySizes();

            startFillLevels = new float[maxLayers];
            Array.Copy(currentFillLevels, startFillLevels, Mathf.Min(currentFillLevels.Length, maxLayers));

            float[] resizedCurrent = new float[maxLayers];
            Array.Copy(startFillLevels, resizedCurrent, maxLayers);
            currentFillLevels = resizedCurrent;

            targetFillLevels = new float[maxLayers];
            int copyCount = Mathf.Min(levels.Length, maxLayers);
            Array.Copy(levels, targetFillLevels, copyCount);
            for (int i = copyCount; i < maxLayers; i++)
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
            EnsureFillArraySizes();

            int copyCount = Mathf.Min(levels.Length, maxLayers);
            Array.Copy(levels, currentFillLevels, copyCount);
            Array.Copy(levels, targetFillLevels, copyCount);
            for (int i = copyCount; i < maxLayers; i++)
            {
                float val = copyCount > 0 ? targetFillLevels[copyCount - 1] : 0f;
                currentFillLevels[i] = val;
                targetFillLevels[i] = val;
            }

            Array.Copy(currentFillLevels, fillLevels, maxLayers);
            UpdateLiquidShader();
        }

        /// <summary>
        /// Pour the topmost liquid layer from this bottle into target.
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
                return false;
            }

            Color pourColor = layerColors[srcTop];
            float layerHeight = GetLayerHeight(srcTop);
            if (layerHeight < 0.001f)
            {
                BottleLogger.LogDebug("Source layer height too small to pour");
                return false;
            }

            int tgtTop = target.GetTopLayerIndex();
            if (tgtTop >= 0 && !ColorsMatch(target.layerColors[tgtTop], pourColor))
            {
                BottleLogger.LogDebug("Target already has different color, cannot pour");
                return false;
            }

            float tgtUsed = target.currentFillLevels[target.currentFillLevels.Length - 1];
            float tgtSpace = 1.0f - tgtUsed;
            if (tgtSpace < 0.01f)
            {
                BottleLogger.LogDebug("Target bottle is full");
                return false;
            }

            float transfer = Mathf.Min(layerHeight, tgtSpace);
            if (transfer < 0.001f)
            {
                BottleLogger.LogDebug("Not enough space for transfer");
                return false;
            }

            BottleLogger.LogDebug($"Pouring from bottle {gameObject.name} to {target.gameObject.name}, amount: {transfer}");

            if (transfer >= layerHeight - 0.001f)
            {
                layerColors[srcTop] = Color.clear;
                currentFillLevels[srcTop] = srcTop > 0 ? currentFillLevels[srcTop - 1] : 0f;
            }
            else
            {
                currentFillLevels[srcTop] -= transfer;
            }
            CompactLayers();

            if (tgtTop < 0 || target.layerColors[tgtTop].a < 0.01f)
            {
                target.layerColors[0] = pourColor;
                for (int i = 0; i < target.currentFillLevels.Length; i++) { target.currentFillLevels[i] = transfer; }
            }
            else
            {
                for (int i = tgtTop; i < target.currentFillLevels.Length; i++) { target.currentFillLevels[i] += transfer; }
            }
            target.CompactLayers();

            SyncSerializedFields();
            target.SyncSerializedFields();

            UpdateLiquidShader();
            target.UpdateLiquidShader();

            return true;
        }

        private float GetLayerHeight(int index)
        {
            if (index < 0 || index >= currentFillLevels.Length) return 0f;
            float bottom = index > 0 ? currentFillLevels[index - 1] : 0f;
            return currentFillLevels[index] - bottom;
        }

        private void CompactLayers()
        {
            EnsureFillArraySizes();

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
            for (int i = write; i < maxLayers; i++)
            {
                layerColors[i] = Color.clear;
                currentFillLevels[i] = i > 0 ? currentFillLevels[i - 1] : 0f;
            }
        }

        private void SyncSerializedFields()
        {
            EnsureFillArraySizes();
            Array.Copy(currentFillLevels, fillLevels, maxLayers);
        }

        public bool CanAcceptLiquid(int layerIndex, float amount)
        {
            if (layerIndex < 0 || layerIndex >= currentFillLevels.Length) return false;
            return currentFillLevels[layerIndex] + amount <= 1.0f;
        }

        public Color GetLayerColor(int layerIndex)
        {
            if (layerIndex < 0 || layerIndex >= layerColors.Length) return Color.clear;
            return layerColors[layerIndex];
        }

        public float GetFillLevel(int layerIndex)
        {
            if (layerIndex < 0 || layerIndex >= currentFillLevels.Length) return 0f;
            return currentFillLevels[layerIndex];
        }

        public float[] GetAllFillLevels()
        {
            float[] result = new float[currentFillLevels.Length];
            Array.Copy(currentFillLevels, result, result.Length);
            return result;
        }

        public bool IsFull()
        {
            if (currentFillLevels.Length == 0) return false;
            return currentFillLevels[currentFillLevels.Length - 1] >= 0.99f;
        }

        public bool IsEmpty()
        {
            return GetTopLayerIndex() < 0;
        }

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

            if (layerColors.Length > 0 && layerColors[0].a > 0.01f)
            {
                glassBlock.SetColor(ColorID, new Color(
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

            for (int i = 0; i < layerColors.Length && i < 4; i++)
            {
                Color adjustedColor = layerColors[i];
                if (adjustedColor.a > 0.01f)
                {
                    float avg = (adjustedColor.r + adjustedColor.g + adjustedColor.b) / 3f;
                    adjustedColor = new Color(
                        Mathf.Clamp01(avg + (adjustedColor.r - avg) * 1.5f),
                        Mathf.Clamp01(avg + (adjustedColor.g - avg) * 1.5f),
                        Mathf.Clamp01(avg + (adjustedColor.b - avg) * 1.5f),
                        adjustedColor.a
                    );
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

            for (int i = 1; i < fillLevels.Length; i++)
            {
                if (fillLevels[i] < fillLevels[i - 1])
                {
                    fillLevels[i] = fillLevels[i - 1];
                }
            }

            EnsurePropertyBlocks();

            if (glassBlock != null && bottleRenderer != null)
            {
                UpdateGlassShader();
                UpdateLiquidShader();

                UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
                if (UnityEditor.SceneView.lastActiveSceneView != null)
                    UnityEditor.SceneView.lastActiveSceneView.Repaint();
            }
        }
#endif

        #endregion
    }
}