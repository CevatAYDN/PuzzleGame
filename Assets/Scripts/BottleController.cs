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
        [SerializeField] private Color[] layerColors =
        {
            new Color(0.2f, 0.6f, 1.0f, 1f),
            new Color(0.1f, 0.5f, 0.3f, 1f),
            new Color(0.8f, 0.2f, 0.3f, 1f),
            new Color(0.9f, 0.7f, 0.1f, 1f)
        };

        [Tooltip("Fill levels for each layer (0-1). Each represents cumulative fill.")]
        [SerializeField] [Range(0f, 1f)] private float[] fillLevels =
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

        private const float Epsilon = 0.001f;

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
                float duration = Mathf.Max(0.01f, animationDuration);
                animationProgress += Time.deltaTime / duration;

                if (animationProgress >= 1f)
                {
                    animationProgress = 1f;
                    isAnimating = false;

                    for (int i = 0; i < currentFillLevels.Length; i++)
                    {
                        currentFillLevels[i] = targetFillLevels[i];
                    }

                    SyncSerializedFields();
                    UpdateLiquidShader();
                    onAnimationComplete?.Invoke();
                }
                else
                {
                    float t = EaseOutCubic(animationProgress);

                    if (startFillLevels == null || startFillLevels.Length == 0)
                    {
                        for (int i = 0; i < currentFillLevels.Length; i++)
                            currentFillLevels[i] = targetFillLevels[i];
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

                if (bottleRenderer.sharedMaterials != null && bottleRenderer.sharedMaterials.Length > 1)
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
            NormalizeSerializedState();

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

            CompactLayers();
            SyncSerializedFields();
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
            maxLayers = Mathf.Clamp(maxLayers, 1, 4);
            EnsureArraySize(ref layerColors, maxLayers);
            EnsureArraySize(ref fillLevels, maxLayers);
            EnsureArraySize(ref currentFillLevels, maxLayers);
            EnsureArraySize(ref targetFillLevels, maxLayers);
        }

        private void NormalizeSerializedState()
        {
            EnsureFillArraySizes();

            float prev = 0f;
            for (int i = 0; i < maxLayers; i++)
            {
                fillLevels[i] = Mathf.Clamp(fillLevels[i], prev, 1f);

                if (layerColors[i].a <= 0.01f)
                {
                    layerColors[i] = Color.clear;
                    fillLevels[i] = i > 0 ? fillLevels[i - 1] : 0f;
                }

                prev = fillLevels[i];
            }

            if (currentFillLevels != null)
            {
                Array.Copy(fillLevels, currentFillLevels, maxLayers);
            }

            if (targetFillLevels != null)
            {
                Array.Copy(fillLevels, targetFillLevels, maxLayers);
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Set the colors for liquid layers.
        /// </summary>
        public void SetLayerColors(Color[] colors)
        {
            EnsureFillArraySizes();

            if (colors == null || colors.Length == 0)
            {
                for (int i = 0; i < maxLayers; i++)
                    layerColors[i] = Color.clear;
                CompactLayers();
                UpdateGlassShader();
                UpdateLiquidShader();
                return;
            }

            int copyCount = Mathf.Min(colors.Length, maxLayers);
            Array.Copy(colors, layerColors, copyCount);
            for (int i = copyCount; i < maxLayers; i++)
                layerColors[i] = Color.clear;

            CompactLayers();
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

            targetFillLevels = SanitizeFillLevels(levels);

            this.animationDuration = animationDuration ?? fillTransitionDuration;
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

            float[] sanitized = SanitizeFillLevels(levels);
            Array.Copy(sanitized, currentFillLevels, maxLayers);
            Array.Copy(sanitized, targetFillLevels, maxLayers);

            SyncSerializedFields();
            UpdateLiquidShader();
        }

        private float[] SanitizeFillLevels(float[] levels)
        {
            float[] result = new float[maxLayers];
            float prev = 0f;

            for (int i = 0; i < maxLayers; i++)
            {
                float source = 0f;
                if (levels != null && i < levels.Length)
                    source = levels[i];
                else if (i > 0)
                    source = result[i - 1];

                result[i] = Mathf.Clamp(source, prev, 1f);
                prev = result[i];
            }

            return result;
        }

        /// <summary>
        /// Pour top contiguous color block from this bottle to target.
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
            int tgtTop = target.GetTopLayerIndex();
            if (tgtTop >= 0 && !ColorsMatch(target.layerColors[tgtTop], pourColor))
            {
                BottleLogger.LogDebug("Target already has different color, cannot pour");
                return false;
            }

            float contiguousHeight = GetTopContiguousHeight(srcTop, pourColor);
            if (contiguousHeight < Epsilon)
            {
                BottleLogger.LogDebug("No pourable liquid at source top");
                return false;
            }

            float tgtUsed = target.GetUsedFillAmount();
            float tgtSpace = 1.0f - tgtUsed;
            if (tgtSpace < 0.01f)
            {
                BottleLogger.LogDebug("Target bottle is full");
                return false;
            }

            float transfer = Mathf.Min(contiguousHeight, tgtSpace);
            if (transfer < Epsilon)
            {
                BottleLogger.LogDebug("Not enough space for transfer");
                return false;
            }

            BottleLogger.LogDebug($"Pouring from bottle {gameObject.name} to {target.gameObject.name}, amount: {transfer}");

            RemoveAmountFromTopColor(transfer, srcTop, pourColor);
            CompactLayers();

            target.AddAmountToTop(transfer, pourColor, tgtTop);
            target.CompactLayers();

            SyncSerializedFields();
            target.SyncSerializedFields();

            UpdateGlassShader();
            UpdateLiquidShader();
            target.UpdateGlassShader();
            target.UpdateLiquidShader();

            return true;
        }

        private void RemoveAmountFromTopColor(float amount, int srcTop, Color color)
        {
            float remaining = amount;

            for (int i = srcTop; i >= 0 && remaining > Epsilon; i--)
            {
                if (!ColorsMatch(layerColors[i], color))
                    break;

                float h = GetLayerHeight(i);
                if (h <= Epsilon) continue;

                float take = Mathf.Min(h, remaining);

                if (take >= h - Epsilon)
                {
                    layerColors[i] = Color.clear;
                    currentFillLevels[i] = i > 0 ? currentFillLevels[i - 1] : 0f;
                }
                else
                {
                    currentFillLevels[i] -= take;
                }

                remaining -= take;
            }
        }

        private void AddAmountToTop(float amount, Color color, int tgtTop)
        {
            if (tgtTop < 0)
            {
                layerColors[0] = color;
                float newUsed = Mathf.Clamp01(amount);
                for (int i = 0; i < currentFillLevels.Length; i++)
                    currentFillLevels[i] = newUsed;
                return;
            }

            for (int i = tgtTop; i < currentFillLevels.Length; i++)
                currentFillLevels[i] = Mathf.Clamp01(currentFillLevels[i] + amount);
        }

        private float GetTopContiguousHeight(int topIndex, Color topColor)
        {
            float amount = 0f;
            for (int i = topIndex; i >= 0; i--)
            {
                if (!ColorsMatch(layerColors[i], topColor))
                    break;

                float h = GetLayerHeight(i);
                if (h <= Epsilon) break;
                amount += h;
            }

            return amount;
        }

        private float GetLayerHeight(int index)
        {
            if (index < 0 || index >= currentFillLevels.Length) return 0f;
            float bottom = index > 0 ? currentFillLevels[index - 1] : 0f;
            return Mathf.Max(0f, currentFillLevels[index] - bottom);
        }

        private void CompactLayers()
        {
            EnsureFillArraySizes();

            int write = 0;
            float cumulative = 0f;

            for (int read = 0; read < maxLayers; read++)
            {
                float h = GetLayerHeight(read);
                if (h > Epsilon && layerColors[read].a > 0.01f)
                {
                    layerColors[write] = layerColors[read];
                    cumulative = Mathf.Clamp01(cumulative + h);
                    currentFillLevels[write] = cumulative;
                    write++;
                }
            }

            for (int i = write; i < maxLayers; i++)
            {
                layerColors[i] = Color.clear;
                currentFillLevels[i] = cumulative;
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

        public float GetUsedFillAmount()
        {
            if (currentFillLevels == null || currentFillLevels.Length == 0) return 0f;
            return currentFillLevels[currentFillLevels.Length - 1];
        }

        public bool HasSingleColorContent()
        {
            int top = GetTopLayerIndex();
            if (top < 0) return true;

            Color first = Color.clear;
            bool hasColor = false;

            for (int i = 0; i <= top; i++)
            {
                if (GetLayerHeight(i) <= Epsilon || layerColors[i].a <= 0.01f) continue;

                if (!hasColor)
                {
                    first = layerColors[i];
                    hasColor = true;
                }
                else if (!ColorsMatch(first, layerColors[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public bool IsFull()
        {
            return GetUsedFillAmount() >= 0.99f;
        }

        public bool IsEmpty()
        {
            return GetTopLayerIndex() < 0;
        }

        public int GetTopLayerIndex()
        {
            for (int i = currentFillLevels.Length - 1; i >= 0; i--)
            {
                if (GetLayerHeight(i) > Epsilon && layerColors[i].a > 0.01f) return i;
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
            else
            {
                glassBlock.SetColor(ColorID, new Color(1f, 1f, 1f, 0.18f));
            }

            bottleRenderer.SetPropertyBlock(glassBlock, 0);
        }

        private void UpdateLiquidShader()
        {
            EnsurePropertyBlocks();
            if (liquidBlock == null || bottleRenderer == null) return;

            Color[] adjusted = { Color.clear, Color.clear, Color.clear, Color.clear };

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
                        Mathf.Clamp01(adjustedColor.a)
                    );
                    adjustedColor = new Color(
                        Mathf.Clamp01(adjustedColor.r * 1.2f),
                        Mathf.Clamp01(adjustedColor.g * 1.2f),
                        Mathf.Clamp01(adjustedColor.b * 1.2f),
                        adjustedColor.a
                    );
                }

                adjusted[i] = adjustedColor;
            }

            liquidBlock.SetColor(Color1ID, adjusted[0]);
            liquidBlock.SetColor(Color2ID, adjusted[1]);
            liquidBlock.SetColor(Color3ID, adjusted[2]);
            liquidBlock.SetColor(Color4ID, adjusted[3]);

            float f1 = currentFillLevels != null && currentFillLevels.Length > 0 ? currentFillLevels[0] : 0f;
            float f2 = currentFillLevels != null && currentFillLevels.Length > 1 ? currentFillLevels[1] : f1;
            float f3 = currentFillLevels != null && currentFillLevels.Length > 2 ? currentFillLevels[2] : f2;
            float f4 = currentFillLevels != null && currentFillLevels.Length > 3 ? currentFillLevels[3] : f3;

            liquidBlock.SetFloat(Fill1ID, f1);
            liquidBlock.SetFloat(Fill2ID, f2);
            liquidBlock.SetFloat(Fill3ID, f3);
            liquidBlock.SetFloat(Fill4ID, f4);

            float topFill = GetUsedFillAmount();
            liquidBlock.SetFloat(SurfaceHeightID, topFill);
            liquidBlock.SetFloat(BottleHeightID, Mathf.Max(0.001f, bottleHeight));

            if (bottleRenderer.sharedMaterials != null && bottleRenderer.sharedMaterials.Length > 1)
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
            EnsurePropertyBlocks();
            EnsureFillArraySizes();
            NormalizeSerializedState();
            CompactLayers();
            SyncSerializedFields();

            if (bottleRenderer != null)
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
