using UnityEngine;
using PuzzleGame.Infrastructure.Interfaces;
using PuzzleGame.Domain.Models;
using PuzzleGame.Domain.Interfaces;
using PuzzleGame.Logging;
using PuzzleGame.Application.Interfaces;
using System.Collections.Generic;

namespace PuzzleGame
{
    [RequireComponent(typeof(Renderer))]
    [RequireComponent(typeof(Wobble))]
    public class BottleController : MonoBehaviour
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
        public float Height => _meshGenerator != null ? _meshGenerator.height : 2.4f;
        public bool IsCapped { get; private set; }

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
        private static readonly int RimIntensityID     = Shader.PropertyToID("_RimIntensity");

        public void Initialize(IRendererService rendererService,
                               IBottleValidator  validator,
                               IAnimationService animationService,
                               List<LiquidLayer> initialLayers)
        {
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

            int maxLayers = visualConfig != null ? visualConfig.maxLayers : BottleState.MaxSupportedLayers;
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

        public bool IsEmpty() => State?.IsEmpty ?? true;
        public bool IsFull()  => State?.IsFull  ?? false;

        public bool HasSingleColorContent()
        {
            if (State == null || State.IsEmpty || State.Layers.Count == 0) return true;
            var firstColor = State.Layers[0].Color;
            for (int i = 1; i < State.Layers.Count; i++)
                if (!_validator.ColorsMatch(State.Layers[i].Color, firstColor)) return false;
            return true;
        }

        public bool TryPourTo(BottleController target)
        {
            if (target == null)
            {
                BottleLogger.LogWarning($"'{name}': TryPourTo called with null target.");
                return false;
            }

            if (!_validator.CanPour(State, target.State))
            {
                BottleLogger.LogDebug($"'{name}' → '{target.name}': pour rejected by validator.");
                return false;
            }

            var layer = State.PopTopLayer();
            if (layer == null)
            {
                BottleLogger.LogError($"'{name}': validator allowed pour but PopTopLayer returned null.");
                return false;
            }

            if (!target.State.AddLayer(layer.Value))
            {
                State.AddLayer(layer.Value);
                BottleLogger.LogError($"'{name}' → '{target.name}': AddLayer failed after validator approval. Rolled back.");
                return false;
            }

            // Add wobble impulse for pour effect
            float impulse = visualConfig != null ? visualConfig.pourImpulseStrength : 2.0f;
            Vector3 pourDirection = (target.transform.position - transform.position).normalized;
            _wobble?.AddImpulse(-pourDirection, impulse);
            target._wobble?.AddImpulse(pourDirection, impulse * 0.8f);

            BottleLogger.LogInfo($"Poured {layer.Value.Color} from '{name}' to '{target.name}'.");
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
                    if (layer.Amount > 0.0001f)
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
                if (extra.Amount > 0.0001f)
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

            float sat = visualConfig != null ? visualConfig.saturationBoost : 1.35f;
            float bri = visualConfig != null ? visualConfig.brightnessBoost : 1.2f;
            _rendererService.UpdateLiquid(_renderer, _visualLayers, _visualTotalFill, sat, bri);

            bool isEmpty = _visualLayers.Count == 0 || _visualTotalFill <= 0.001f;
            DomainColor baseColor = _visualLayers.Count > 0 ? _visualLayers[0].Color : new DomainColor(0, 0, 0, 0);
            _rendererService.UpdateGlass(_renderer, isEmpty, baseColor);
        }

        public void SetSelectionHighlight(bool active)
        {
            if (_renderer == null) return;
            if (_isHighlighted == active) return;
            _isHighlighted = active;
            if (_propBlock == null) _propBlock = new MaterialPropertyBlock();
            _renderer.GetPropertyBlock(_propBlock, 0);
            _propBlock.SetFloat(FresnelIntensityID, active ? 4.0f : 1.5f);
            _renderer.SetPropertyBlock(_propBlock, 0);
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

            if (_renderer != null && _renderer.sharedMaterials.Length > 1)
            {
                _animationService?.AnimateLiquidFlash(
                    _renderer, 1, 4.0f, 0.6f, onComplete: null);
            }
        }

        public void PlaySettleBounce()
        {
            _animationService?.AnimateSettleBounce(this, 0.6f, onComplete: null);
        }

        private GameObject CreateProceduralCork()
        {
            var cork = new GameObject("Cork");
            cork.transform.SetParent(transform, false);
            cork.transform.localPosition = new Vector3(0f, Height - 0.05f, 0f);
            cork.transform.localRotation = Quaternion.identity;
            cork.transform.localScale = Vector3.zero;

            var filter = cork.AddComponent<MeshFilter>();
            var renderer = cork.AddComponent<MeshRenderer>();

            var mesh = new Mesh { name = "CorkMesh" };
            int segments = 16;
            float r = _meshGenerator != null ? _meshGenerator.neckRadius : 0.15f;
            float h = 0.25f;

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
            Color woodColor = new Color(0.45f, 0.28f, 0.16f);
            mat.color = woodColor;
            mat.SetColor("_BaseColor", woodColor);
            renderer.sharedMaterial = mat;

            cork.SetActive(false);
            return cork;
        }

        private void OnDestroy()
        {
            if (corkObject != null)
            {
                var filter = corkObject.GetComponent<MeshFilter>();
                if (filter != null && filter.sharedMesh != null && filter.sharedMesh.name == "CorkMesh")
                {
#if UNITY_EDITOR
                    if (!UnityEngine.Application.isPlaying)
                        DestroyImmediate(filter.sharedMesh);
                    else
                        Destroy(filter.sharedMesh);
#else
                    Destroy(filter.sharedMesh);
#endif
                }

                var renderer = corkObject.GetComponent<MeshRenderer>();
                if (renderer != null && renderer.sharedMaterial != null)
                {
#if UNITY_EDITOR
                    if (!UnityEngine.Application.isPlaying)
                        DestroyImmediate(renderer.sharedMaterial);
                    else
                        Destroy(renderer.sharedMaterial);
#else
                    Destroy(renderer.sharedMaterial);
#endif
                }
            }
        }
    }
}
