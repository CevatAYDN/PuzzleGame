using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PuzzleGame
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class MoldMeshGenerator : MonoBehaviour
    {
        [Header("Mold Shape")]
        public float height = 2.6f;
        public float bodyRadius = 0.4f;
        public float neckRadius = 0.12f;
        public float neckHeight = 0.6f;
        public float capRadius = 0.14f;
        public float capHeight = 0.07f;

        [Range(8, 64)] public int segments = 32;

        [Header("Materials")]
        public Material glassMaterial;
        public Material OreMaterial;

        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;

#if UNITY_EDITOR
        private bool _isBuildingInProgress;
#endif

        private Mesh _instantiatedMesh;

        private void Awake()
        {
            _meshFilter = GetComponent<MeshFilter>();
            _meshRenderer = GetComponent<MeshRenderer>();
        }

        private void Start()
        {
            if (_meshFilter == null) _meshFilter = GetComponent<MeshFilter>();
            if (_meshRenderer == null) _meshRenderer = GetComponent<MeshRenderer>();

            BuildMesh();

            if (_meshRenderer != null && (_meshRenderer.sharedMaterials == null || _meshRenderer.sharedMaterials.Length == 0))
                ApplyMaterials();
        }

        public void BuildMesh()
        {
            if (_meshFilter == null) _meshFilter = GetComponent<MeshFilter>();
            if (_meshRenderer == null) _meshRenderer = GetComponent<MeshRenderer>();
            if (_meshFilter == null || _meshRenderer == null) return;

            segments = Mathf.Clamp(segments, 8, 64);

            float safeHeight = Mathf.Max(0.5f, height);
            float safeCapHeight = Mathf.Clamp(capHeight, 0.02f, safeHeight * 0.25f);
            float safeNeckHeight = Mathf.Clamp(neckHeight, 0.05f, safeHeight * 0.5f);
            float bodyH = Mathf.Max(0.15f, safeHeight - safeNeckHeight - safeCapHeight);

            float safeBodyRadius = Mathf.Max(0.05f, bodyRadius);
            float safeNeckRadius = Mathf.Clamp(neckRadius, 0.03f, safeBodyRadius);
            float safeCapRadius = Mathf.Max(safeNeckRadius, capRadius);

            var verts = new List<Vector3>();
            var norms = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();
            var ringStarts = new List<int>();

            float angleStep = Mathf.PI * 2f / segments;

            void AddRing(float y, float radius)
            {
                ringStarts.Add(verts.Count);
                for (int s = 0; s < segments; s++)
                {
                    float a = s * angleStep;
                    float x = Mathf.Cos(a) * radius;
                    float z = Mathf.Sin(a) * radius;
                    verts.Add(new Vector3(x, y, z));
                    norms.Add(new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)));
                    uvs.Add(new Vector2((float)s / segments, Mathf.Clamp01(y / safeHeight)));
                }
            }

            int bottomCenter = verts.Count;
            verts.Add(Vector3.zero);
            norms.Add(Vector3.down);
            uvs.Add(new Vector2(0.5f, 0f));

            // Pure crucible shape: tapered cylinder from bottom to top with draft angle
            float bottomRadius = safeBodyRadius * 0.8f;
            float topRadius = safeBodyRadius * 1.05f;

            int ringCount = 8;
            for (int i = 0; i <= ringCount; i++)
            {
                float t = (float)i / ringCount;
                float y = t * safeHeight;
                float r = Mathf.Lerp(bottomRadius, topRadius, t);
                AddRing(y, r);
            }

            int topCenter = verts.Count;
            float topY = safeHeight;
            verts.Add(new Vector3(0f, topY, 0f));
            norms.Add(Vector3.up);
            uvs.Add(new Vector2(0.5f, 1f));

            int firstRing = ringStarts[0];
            for (int s = 0; s < segments; s++)
            {
                int s1 = (s + 1) % segments;
                tris.Add(bottomCenter);
                tris.Add(firstRing + s1);
                tris.Add(firstRing + s);
            }

            for (int r = 0; r < ringStarts.Count - 1; r++)
            {
                int aStart = ringStarts[r];
                int bStart = ringStarts[r + 1];

                for (int s = 0; s < segments; s++)
                {
                    int s1 = (s + 1) % segments;
                    tris.Add(aStart + s);  tris.Add(bStart + s);  tris.Add(aStart + s1);
                    tris.Add(aStart + s1); tris.Add(bStart + s);  tris.Add(bStart + s1);
                }
            }

            int lastRing = ringStarts[ringStarts.Count - 1];
            for (int s = 0; s < segments; s++)
            {
                int s1 = (s + 1) % segments;
                tris.Add(topCenter);
                tris.Add(lastRing + s);
                tris.Add(lastRing + s1);
            }

            Mesh oldMesh = _instantiatedMesh;
            var mesh = new Mesh { name = "Mold" };
            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();

            _meshFilter.sharedMesh = mesh;
            _instantiatedMesh = mesh;

            if (oldMesh != null)
            {
                SafeDestroy(oldMesh);
            }
        }

        private void ApplyMaterials()
        {
            if (_meshRenderer == null) _meshRenderer = GetComponent<MeshRenderer>();
            if (_meshRenderer == null) return;

            if (glassMaterial != null && OreMaterial != null)
                _meshRenderer.sharedMaterials = new[] { glassMaterial, OreMaterial };
            else if (glassMaterial != null)
                _meshRenderer.sharedMaterials = new[] { glassMaterial };
        }

        private void OnDestroy()
        {
            if (_instantiatedMesh != null)
            {
                SafeDestroy(_instantiatedMesh);
                _instantiatedMesh = null;
            }
        }

        private void SafeDestroy(UnityEngine.Object obj)
        {
            if (obj == null) return;
#if UNITY_EDITOR
            if (!UnityEngine.Application.isPlaying)
            {
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    if (obj != null)
                    {
                        DestroyImmediate(obj);
                    }
                };
                return;
            }
#endif
            Destroy(obj);
        }

#if UNITY_EDITOR
        // Snapshot of the last values that triggered a build. Subsequent
        // OnValidate calls whose deltas fall below the threshold are elided —
        // the editor drags the slider by tiny fractions every frame, and each
        // one used to issue a full mesh rebuild + material reapply.
        private float _lastHeight = -1f;
        private float _lastBodyRadius = -1f;
        private float _lastNeckRadius = -1f;
        private float _lastNeckHeight = -1f;
        private float _lastCapRadius = -1f;
        private float _lastCapHeight = -1f;
        private int _lastSegments = -1;
        private const float RebuildEpsilon = 1e-3f;

        private bool HasShapeChanged()
        {
            if (Mathf.Abs(height       - _lastHeight)       > RebuildEpsilon) return true;
            if (Mathf.Abs(bodyRadius   - _lastBodyRadius)   > RebuildEpsilon) return true;
            if (Mathf.Abs(neckRadius   - _lastNeckRadius)   > RebuildEpsilon) return true;
            if (Mathf.Abs(neckHeight   - _lastNeckHeight)   > RebuildEpsilon) return true;
            if (Mathf.Abs(capRadius    - _lastCapRadius)    > RebuildEpsilon) return true;
            if (Mathf.Abs(capHeight    - _lastCapHeight)    > RebuildEpsilon) return true;
            if (segments != _lastSegments) return true;
            return false;
        }

        private void CacheShapeSnapshot()
        {
            _lastHeight = height;
            _lastBodyRadius = bodyRadius;
            _lastNeckRadius = neckRadius;
            _lastNeckHeight = neckHeight;
            _lastCapRadius = capRadius;
            _lastCapHeight = capHeight;
            _lastSegments = segments;
        }

        private void OnValidate()
        {
            if (UnityEngine.Application.isPlaying || _isBuildingInProgress) return;

            // First OnValidate after a fresh domain reload — always build.
            bool firstBuild = _lastHeight < 0f;
            if (!firstBuild && !HasShapeChanged()) return;

            _isBuildingInProgress = true;
            EditorApplication.delayCall += () =>
            {
                if (this == null) return;
                _meshFilter = GetComponent<MeshFilter>();
                _meshRenderer = GetComponent<MeshRenderer>();
                BuildMesh();
                ApplyMaterials();
                CacheShapeSnapshot();
                _isBuildingInProgress = false;
            };
        }
#endif
    }
}