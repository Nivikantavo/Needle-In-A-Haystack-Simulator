using Needle.Haystack;
using NUnit.Framework;
using UnityEngine;

namespace Needle.Haystack.Editor.Tests
{
    public class StrawPickQueryTests
    {
        static StrawPickQuery QueryWith(Matrix4x4[] locals, byte[] flags)
        {
            var query = new StrawPickQuery(0.4f);
            query.Rebuild(locals, flags, locals.Length, Matrix4x4.identity);
            return query;
        }

        static Matrix4x4 StrawAt(Vector3 p)
        {
            return Matrix4x4.TRS(p, Quaternion.identity, new Vector3(0.04f, 0.4f, 0.04f));
        }

        [Test]
        public void TryPick_ChoosesNearestAlongRay()
        {
            var locals = new[]
            {
                StrawAt(new Vector3(2f, 0f, 0f)),
                StrawAt(new Vector3(1f, 0f, 0f))
            };
            var flags = new byte[2];
            StrawPickQuery query = QueryWith(locals, flags);

            bool hit = query.TryPick(
                new Ray(new Vector3(-1f, 0f, 0f), Vector3.right),
                8f,
                locals,
                flags,
                locals.Length,
                Matrix4x4.identity,
                1f,
                out int index,
                out float distance);

            Assert.IsTrue(hit);
            Assert.AreEqual(1, index);
            Assert.Less(distance, 2.1f);
        }

        [Test]
        public void TryPick_SkipsBuriedAndGrabbed()
        {
            var locals = new[]
            {
                StrawAt(new Vector3(1f, 0f, 0f)),
                StrawAt(new Vector3(2f, 0f, 0f))
            };
            var flags = new[] { (byte)StrawFlags.Buried, (byte)0 };
            StrawPickQuery query = QueryWith(locals, flags);

            Assert.IsTrue(query.TryPick(
                new Ray(new Vector3(-1f, 0f, 0f), Vector3.right),
                8f,
                locals,
                flags,
                locals.Length,
                Matrix4x4.identity,
                1f,
                out int index,
                out _));
            Assert.AreEqual(1, index);

            flags[1] = (byte)StrawFlags.Grabbed;
            query.Rebuild(locals, flags, locals.Length, Matrix4x4.identity);
            Assert.IsFalse(query.TryPick(
                new Ray(new Vector3(-1f, 0f, 0f), Vector3.right),
                8f,
                locals,
                flags,
                locals.Length,
                Matrix4x4.identity,
                1f,
                out _,
                out _));
        }

        [Test]
        public void TryPick_RespectsMaxDistance()
        {
            var locals = new[] { StrawAt(new Vector3(3f, 0f, 0f)) };
            var flags = new byte[1];
            StrawPickQuery query = QueryWith(locals, flags);
            var ray = new Ray(Vector3.zero, Vector3.right);

            Assert.IsFalse(query.TryPick(ray, 1.5f, locals, flags, 1, Matrix4x4.identity, 1f, out _, out _));
            Assert.IsTrue(query.TryPick(ray, 4f, locals, flags, 1, Matrix4x4.identity, 1f, out int index, out _));
            Assert.AreEqual(0, index);
        }

        [Test]
        public void SpatialHash_CollectsOnlyNearbyCell()
        {
            var hash = new StrawSpatialHash(0.4f);
            var locals = new[]
            {
                StrawAt(Vector3.zero),
                StrawAt(new Vector3(5f, 0f, 0f))
            };
            hash.Rebuild(locals, new byte[2], 2, Matrix4x4.identity);

            var results = new int[16];
            int n = hash.CollectAlongRay(new Vector3(-0.2f, 0f, 0f), Vector3.right, 0.5f, 0.2f, results);
            Assert.Greater(n, 0);
            bool foundNear = false;
            bool foundFar = false;
            for (int i = 0; i < n; i++)
            {
                if (results[i] == 0)
                    foundNear = true;
                if (results[i] == 1)
                    foundFar = true;
            }

            Assert.IsTrue(foundNear);
            Assert.IsFalse(foundFar);
        }
    }
}
