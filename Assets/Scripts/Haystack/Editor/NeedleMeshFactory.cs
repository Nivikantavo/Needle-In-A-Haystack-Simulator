using System.Collections.Generic;
using UnityEngine;

namespace Needle.Haystack.Editor
{
    public static class NeedleMeshFactory
    {
        const float ShaftRadius = 0.22f;
        const float ShaftBottom = -0.36f;
        const float ShaftTop = 0.32f;
        const float TipY = -0.5f;
        const float EyeY = 0.4f;
        const float EyeMajor = 0.13f;
        const float EyeTube = 0.055f;

        public static Mesh Create(int sides = 6)
        {
            sides = Mathf.Clamp(sides, 6, 10);
            var vertices = new List<Vector3>(160);
            var normals = new List<Vector3>(160);
            var uvs = new List<Vector2>(160);
            var triangles = new List<int>(480);

            AddCylinder(vertices, normals, uvs, triangles, ShaftBottom, ShaftTop, ShaftRadius, sides);
            AddCone(vertices, normals, uvs, triangles, ShaftBottom, TipY, ShaftRadius, sides);
            AddTorus(vertices, normals, uvs, triangles, new Vector3(0f, EyeY, 0f), EyeMajor, EyeTube, 8, 6);

            var mesh = new Mesh { name = "Needle" };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            return mesh;
        }

        static void AddCylinder(
            List<Vector3> vertices,
            List<Vector3> normals,
            List<Vector2> uvs,
            List<int> triangles,
            float y0,
            float y1,
            float radius,
            int sides)
        {
            int start = vertices.Count;
            for (int i = 0; i <= sides; i++)
            {
                float t = i / (float)sides;
                float a = t * Mathf.PI * 2f;
                float x = Mathf.Cos(a) * radius;
                float z = Mathf.Sin(a) * radius;
                Vector3 radial = new Vector3(x, 0f, z).normalized;
                vertices.Add(new Vector3(x, y0, z));
                normals.Add(radial);
                uvs.Add(new Vector2(t, 0f));
                vertices.Add(new Vector3(x, y1, z));
                normals.Add(radial);
                uvs.Add(new Vector2(t, 1f));
            }

            for (int i = 0; i < sides; i++)
            {
                int i0 = start + i * 2;
                int i1 = i0 + 1;
                int i2 = i0 + 2;
                int i3 = i0 + 3;
                triangles.Add(i0);
                triangles.Add(i2);
                triangles.Add(i1);
                triangles.Add(i1);
                triangles.Add(i2);
                triangles.Add(i3);
            }
        }

        static void AddCone(
            List<Vector3> vertices,
            List<Vector3> normals,
            List<Vector2> uvs,
            List<int> triangles,
            float yBase,
            float yApex,
            float radius,
            int sides)
        {
            int start = vertices.Count;
            Vector3 apex = new Vector3(0f, yApex, 0f);
            for (int i = 0; i <= sides; i++)
            {
                float t = i / (float)sides;
                float a = t * Mathf.PI * 2f;
                Vector3 rim = new Vector3(Mathf.Cos(a) * radius, yBase, Mathf.Sin(a) * radius);
                Vector3 radial = new Vector3(rim.x, 0f, rim.z).normalized;
                Vector3 along = apex - rim;
                Vector3 n = Vector3.Cross(Vector3.Cross(along, radial), along).normalized;
                if (n.sqrMagnitude < 1e-8f)
                    n = radial;
                if (Vector3.Dot(n, radial) < 0f)
                    n = -n;

                vertices.Add(rim);
                normals.Add(n);
                uvs.Add(new Vector2(t, 1f));
                vertices.Add(apex);
                normals.Add(n);
                uvs.Add(new Vector2(t, 0f));
            }

            for (int i = 0; i < sides; i++)
            {
                int i0 = start + i * 2;
                int i1 = i0 + 1;
                int i2 = i0 + 2;
                triangles.Add(i0);
                triangles.Add(i2);
                triangles.Add(i1);
            }
        }

        static void AddTorus(
            List<Vector3> vertices,
            List<Vector3> normals,
            List<Vector2> uvs,
            List<int> triangles,
            Vector3 center,
            float major,
            float tube,
            int radialSegments,
            int tubeSegments)
        {
            int start = vertices.Count;
            for (int i = 0; i <= radialSegments; i++)
            {
                float u = i / (float)radialSegments * Mathf.PI * 2f;
                float cu = Mathf.Cos(u);
                float su = Mathf.Sin(u);
                for (int j = 0; j <= tubeSegments; j++)
                {
                    float v = j / (float)tubeSegments * Mathf.PI * 2f;
                    float cv = Mathf.Cos(v);
                    float sv = Mathf.Sin(v);
                    Vector3 offset = new Vector3(
                        (major + tube * cv) * cu,
                        (major + tube * cv) * su,
                        tube * sv);
                    vertices.Add(center + offset);
                    normals.Add(new Vector3(cv * cu, cv * su, sv));
                    uvs.Add(new Vector2(i / (float)radialSegments, j / (float)tubeSegments));
                }
            }

            int stride = tubeSegments + 1;
            for (int i = 0; i < radialSegments; i++)
            {
                for (int j = 0; j < tubeSegments; j++)
                {
                    int a = start + i * stride + j;
                    int b = a + 1;
                    int c = a + stride;
                    int d = c + 1;
                    triangles.Add(a);
                    triangles.Add(c);
                    triangles.Add(b);
                    triangles.Add(b);
                    triangles.Add(c);
                    triangles.Add(d);
                }
            }
        }
    }
}
