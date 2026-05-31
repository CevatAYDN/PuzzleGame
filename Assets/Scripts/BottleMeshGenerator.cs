using UnityEngine;
using System.Collections.Generic;

namespace BottleShaders
{
    /// <summary>
    /// Generates a bottle-shaped mesh using a simple ring-to-ring approach.
    /// Each ring has exactly `segments` vertices. Caps are handled by zero-radius rings.
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

        [Range(8, 64)]
        public int segments = 24;

        [Header("Materials")]
        public Material glassMaterial;
        public Material liquidMaterial;

        private MeshFilter mf;
        private MeshRenderer mr;

        void Start()
        {
            mf = GetComponent<MeshFilter>();
            mr = GetComponent<MeshRenderer>();
            BuildMesh();
            ApplyMaterials();
        }

        public void BuildMesh()
        {
            if (mf == null) mf = GetComponent<MeshFilter>();
            if (mr == null) mr = GetComponent<MeshRenderer>();
            if (mf == null || mr == null) return;

            var verts = new List<Vector3>();
            var norms = new List<Vector3>();
            var uvs   = new List<Vector2>();

            float bodyH = height - neckHeight - capHeight;
            float angleStep = Mathf.PI * 2f / segments;

            // ── Ring 0: Bottom cap center (r=0) ──
            verts.Add(Vector3.zero);
            norms.Add(Vector3.down);
            uvs.Add(new Vector2(0.5f, 0f));

            // ── Ring 1: Bottom edge ──
            for (int s = 0; s < segments; s++)
            {
                float a = s * angleStep;
                verts.Add(new Vector3(Mathf.Cos(a) * bodyRadius, 0, Mathf.Sin(a) * bodyRadius));
                norms.Add(new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)));
                uvs.Add(new Vector2((float)s / segments, 0f));
            }

            // ── Body rings ──
            for (int i = 1; i < 4; i++)
            {
                float t = i / 4f;
                float y = t * bodyH;
                float bulge = Mathf.Sin(t * Mathf.PI) * bodyRadius * 0.08f;
                float r = bodyRadius + bulge;
                for (int s = 0; s < segments; s++)
                {
                    float a = s * angleStep;
                    verts.Add(new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r));
                    norms.Add(new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)));
                    uvs.Add(new Vector2((float)s / segments, y / height));
                }
            }

            // ── Ring: Body top ──
            for (int s = 0; s < segments; s++)
            {
                float a = s * angleStep;
                verts.Add(new Vector3(Mathf.Cos(a) * bodyRadius, bodyH, Mathf.Sin(a) * bodyRadius));
                norms.Add(new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)));
                uvs.Add(new Vector2((float)s / segments, bodyH / height));
            }

            // ── Neck rings ──
            for (int i = 1; i < 4; i++)
            {
                float t = i / 4f;
                float y = bodyH + t * neckHeight;
                float r = Mathf.Lerp(bodyRadius, neckRadius, t);
                for (int s = 0; s < segments; s++)
                {
                    float a = s * angleStep;
                    verts.Add(new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r));
                    norms.Add(new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)));
                    uvs.Add(new Vector2((float)s / segments, y / height));
                }
            }

            // ── Ring: Neck top ──
            for (int s = 0; s < segments; s++)
            {
                float a = s * angleStep;
                verts.Add(new Vector3(Mathf.Cos(a) * neckRadius, bodyH + neckHeight, Mathf.Sin(a) * neckRadius));
                norms.Add(new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)));
                uvs.Add(new Vector2((float)s / segments, (bodyH + neckHeight) / height));
            }

            // ── Ring: Cap bottom (same Y, wider) ──
            for (int s = 0; s < segments; s++)
            {
                float a = s * angleStep;
                verts.Add(new Vector3(Mathf.Cos(a) * capRadius, bodyH + neckHeight, Mathf.Sin(a) * capRadius));
                norms.Add(new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)));
                uvs.Add(new Vector2((float)s / segments, (bodyH + neckHeight) / height));
            }

            // ── Ring: Cap top ──
            for (int s = 0; s < segments; s++)
            {
                float a = s * angleStep;
                float y = bodyH + neckHeight + capHeight;
                verts.Add(new Vector3(Mathf.Cos(a) * capRadius, y, Mathf.Sin(a) * capRadius));
                norms.Add(new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)));
                uvs.Add(new Vector2((float)s / segments, y / height));
            }

            // ── Ring 11: Cap top center ──
            float topY = bodyH + neckHeight + capHeight;
            verts.Add(new Vector3(0, topY, 0));
            norms.Add(Vector3.up);
            uvs.Add(new Vector2(0.5f, 1f));

            // ══════════════════════════════════════════════════
            //  Triangles
            //  Ring layout:
            //    0: center (1 vert)
            //    1-10: rings (segments verts each)
            //    11: center (1 vert)
            //  Index = ringStart[r] + s  (for non-center rings)
            // ═══════════════════════════════════════════════════
            var tris = new List<int>();

            // Ring start indices
            int[] ringStart = new int[12];
            ringStart[0] = 0;                              // 1 vert
            for (int r = 1; r <= 10; r++) ringStart[r] = 1 + (r - 1) * segments;
            ringStart[11] = 1 + 10 * segments;             // center

            // Bottom cap fan: ring 0 (center) → ring 1
            for (int s = 0; s < segments; s++)
            {
                tris.Add(ringStart[0]);
                tris.Add(ringStart[1] + ((s + 1) % segments));
                tris.Add(ringStart[1] + s);
            }

            // Side strips: rings 1→2, 2→3, ..., 9→10, 10→11
            for (int r = 1; r <= 10; r++)
            {
                int rNext = r + 1;
                for (int s = 0; s < segments; s++)
                {
                    int s1 = (s + 1) % segments;
                    int a0 = ringStart[r] + s;
                    int a1 = ringStart[r] + s1;
                    int b0 = ringStart[rNext] + s;
                    int b1 = ringStart[rNext] + s1;

                    tris.Add(a0); tris.Add(b0); tris.Add(a1);
                    tris.Add(a1); tris.Add(b0); tris.Add(b1);
                }
            }

            // Top cap fan: ring 10 → ring 11 (center)
            for (int s = 0; s < segments; s++)
            {
                tris.Add(ringStart[11]);
                tris.Add(ringStart[10] + s);
                tris.Add(ringStart[10] + ((s + 1) % segments));
            }

            var mesh = new Mesh { name = "Bottle" };
            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();

            if (mf.sharedMesh != null)
                DestroyImmediate(mf.sharedMesh);
            mf.sharedMesh = mesh;
        }

        void ApplyMaterials()
        {
            if (mr == null) mr = GetComponent<MeshRenderer>();
            if (mr == null) return;

            if (glassMaterial != null && liquidMaterial != null)
                mr.sharedMaterials = new Material[] { glassMaterial, liquidMaterial };
            else if (glassMaterial != null)
                mr.sharedMaterial = glassMaterial;
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (!Application.isPlaying)
            {
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    if (this == null) return;
                    mf = GetComponent<MeshFilter>();
                    mr = GetComponent<MeshRenderer>();
                    BuildMesh();
                    ApplyMaterials();
                };
            }
        }
#endif
    }
}
