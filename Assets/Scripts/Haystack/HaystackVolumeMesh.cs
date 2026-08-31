using System.Collections.Generic;
using UnityEngine;

namespace Needle.Haystack
{
    public static class HaystackVolumeMesh
    {
        const int CylinderSides = 16;
        const int HemisphereLon = 16;
        const int HemisphereLat = 8;

        public static Mesh Create(HaystackShape shape, float diameter, float height)
        {
            diameter = Mathf.Max(0.01f, diameter);
            height = Mathf.Max(0.01f, height);

            switch (shape)
            {
                case HaystackShape.Cylinder:
                    return CreateCylinder(diameter * 0.5f, height);
                case HaystackShape.Hemisphere:
                    return CreateHemisphere(diameter * 0.5f, height);
                case HaystackShape.Pyramid:
                    return CreatePyramid(diameter * 0.5f, height);
                default:
                    return null;
            }
        }

        static Mesh CreateCylinder(float radius, float height)
        {
            int sides = CylinderSides;
            var vertices = new Vector3[sides * 2];
            for (int i = 0; i < sides; i++)
            {
                float a = i / (float)sides * Mathf.PI * 2f;
                float x = Mathf.Cos(a) * radius;
                float z = Mathf.Sin(a) * radius;
                vertices[i] = new Vector3(x, 0f, z);
                vertices[i + sides] = new Vector3(x, height, z);
            }

            var triangles = new List<int>(sides * 12);
            for (int i = 0; i < sides; i++)
            {
                int next = (i + 1) % sides;
                int b0 = i;
                int b1 = next;
                int t0 = i + sides;
                int t1 = next + sides;
                triangles.Add(b0);
                triangles.Add(t0);
                triangles.Add(b1);
                triangles.Add(b1);
                triangles.Add(t0);
                triangles.Add(t1);
            }

            for (int i = 1; i < sides - 1; i++)
            {
                triangles.Add(0);
                triangles.Add(i);
                triangles.Add(i + 1);
                triangles.Add(sides);
                triangles.Add(sides + i + 1);
                triangles.Add(sides + i);
            }

            return BuildMesh("HayPileCylinder", vertices, triangles.ToArray());
        }

        static Mesh CreateHemisphere(float radius, float height)
        {
            int lon = HemisphereLon;
            int lat = HemisphereLat;
            var vertices = new List<Vector3>((lat + 1) * (lon + 1));
            for (int y = 0; y <= lat; y++)
            {
                float v = y / (float)lat;
                float phi = v * Mathf.PI * 0.5f;
                float ringY = height * Mathf.Cos(phi);
                float ringR = radius * Mathf.Sin(phi);
                for (int x = 0; x <= lon; x++)
                {
                    float theta = x / (float)lon * Mathf.PI * 2f;
                    vertices.Add(new Vector3(Mathf.Cos(theta) * ringR, ringY, Mathf.Sin(theta) * ringR));
                }
            }

            var triangles = new List<int>();
            int stride = lon + 1;
            for (int y = 0; y < lat; y++)
            {
                for (int x = 0; x < lon; x++)
                {
                    int i0 = y * stride + x;
                    int i1 = i0 + 1;
                    int i2 = i0 + stride;
                    int i3 = i2 + 1;
                    triangles.Add(i0);
                    triangles.Add(i2);
                    triangles.Add(i1);
                    triangles.Add(i1);
                    triangles.Add(i2);
                    triangles.Add(i3);
                }
            }

            int baseStart = lat * stride;
            for (int x = 1; x < lon; x++)
            {
                triangles.Add(baseStart);
                triangles.Add(baseStart + x + 1);
                triangles.Add(baseStart + x);
            }

            return BuildMesh("HayPileHemisphere", vertices, triangles);
        }

        static Mesh CreatePyramid(float half, float height)
        {
            var vertices = new[]
            {
                new Vector3(0f, height, 0f),
                new Vector3(-half, 0f, -half),
                new Vector3(half, 0f, -half),
                new Vector3(half, 0f, half),
                new Vector3(-half, 0f, half)
            };

            var triangles = new[]
            {
                0, 2, 1,
                0, 3, 2,
                0, 4, 3,
                0, 1, 4,
                1, 2, 3,
                1, 3, 4
            };

            return BuildMesh("HayPilePyramid", vertices, triangles);
        }

        static Mesh BuildMesh(string name, Vector3[] vertices, int[] triangles)
        {
            var mesh = new Mesh { name = name, hideFlags = HideFlags.DontSave };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            return mesh;
        }

        static Mesh BuildMesh(string name, List<Vector3> vertices, List<int> triangles)
        {
            var mesh = new Mesh { name = name, hideFlags = HideFlags.DontSave };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            return mesh;
        }
    }
}
