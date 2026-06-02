using System;
using System.Collections.Generic;
using PuzzleGame.Domain;
using PuzzleGame.Domain.Models;
using PuzzleGame.Domain.Interfaces;
using PuzzleGame.Infrastructure.Interfaces;
using PuzzleGame.Logging;
using PuzzleGame.Application.Interfaces;
using UnityEngine;

namespace PuzzleGame
{
    [RequireComponent(typeof(Renderer))]
    [RequireComponent(typeof(Wobble))]
    public class BottleController : MonoBehaviour, IBottleView
    {
        [Header("Materials (assigned by BottleMeshGenerator or Editor tool)")]
        public Material glassMaterial;
        public Material liquidMaterial;

        [Header("Configuration")]
        public Configuration.BottleVisualConfig visualConfig;

        public GameObject corkObject;

        public BottleState State { get; private set; }
        public IReadOnlyList<LiquidLayer> VisualLayers => _visualLayers;
        public float VisualTotalFill => _visualTotalFill;
        public float Height => _meshGenerator != null ? _meshGenerator.height : BottleConstants.DefaultBottleHeight;
        public bool IsCapped { get; private set; }
        public Transform Transform => transform;
        public GameObject GameObject => gameObject;

        private readonly List<LiquidLayer> _visualLayers = new List<LiquidLayer>();
        private float _visualTotalFill = 0f;

        private IRendererService  _rendererService;
        private IBottleValidator  _validator;
        private IAnimationService _animationService;
        private Renderer          _renderer;
        private Wobble            _wobble;
        private BottleMeshGenerator _meshGenerator;
        private MaterialPropertyBlock _propBlock;
        private bool _isHighlighted;

        private static readonly int FresnelIntensityID = Shader.PropertyToID("_FresnelIntensity");

        public void Initialize(IRendererService rendererService,
                               IBottleValidator  validator,
                               IAnimationService animationService,
                               List<LiquidLayer> initialLayers)
        {
            if (rendererService == null) throw new ArgumentNullException(nameof(rendererService));
            if (validator == null)       throw new ArgumentNullException(nameof(validator));
            if (animationService == null) throw new ArgumentNullException(nameof(animationService));
            if (initialLayers == null)    throw new ArgumentNullException(nameof(initialLayers));

            _rendererService = rendererService;
            _validator       = validator;
            _animationService = animationService;
            _renderer        = GetComponent<Renderer>();
            _wobble          = GetComponent<Wobble>();
            _meshGenerator   = GetComponent<BottleMeshGenerator>();

            if (corkObject == null)
            {
                var child = transform.Find("Cork");
                if (child != null) corkObject = child.gameObject;
                else corkObject = CreateProceduralCork();
            }

            if (corkObject != null)
            {
                corkObject.SetActive(false);
            }
            IsCapped = false;
            _isHighlighted = false;
            SetSelectionHighlight(false);

            int maxLayers = visualConfig != null ? visualConfig.maxLayers : BottleConstants.DefaultLayerCapacity;
            State = new BottleState(maxLayers);
            _visualLayers.Clear();
            foreach (var layer in initialLayers)
            {
                State.AddLayer(layer);
                _visualLayers.Add(layer);
            }
            _visualTotalFill = State.TotalFill;

            BottleLogger.LogDebug($"Bottle '{name}' initialized with {initialLayers.Count} layers.");
            UpdateVisuals();
        }

        public bool IsEmpty => State?.IsEmpty ?? true;
        public bool IsFull()  => State?.IsFull  ?? false;

        public bool HasSingleColorContent()
        {
            if (State == null || State.IsEmpty || State.Layers.Count == 0) return true;
            var firstColor = State.Layers[0].Color;
            for (int i = 1; i < State.Layers.Count; i++)
                if (!_validator.ColorsMatch(State.Layers[i].Color, firstColor)) return false;
            return true;
        }

        public void AddWobbleImpulse(Vector3 direction, float strength)
        {
            _wobble?.AddImpulse(direction, strength);
        }

        public bool TryPourTo(IBottleView target)
        {
            if (target == null)
            {
                BottleLogger.LogWarning($"'{name}': TryPourTo called with null target.");
                return false;
            }

            if (!_validator.CanPour(State, target.State))
            {
                BottleLogger.LogDebug($"'{name}' → '{target.GameObject.name}': pour rejected by validator.");
                return false;
            }

            LiquidLayer layer;
            try
            {
                layer = State.PopTopLayer();
            }
            catch (InvalidOperationException ex)
            {
                // Validator said CanPour=true but the bottle is empty — invariant violation.
                throw new InvalidOperationException(
                    $"Bottle '{name}' invariant violated: validator approved pour but bottle is empty.", ex);
            }

            try
            {
                target.State.AddLayer(layer);
            }
            catch (InvalidOperationException ex)
            {
                State.AddLayer(layer);
                throw new InvalidOperationException(
                    $"Bottle '{name}' → '{target.GameObject.name}': AddLayer threw after validator approval. Rolled back.",
                    ex);
            }

            float impulse = visualConfig != null
                ? visualConfig.pourImpulseStrength
                : BottleConstants.DefaultPourImpulseStrength;
            Vector3 pourDirection = (target.Transform.position - transform.position).normalized;
            _wobble?.AddImpulse(-pourDirection, impulse);
            target.AddWobbleImpulse(pourDirection, impulse * BottleConstants.WobbleTargetMultiplier);

            BottleLogger.LogInfo($"Poured {layer.Color} from '{name}' to '{target.GameObject.name}'.");
            return true;
        }

        public void SetVisualState(List<LiquidLayer> layers, float totalFill)
        {
            _visualLayers.Clear();
            _visualLayers.AddRange(layers);
            _visualTotalFill = totalFill;
            UpdateVisuals();
        }

        public void SetVisualPourProgress(LayerSnapshot startLayers, float t, bool isSource, LiquidLayer pouredLayer)
        {
            _visualLayers.Clear();
            float totalFill = 0f;
            if (isSource)
            {
                int count = startLayers.Count;
                for (int i = 0; i < count; i++)
                {
                    var layer = startLayers.Get(i);
                    if (i == count - 1)
                    {
                        layer = layer.WithAmount(layer.Amount * (1f - t));
                    }
                    if (layer.Amount > BottleConstants.LayerAmountEpsilon)
                    {
                        _visualLayers.Add(layer);
                        totalFill += layer.Amount;
                    }
                }
            }
            else
            {
                int count = startLayers.Count;
                for (int i = 0; i < count; i++)
                {
                    var layer = startLayers.Get(i);
                    _visualLayers.Add(layer);
                    totalFill += layer.Amount;
                }
                var extra = pouredLayer.WithAmount(pouredLayer.Amount * t);
                if (extra.Amount > BottleConstants.LayerAmountEpsilon)
                {
                    _visualLayers.Add(extra);
                    totalFill += extra.Amount;
                }
            }
            _visualTotalFill = totalFill;
            UpdateVisuals();
        }

        public void UpdateVisualsFromState()
        {
            if (State == null) return;
            _visualLayers.Clear();
            _visualLayers.AddRange(State.Layers);
            _visualTotalFill = State.TotalFill;
            UpdateVisuals();
        }

        public void UpdateVisuals()
        {
            if (_rendererService == null)
            {
                BottleLogger.LogWarning($"'{name}': UpdateVisuals called before Initialize.");
                return;
            }

            if (_renderer == null)
            {
                BottleLogger.LogWarning($"'{name}': Renderer is null, skipping visual update.");
                return;
            }

            float sat = visualConfig != null ? visualConfig.saturationBoost : BottleConstants.DefaultSaturationBoost;
            float bri = visualConfig != null ? visualConfig.brightnessBoost : BottleConstants.DefaultBrightnessBoost;
            int liquidIndex = visualConfig != null ? visualConfig.liquidMaterialIndex : BottleConstants.DefaultLiquidMaterialIndex;
            _rendererService.UpdateLiquid(_renderer, _visualLayers, _visualTotalFill, sat, bri, liquidIndex);

            bool isEmpty = _visualLayers.Count == 0 || _visualTotalFill <= BottleConstants.LayerAmountEpsilon;
            DomainColor baseColor = _visualLayers.Count > 0 ? _visualLayers[0].Color : new DomainColor(0, 0, 0, 0);
            int glassIndex = visualConfig != null ? visualConfig.glassMaterialIndex : BottleConstants.DefaultGlassMaterialIndex;
            _rendererService.UpdateGlass(_renderer, isEmpty, baseColor, glassIndex);
        }

        public void SetSelectionHighlight(bool active)
        {
            if (_renderer == null) return;
            if (_isHighlighted == active) return;
            _isHighlighted = active;
            if (_propBlock == null) _propBlock = new MaterialPropertyBlock();
            int glassIndex = visualConfig != null ? visualConfig.glassMaterialIndex : BottleConstants.DefaultGlassMaterialIndex;
            _renderer.GetPropertyBlock(_propBlock, glassIndex);
            _propBlock.SetFloat(FresnelIntensityID,
                active ? BottleConstants.HighlightActiveFresnel : BottleConstants.HighlightInactiveFresnel);
            _renderer.SetPropertyBlock(_propBlock, glassIndex);
        }

        public void AnimateCompletion()
        {
            if (IsCapped) return;
            IsCapped = true;

            if (corkObject != null)
            {
                corkObject.SetActive(true);
                _animationService?.AnimateCorkDrop(
                    corkObject.transform, Height, onComplete: null);
            }

            int liquidIndex = visualConfig != null ? visualConfig.liquidMaterialIndex : BottleConstants.DefaultLiquidMaterialIndex;
            if (_renderer != null && _renderer.sharedMaterials.Length > liquidIndex)
            {
                _animationService?.AnimateLiquidFlash(
                    _renderer, liquidIndex,
                    BottleConstants.CompletionFlashIntensity,
                    BottleConstants.CompletionFlashDuration,
                    onComplete: null);
            }
        }

        public void PlaySettleBounce()
        {
            _animationService?.AnimateSettleBounce(this, BottleConstants.SettleBounceDuration, onComplete: null);
        }

        private GameObject CreateProceduralCork()
        {
            var cork = new GameObject("Cork");
            cork.transform.SetParent(transform, false);
            cork.transform.localPosition = new Vector3(0f, Height - BottleConstants.CorkYOffset, 0f);
            cork.transform.localRotation = Quaternion.identity;
            cork.transform.localScale = Vector3.zero;

            var filter = cork.AddComponent<MeshFilter>();
            var corkRenderer = cork.AddComponent<MeshRenderer>();

            var mesh = new Mesh { name = "CorkMesh" };
            int segments = BottleConstants.CorkSegments;
            float r = _meshGenerator != null ? _meshGenerator.neckRadius : BottleConstants.CorkRadius;
            float h = BottleConstants.CorkHeight;

            var verts = new List<Vector3>();
            var tris = new List<int>();

            int bottomCenter = verts.Count;
            verts.Add(new Vector3(0f, -h * 0.5f, 0f));

            int bottomRing = verts.Count;
            for (int i = 0; i < segments; i++)
            {
                float a = i * Mathf.PI * 2f / segments;
                verts.Add(new Vector3(Mathf.Cos(a) * r, -h * 0.5f, Mathf.Sin(a) * r));
            }

            int topRing = verts.Count;
            for (int i = 0; i < segments; i++)
            {
                float a = i * Mathf.PI * 2f / segments;
                verts.Add(new Vector3(Mathf.Cos(a) * r, h * 0.5f, Mathf.Sin(a) * r));
            }

            int topCenter = verts.Count;
            verts.Add(new Vector3(0f, h * 0.5f, 0f));

            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                tris.Add(bottomCenter); tris.Add(bottomRing + next); tris.Add(bottomRing + i);
            }

            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                int bCurr = bottomRing + i;
                int bNext = bottomRing + next;
                int tCurr = topRing + i;
                int tNext = topRing + next;
                tris.Add(bCurr); tris.Add(tCurr); tris.Add(bNext);
                tris.Add(bNext); tris.Add(tCurr); tris.Add(tNext);
            }

            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                tris.Add(topCenter); tris.Add(topRing + i); tris.Add(topRing + next);
            }

            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            filter.sharedMesh = mesh;

            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
            var mat = new Material(shader);
            Color woodColor = new Color(
                BottleConstants.CorkWoodR,
                BottleConstants.CorkWoodG,
                BottleConstants.CorkWoodB);
            mat.color = woodColor;
            mat.SetColor("_BaseColor", woodColor);
            corkRenderer.sharedMaterial = mat;

            cork.SetActive(false);
            return cork;
        }

        private void OnDestroy()
        {
            if (corkObject == null) return;

            var filter = corkObject.GetComponent<MeshFilter>();
            if (filter != null && filter.sharedMesh != null && filter.sharedMesh.name == "CorkMesh")
            {
                SafeDestroy(filter.sharedMesh);
            }

            var corkRenderer = corkObject.GetComponent<MeshRenderer>();
            if (corkRenderer != null && corkRenderer.sharedMaterial != null)
            {
                SafeDestroy(corkRenderer.sharedMaterial);
            }
        }

        private static void SafeDestroy(UnityEngine.Object obj)
        {
#if UNITY_EDITOR
            if (!UnityEngine.Application.isPlaying)
                DestroyImmediate(obj);
            else
                Destroy(obj);
#else
            Destroy(obj);
#endif
        }
    }
}
