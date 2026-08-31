using Needle.Haystack;
using NUnit.Framework;
using UnityEngine;

namespace Needle.Haystack.Editor.Tests
{
    public class HaystackVolumeMeshTests
    {
        [Test]
        public void Cylinder_Bounds_MatchVolume()
        {
            Mesh mesh = HaystackVolumeMesh.Create(HaystackShape.Cylinder, 4f, 2f);
            Assert.IsNotNull(mesh);
            Assert.Greater(mesh.vertexCount, 8);
            Bounds b = mesh.bounds;
            Assert.AreEqual(2f, b.size.y, 0.05f);
            Assert.AreEqual(4f, b.size.x, 0.15f);
            Assert.AreEqual(4f, b.size.z, 0.15f);
            Object.DestroyImmediate(mesh);
        }

        [Test]
        public void Hemisphere_StaysInUpperHalf()
        {
            Mesh mesh = HaystackVolumeMesh.Create(HaystackShape.Hemisphere, 4f, 2f);
            Assert.IsNotNull(mesh);
            Vector3[] vertices = mesh.vertices;
            float minY = float.PositiveInfinity;
            float maxY = float.NegativeInfinity;
            for (int i = 0; i < vertices.Length; i++)
            {
                minY = Mathf.Min(minY, vertices[i].y);
                maxY = Mathf.Max(maxY, vertices[i].y);
            }

            Assert.GreaterOrEqual(minY, -0.01f);
            Assert.AreEqual(2f, maxY, 0.05f);
            Object.DestroyImmediate(mesh);
        }

        [Test]
        public void Pyramid_HasFiveVertices()
        {
            Mesh mesh = HaystackVolumeMesh.Create(HaystackShape.Pyramid, 4f, 2f);
            Assert.AreEqual(5, mesh.vertexCount);
            Object.DestroyImmediate(mesh);
        }

        [Test]
        public void Box_HasNoMesh()
        {
            Assert.IsNull(HaystackVolumeMesh.Create(HaystackShape.Box, 4f, 2f));
        }
    }
}
