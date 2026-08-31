using NUnit.Framework;
using UnityEngine;

namespace Needle.Haystack.Editor.Tests
{
    public class NeedleMeshFactoryTests
    {
        [Test]
        public void Create_BuildsAFiniteNamedMesh()
        {
            Mesh mesh = NeedleMeshFactory.Create();
            Assert.AreEqual("Needle", mesh.name);
            Assert.Greater(mesh.vertexCount, 24);
            Assert.Greater(mesh.triangles.Length, 36);
            Assert.IsTrue(IsFinite(mesh.bounds));
            Object.DestroyImmediate(mesh);
        }

        static bool IsFinite(Bounds bounds)
        {
            Vector3 c = bounds.center;
            Vector3 e = bounds.extents;
            return IsFinite(c) && IsFinite(e) && e.x > 0f && e.y > 0f && e.z > 0f;
        }

        static bool IsFinite(Vector3 v)
        {
            return !(float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z) ||
                     float.IsInfinity(v.x) || float.IsInfinity(v.y) || float.IsInfinity(v.z));
        }
    }
}
