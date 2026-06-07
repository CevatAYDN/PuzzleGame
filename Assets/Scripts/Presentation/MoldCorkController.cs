using System;
using System.Collections.Generic;
using PuzzleGame.Application;
using PuzzleGame.Application.Interfaces;
using UnityEngine;

namespace PuzzleGame
{
    /// <summary>
    /// Manages cork lifecycle: procedural mesh creation, show/hide, drop animation, resource cleanup.
    /// Extracted from MoldController for SRP (single responsibility = cork).
    /// Animation service is optional — cork drop animation is skipped if null.
    /// </summary>
    public sealed class MoldCorkController
    {
        // ── Shared mesh/material cache (static — shared across all corks) ──
        private static readonly Dictionary<int, Mesh> s_meshCache = new Dictionary<int, Mesh>();
        private static Material s_sharedMaterial;
        private static Shader s_cachedShader;

        /// <summary>
        /// Clears the static mesh/material cache. Call on scene unload or application quit
        /// to prevent memory leaks. Safe to call multiple times.
        /// </summary>
        public static void ClearCache()
        {
            foreach (var kvp in s_meshCache)
            {
                if (kvp.Value != null)
                    SafeDestroy(kvp.Value);
            }
            s_meshCache.Clear();

            if (s_sharedMaterial != null)
            {
                SafeDestroy(s_sharedMaterial);
                s_sharedMaterial = null;
            }

            s_cachedShader = null;
        }

        // ── Instance fields ──
        private readonly Transform _MoldTransform;
        private readonly IAnimationService _animationService;
        private readonly Func<float> _MoldHeightProvider;
        private readonly Func<float> _neckRadiusProvider;
        private GameObject _corkObject;
        private bool _isCapped;
        private bool _isProcedural;

        public bool IsCapped => _isCapped;
        public GameObject CorkObject => _corkObject;

        public MoldCorkController(
            Transform MoldTransform,
            IAnimationService animationService,
            Func<float> MoldHeightProvider,
            Func<float> neckRadiusProvider,
            GameObject existingCork)
        {
            _MoldTransform = MoldTransform ?? throw new ArgumentNullException(nameof(MoldTransform));
            _animationService = animationService; // nullable — cork drop animation is optional
            _MoldHeightProvider = MoldHeightProvider ?? throw new ArgumentNullException(nameof(MoldHeightProvider));
            _neckRadiusProvider = neckRadiusProvider ?? throw new ArgumentNullException(nameof(neckRadiusProvider));
            _corkObject = existingCork;
        }

        /// <summary>
        /// Ensures a cork GameObject exists. If none is assigned, finds a child named "Cork" or creates a procedural one.
        /// Cork starts hidden (inactive). Resets capped state.
        /// </summary>
        public void EnsureCork(bool isFromOnValidate = false)
        {
            var child = _MoldTransform.Find("Cork");
            if (child != null)
            {
                _corkObject = child.gameObject;
                _isProcedural = false;
                UpdateCorkMeshAndMaterial(_corkObject);
            }
            else if (!isFromOnValidate)
            {
                _corkObject = CreateProceduralCork();
                _isProcedural = true;
            }

            if (_corkObject != null)
            {
                _corkObject.SetActive(false);
            }
            _isCapped = false;
        }

        /// <summary>
        /// Activates the cork, animates the drop, and marks as capped. No-op if already capped.
        /// </summary>
        public void AnimateDrop()
        {
            if (_isCapped) return;
            _isCapped = true;

            if (_corkObject != null)
            {
                _corkObject.SetActive(true);
                _animationService?.AnimateCorkDrop(
                    _corkObject.transform, _MoldHeightProvider(), onComplete: null);
            }
        }

        /// <summary>
        /// Cleans up procedurally-created cork mesh and material to prevent memory leaks.
        /// Safe to call multiple times. Does not destroy externally-assigned corks.
        /// Cached shared mesh/material is NOT destroyed here (use ClearCache() for that).
        /// </summary>
        public void DisposeResources()
        {
            if (_corkObject == null || !_isProcedural) return;

            // Only destroy non-cached resources (externally assigned meshes/materials)
            var filter = _corkObject.GetComponent<MeshFilter>();
            if (filter != null && filter.sharedMesh != null && filter.sharedMesh.name != "CorkMesh")
            {
                SafeDestroy(filter.sharedMesh);
            }

            var corkRenderer = _corkObject.GetComponent<MeshRenderer>();
            if (corkRenderer != null && corkRenderer.sharedMaterial != null && corkRenderer.sharedMaterial != s_sharedMaterial)
            {
                SafeDestroy(corkRenderer.sharedMaterial);
            }
        }

        private GameObject CreateProceduralCork()
        {
            var cork = new GameObject("Cork");
            cork.transform.SetParent(_MoldTransform, false);
            float MoldHeight = _MoldHeightProvider();
            cork.transform.localPosition = new Vector3(0f, MoldHeight - CorkConstants.YOffset, 0f);
            cork.transform.localRotation = Quaternion.identity;
            cork.transform.localScale = Vector3.zero;

            UpdateCorkMeshAndMaterial(cork);

            cork.SetActive(false);
            return cork;
        }

        private void UpdateCorkMeshAndMaterial(GameObject cork)
        {
            if (cork == null) return;

            var filter = cork.GetComponent<MeshFilter>();
            if (filter == null) filter = cork.AddComponent<MeshFilter>();

            var corkRenderer = cork.GetComponent<MeshRenderer>();
            if (corkRenderer == null) corkRenderer = cork.AddComponent<MeshRenderer>();

            // ── Mesh: use cache (keyed by quantized neck radius) ──
            float r = _neckRadiusProvider();
            int radiusKey = Mathf.RoundToInt(r * 10000f); // quantize to 0.0001m precision

            if (!s_meshCache.TryGetValue(radiusKey, out var mesh) || mesh == null)
            {
                mesh = BuildCorkMesh(r);
                s_meshCache[radiusKey] = mesh;
            }

            Mesh oldMesh = filter.sharedMesh;
            filter.sharedMesh = mesh;
            // Only destroy old mesh if it was not from cache
            if (oldMesh != null && oldMesh != mesh && oldMesh.name != "CorkMesh")
            {
                SafeDestroy(oldMesh);
            }

            // ── Material: use shared singleton ──
            if (s_sharedMaterial == null)
            {
                s_sharedMaterial = BuildCorkMaterial();
            }

            Material oldMat = corkRenderer.sharedMaterial;
            corkRenderer.sharedMaterial = s_sharedMaterial;
            // Only destroy old material if it was not the shared one
            if (oldMat != null && oldMat != s_sharedMaterial)
            {
                SafeDestroy(oldMat);
            }
        }

        /// <summary>
        /// Builds a procedural cylinder mesh for the cork.
        /// </summary>
        private static Mesh BuildCorkMesh(float radius)
        {
            var mesh = new Mesh { name = "CorkMesh" };
            int segments = CorkConstants.Segments;
            float h = CorkConstants.Height;

            // Pre-allocate exact sizes to avoid List resizing
            int vertCount = 1 + segments + segments + 1; // bottom center + bottom ring + top ring + top center
            int triCount = segments + segments * 2 + segments; // bottom fan + side quads + top fan
            var verts = new Vector3[vertCount];
            var tris = new int[triCount * 3];

            int idx = 0;
            // Bottom center
            verts[idx++] = new Vector3(0f, -h * 0.5f, 0f); // 0

            // Bottom ring
            int bottomRingStart = idx;
            for (int i = 0; i < segments; i++)
            {
                float a = i * Mathf.PI * 2f / segments;
                verts[idx++] = new Vector3(Mathf.Cos(a) * radius, -h * 0.5f, Mathf.Sin(a) * radius);
            }

            // Top ring
            int topRingStart = idx;
            for (int i = 0; i < segments; i++)
            {
                float a = i * Mathf.PI * 2f / segments;
                verts[idx++] = new Vector3(Mathf.Cos(a) * radius, h * 0.5f, Mathf.Sin(a) * radius);
            }

            // Top center
            verts[idx++] = new Vector3(0f, h * 0.5f, 0f);
            int topCenter = idx - 1;

            int ti = 0;
            // Bottom fan
            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                tris[ti++] = 0;
                tris[ti++] = bottomRingStart + next;
                tris[ti++] = bottomRingStart + i;
            }

            // Side quads
            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                int bCurr = bottomRingStart + i;
                int bNext = bottomRingStart + next;
                int tCurr = topRingStart + i;
                int tNext = topRingStart + next;
                tris[ti++] = bCurr; tris[ti++] = tCurr; tris[ti++] = bNext;
                tris[ti++] = bNext; tris[ti++] = tCurr; tris[ti++] = tNext;
            }

            // Top fan
            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                tris[ti++] = topCenter;
                tris[ti++] = topRingStart + i;
                tris[ti++] = topRingStart + next;
            }

            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// Builds the shared cork material (graphite/dark stone appearance).
        /// </summary>
        private static Material BuildCorkMaterial()
        {
            if (s_cachedShader == null)
            {
                s_cachedShader = Shader.Find("Universal Render Pipeline/Lit")
                              ?? Shader.Find("Universal Render Pipeline/Unlit")
                              ?? Shader.Find("Sprites/Default");
            }

            var mat = new Material(s_cachedShader);
            Color woodColor = new Color(
                CorkConstants.WoodR,
                CorkConstants.WoodG,
                CorkConstants.WoodB);
            mat.color = woodColor;
            mat.SetColor("_BaseColor", woodColor);
            if (s_cachedShader.name.Contains("Lit"))
            {
                mat.SetFloat("_Metallic", 0.8f);
                mat.SetFloat("_Smoothness", 0.2f);
            }
            return mat;
        }

        private static void SafeDestroy(UnityEngine.Object obj)
        {
            if (obj == null) return;
#if UNITY_EDITOR
            if (!UnityEngine.Application.isPlaying)
            {
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    if (obj != null)
                    {
                        UnityEngine.Object.DestroyImmediate(obj);
                    }
                };
                return;
            }
#endif
            UnityEngine.Object.Destroy(obj);
        }
    }
}
