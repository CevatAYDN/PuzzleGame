using System;
using System.Collections.Generic;
using PuzzleGame.Application.Interfaces;
using UnityEngine;
using PuzzleGame.Infrastructure;

namespace PuzzleGame
{
    /// <summary>
    /// Manages cork lifecycle: procedural mesh creation, show/hide, drop animation, resource cleanup.
    /// Extracted from MoldController for SRP (single responsibility = cork).
    /// Animation service is optional — cork drop animation is skipped if null.
    /// </summary>
    public sealed class MoldCorkController
    {
        private readonly Transform _MoldTransform;
        private readonly IAnimationService _animationService;
        private readonly Func<float> _MoldHeightProvider;
        private readonly Func<float> _neckRadiusProvider;
        private GameObject _corkObject;
        private bool _isCapped;

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
        public void EnsureCork()
        {
            if (_corkObject == null)
            {
                var child = _MoldTransform.Find("Cork");
                if (child != null)
                {
                    _corkObject = child.gameObject;
                }
                else
                {
                    _corkObject = CreateProceduralCork();
                }
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
        /// </summary>
        public void DisposeResources()
        {
            if (_corkObject == null) return;

            var filter = _corkObject.GetComponent<MeshFilter>();
            if (filter != null && filter.sharedMesh != null && filter.sharedMesh.name == "CorkMesh")
            {
                SafeDestroy(filter.sharedMesh);
            }

            var corkRenderer = _corkObject.GetComponent<MeshRenderer>();
            if (corkRenderer != null && corkRenderer.sharedMaterial != null)
            {
                SafeDestroy(corkRenderer.sharedMaterial);
            }
        }

        private GameObject CreateProceduralCork()
        {
            var cork = new GameObject("Cork");
            cork.transform.SetParent(_MoldTransform, false);
            float MoldHeight = _MoldHeightProvider();
            cork.transform.localPosition = new Vector3(0f, MoldHeight - PuzzleGame.Infrastructure.CorkConstants.YOffset, 0f);
            cork.transform.localRotation = Quaternion.identity;
            cork.transform.localScale = Vector3.zero;

            var filter = cork.AddComponent<MeshFilter>();
            var corkRenderer = cork.AddComponent<MeshRenderer>();

            var mesh = new Mesh { name = "CorkMesh" };
            int segments = PuzzleGame.Infrastructure.CorkConstants.Segments;
            float r = _neckRadiusProvider();
            float h = PuzzleGame.Infrastructure.CorkConstants.Height;

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
                PuzzleGame.Infrastructure.CorkConstants.WoodR,
                PuzzleGame.Infrastructure.CorkConstants.WoodG,
                PuzzleGame.Infrastructure.CorkConstants.WoodB);
            mat.color = woodColor;
            mat.SetColor("_BaseColor", woodColor);
            corkRenderer.sharedMaterial = mat;

            cork.SetActive(false);
            return cork;
        }

        private static void SafeDestroy(UnityEngine.Object obj)
        {
#if UNITY_EDITOR
            if (!UnityEngine.Application.isPlaying)
                UnityEngine.Object.DestroyImmediate(obj);
            else
                UnityEngine.Object.Destroy(obj);
#else
            UnityEngine.Object.Destroy(obj);
#endif
        }
    }
}
