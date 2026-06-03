using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PuzzleGame
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class BottleMeshGenerator : MonoBehaviour
    {
        [Header("Bottle Shape")]
        public float height = 2.6f;
        public float bodyRadius = 0.4f;
        public float neckRadius = 0.12f;
        public float neckHeight = 0.6f;
        public float capRadius = 0.14f;
        public float capHeight = 0.07f;

        [Range(8, 64)] public int segments = 32;

        [Header("Materials")]
        public Material glassMaterial;
        public Material liquidMaterial;

        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;

#if UNITY_EDITOR
        private bool _isBuildingInProgress;
#endif

        private void Awake()
        {
            _meshFilter = GetComponent<MeshFilter>();
            _meshRenderer = GetComponent<MeshRenderer>();
        }

        private void Start()
        {
            if (_meshFilter == null) _meshFilter = GetComponent<MeshFilter>();
            if (_meshRenderer == null) _meshRenderer = GetComponent<MeshRenderer>();

            if (_meshFilter != null && (_meshFilter.sharedMesh == null || _meshFilter.sharedMesh.vertexCount == 0))
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

            AddRing(0f, safeBodyRadius);
            for (int i = 1; i <= 3; i++)
            {
                float t = i / 4f;
                float y = t * bodyH;
                float bulge = Mathf.Sin(t * Mathf.PI) * safeBodyRadius * 0.04f;
                AddRing(y, safeBodyRadius + bulge);
            }
            AddRing(bodyH, safeBodyRadius);

            for (int i = 1; i <= 3; i++)
            {
                float t = i / 4f;
                float y = bodyH + t * safeNeckHeight;
                float r = Mathf.Lerp(safeBodyRadius, safeNeckRadius, t);
                AddRing(y, r);
            }

            AddRing(bodyH + safeNeckHeight, safeNeckRadius);
            AddRing(bodyH + safeNeckHeight, safeCapRadius);
            AddRing(bodyH + safeNeckHeight + safeCapHeight, safeCapRadius);

            int topCenter = verts.Count;
            float topY = bodyH + safeNeckHeight + safeCapHeight;
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

            Mesh oldMesh = _meshFilter.sharedMesh;
            var mesh = new Mesh { name = "Bottle" };
            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();

            _meshFilter.sharedMesh = mesh;

#if UNITY_EDITOR
            if (oldMesh != null && !UnityEngine.Application.isPlaying)
            {
                DestroyImmediate(oldMesh);
            }
#else
            if (oldMesh != null)
            {
                Destroy(oldMesh);
            }
#endif
        }

        private void ApplyMaterials()
        {
            if (_meshRenderer == null) _meshRenderer = GetComponent<MeshRenderer>();
            if (_meshRenderer == null) return;

            if (glassMaterial != null && liquidMaterial != null)
                _meshRenderer.sharedMaterials = new[] { glassMaterial, liquidMaterial };
            else if (glassMaterial != null)
                _meshRenderer.sharedMaterials = new[] { glassMaterial };
        }

        private void OnDestroy()
        {
            if (_meshFilter != null && _meshFilter.sharedMesh != null)
            {
                if (_meshFilter.sharedMesh.name == "Bottle")
                {
#if UNITY_EDITOR
                    if (!UnityEngine.Application.isPlaying)
                        DestroyImmediate(_meshFilter.sharedMesh);
                    else
                        Destroy(_meshFilter.sharedMesh);
#else
                    Destroy(_meshFilter.sharedMesh);
#endif
                }
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (UnityEngine.Application.isPlaying || _isBuildingInProgress) return;

            _isBuildingInProgress = true;
            EditorApplication.delayCall += () =>
            {
                if (this == null) return;
                _meshFilter = GetComponent<MeshFilter>();
                _meshRenderer = GetComponent<MeshRenderer>();
                BuildMesh();
                ApplyMaterials();
                _isBuildingInProgress = false;
            };
        }
#endif
    }
}