using Needle.Haystack;
using NUnit.Framework;
using UnityEngine;

namespace Needle.Haystack.Editor.Tests
{
    public class StrawCapsuleTests
    {
        static Matrix4x4 VerticalStraw(Vector3 center, float radius, float length)
        {
            return Matrix4x4.TRS(center, Quaternion.identity, new Vector3(radius * 2f, length, radius * 2f));
        }

        [Test]
        public void Raycast_HitsSideOfVerticalStraw()
        {
            Matrix4x4 m = VerticalStraw(Vector3.zero, 0.02f, 0.4f);
            bool hit = StrawCapsule.Raycast(
                new Vector3(-1f, 0f, 0f),
                Vector3.right,
                2f,
                m,
                1f,
                out float distance);

            Assert.IsTrue(hit);
            Assert.AreEqual(0.98f, distance, 0.01f);
        }

        [Test]
        public void Raycast_MissesWhenOffsetBeyondRadius()
        {
            Matrix4x4 m = VerticalStraw(Vector3.zero, 0.02f, 0.4f);
            bool hit = StrawCapsule.Raycast(
                new Vector3(-1f, 0f, 0.08f),
                Vector3.right,
                2f,
                m,
                1f,
                out _);

            Assert.IsFalse(hit);
        }

        [Test]
        public void Raycast_RadiusMultiplier_MakesThinStrawEasierToHit()
        {
            Matrix4x4 m = VerticalStraw(Vector3.zero, 0.02f, 0.4f);
            Vector3 origin = new Vector3(-1f, 0f, 0.05f);
            Assert.IsFalse(StrawCapsule.Raycast(origin, Vector3.right, 2f, m, 1f, out _));
            Assert.IsTrue(StrawCapsule.Raycast(origin, Vector3.right, 2f, m, 3.5f, out _));
        }

        [Test]
        public void FromMatrix_ReadsLengthAndRadius()
        {
            Matrix4x4 m = VerticalStraw(new Vector3(1f, 2f, 3f), 0.016f, 0.45f);
            StrawCapsule.FromMatrix(m, out Vector3 a, out Vector3 b, out float radius);
            Assert.AreEqual(0.016f, radius, 0.0001f);
            Assert.AreEqual(0.45f, (b - a).magnitude, 0.0001f);
            Assert.AreEqual(new Vector3(1f, 2f, 3f), (a + b) * 0.5f);
        }
    }
}
