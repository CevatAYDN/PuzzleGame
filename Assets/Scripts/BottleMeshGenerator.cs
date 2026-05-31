using UnityEngine;
using System.Collections.Generic;

namespace BottleShaders
{
    /// <summary>
    /// Generates a bottle mesh procedurally.
    /// </summary>
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class BottleMeshGenerator : MonoBehaviour
    {
        [Header("Bottle Shape")]
        public float height = 2.4f;
        public float bodyRadius = 0.35f;
        public float neckRadius = 0.15f;
        public float neckHeight = 0.4f;
        public float capRadius = 0.17f;
        public float capHeight = 0.1f;

        [Range(8, 64)] public int segments = 24;

        [Header("Materials")]
        public Material glassMaterial;
        public Material liquidMaterial;

        private MeshFilter mf;
        private MeshRenderer mr;

        private void Start()
        {
            mf = GetComponent<MeshFilter>();
            mr = GetComponent<MeshRenderer>();

            if (mf != null && (mf.sharedMesh == null || mf.sharedMesh.vertexCount == 0))
                BuildMesh();

            ApplyMaterials();
        }

        public void BuildMesh()
        {
            if (mf == null) mf = GetComponent<MeshFilter>();
            if (mr == null) mr = GetComponent<MeshRenderer>();
            if (mf == null || mr == null) return;

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

            // Bottom center
            int bottomCenter = verts.Count;
            verts.Add(Vector3.zero);
            norms.Add(Vector3.down);
            uvs.Add(new Vector2(0.5f, 0f));

            // Full rings from bottom to top
            AddRing(0f, safeBodyRadius);                 // 0 bottom edge
            for (int i = 1; i <= 3; i++)                 // 1..3 body transition
            {
                float t = i / 4f;
                float y = t * bodyH;
                float bulge = Mathf.Sin(t * Mathf.PI) * safeBodyRadius * 0.08f;
                AddRing(y, safeBodyRadius + bulge);
            }
            AddRing(bodyH, safeBodyRadius);              // 4 body top

            for (int i = 1; i <= 3; i++)                 // 5..7 neck transition
            {
                float t = i / 4f;
                float y = bodyH + t * safeNeckHeight;
                float r = Mathf.Lerp(safeBodyRadius, safeNeckRadius, t);
                AddRing(y, r);
            }

            AddRing(bodyH + safeNeckHeight, safeNeckRadius);                  // 8 neck top
            AddRing(bodyH + safeNeckHeight, safeCapRadius);                   // 9 cap lip bottom
            AddRing(bodyH + safeNeckHeight + safeCapHeight, safeCapRadius);   // 10 cap top

            // Top center
            int topCenter = verts.Count;
            float topY = bodyH + safeNeckHeight + safeCapHeight;
            verts.Add(new Vector3(0f, topY, 0f));
            norms.Add(Vector3.up);
            uvs.Add(new Vector2(0.5f, 1f));

            // Bottom cap fan
            int firstRing = ringStarts[0];
            for (int s = 0; s < segments; s++)
            {
                int s1 = (s + 1) % segments;
                tris.Add(bottomCenter);
                tris.Add(firstRing + s1);
                tris.Add(firstRing + s);
            }

            // Side strips only between FULL rings (fixes out-of-range triangle indices)
            for (int r = 0; r < ringStarts.Count - 1; r++)
            {
                int aStart = ringStarts[r];
                int bStart = ringStarts[r + 1];

                for (int s = 0; s < segments; s++)
                {
                    int s1 = (s + 1) % segments;

                    int a0 = aStart + s;
                    int a1 = aStart + s1;
                    int b0 = bStart + s;
                    int b1 = bStart + s1;

                    tris.Add(a0); tris.Add(b0); tris.Add(a1);
                    tris.Add(a1); tris.Add(b0); tris.Add(b1);
                }
            }

            // Top cap fan
            int lastRing = ringStarts[ringStarts.Count - 1];
            for (int s = 0; s < segments; s++)
            {
                int s1 = (s + 1) % segments;
                tris.Add(topCenter);
                tris.Add(lastRing + s);
                tris.Add(lastRing + s1);
            }

            var mesh = new Mesh { name = "Bottle" };
            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();

            if (mf.sharedMesh != null)
            {
                if (Application.isPlaying)
                    Destroy(mf.sharedMesh);
                else
                    DestroyImmediate(mf.sharedMesh);
            }

            mf.sharedMesh = mesh;
        }

        private void ApplyMaterials()
        {
            if (mr == null) mr = GetComponent<MeshRenderer>();
            if (mr == null) return;

            // Always use sharedMaterials for consistency
            if (glassMaterial != null && liquidMaterial != null)
                mr.sharedMaterials = new[] { glassMaterial, liquidMaterial };
            else if (glassMaterial != null)
                mr.sharedMaterials = new[] { glassMaterial };
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying) return;

            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this == null) return;
                mf = GetComponent<MeshFilter>();
                mr = GetComponent<MeshRenderer>();
                BuildMesh();
                ApplyMaterials();
            };
        }
#endif
    }
}
