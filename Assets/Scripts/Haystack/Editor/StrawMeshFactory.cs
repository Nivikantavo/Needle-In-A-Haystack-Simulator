using UnityEngine;

namespace Needle.Haystack.Editor
{
    public static class StrawMeshFactory
    {
        public static Mesh CreateUnitCylinder(int sides = 6)
        {
            sides = Mathf.Clamp(sides, 6, 8);

            int sideVertCount = (sides + 1) * 2;
            int capVertCount = sides * 2;
            var vertices = new Vector3[sideVertCount + capVertCount];
            var normals = new Vector3[vertices.Length];
            var uvs = new Vector2[vertices.Length];

            int v = 0;
            for (int i = 0; i <= sides; i++)
            {
                float t = i / (float)sides;
                float a = t * Mathf.PI * 2f;
                float x = Mathf.Cos(a) * 0.5f;
                float z = Mathf.Sin(a) * 0.5f;
                Vector3 radial = new Vector3(x, 0f, z).normalized;

                vertices[v] = new Vector3(x, -0.5f, z);
                normals[v] = radial;
                uvs[v] = new Vector2(t, 0f);
                v++;

                vertices[v] = new Vector3(x, 0.5f, z);
                normals[v] = radial;
                uvs[v] = new Vector2(t, 1f);
                v++;
            }

            int bottomCapStart = v;
            for (int i = 0; i < sides; i++)
            {
                float a = i / (float)sides * Mathf.PI * 2f;
                vertices[v] = new Vector3(Mathf.Cos(a) * 0.5f, -0.5f, Mathf.Sin(a) * 0.5f);
                normals[v] = Vector3.down;
                uvs[v] = new Vector2(0.5f + Mathf.Cos(a) * 0.5f, 0.5f + Mathf.Sin(a) * 0.5f);
                v++;
            }

            int topCapStart = v;
            for (int i = 0; i < sides; i++)
            {
                float a = i / (float)sides * Mathf.PI * 2f;
                vertices[v] = new Vector3(Mathf.Cos(a) * 0.5f, 0.5f, Mathf.Sin(a) * 0.5f);
                normals[v] = Vector3.up;
                uvs[v] = new Vector2(0.5f + Mathf.Cos(a) * 0.5f, 0.5f + Mathf.Sin(a) * 0.5f);
                v++;
            }

            int triCount = sides * 2 + (sides - 2) * 2;
            var triangles = new int[triCount * 3];
            int tIndex = 0;
            for (int i = 0; i < sides; i++)
            {
                int i0 = i * 2;
                int i1 = i0 + 1;
                int i2 = i0 + 2;
                int i3 = i0 + 3;
                triangles[tIndex++] = i0;
                triangles[tIndex++] = i2;
                triangles[tIndex++] = i1;
                triangles[tIndex++] = i1;
                triangles[tIndex++] = i2;
                triangles[tIndex++] = i3;
            }

            for (int i = 1; i < sides - 1; i++)
            {
                triangles[tIndex++] = bottomCapStart;
                triangles[tIndex++] = bottomCapStart + i + 1;
                triangles[tIndex++] = bottomCapStart + i;
            }

            for (int i = 1; i < sides - 1; i++)
            {
                triangles[tIndex++] = topCapStart;
                triangles[tIndex++] = topCapStart + i;
                triangles[tIndex++] = topCapStart + i + 1;
            }

            var mesh = new Mesh { name = "Straw" };
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            return mesh;
        }
    }
}
