using Needle.Haystack;
using NUnit.Framework;
using UnityEngine;

namespace Needle.Haystack.Editor.Tests
{
    public class HaystackDiscardTests
    {
        [Test]
        public void MarkExposed_TakesRequestedCount()
        {
            var flags = new byte[] { 0, 0, 0, 0 };

            int marked = HaystackDiscard.MarkExposed(flags, flags.Length, 2);

            Assert.AreEqual(2, marked);
            Assert.AreEqual((byte)StrawFlags.Discarded, flags[0]);
            Assert.AreEqual((byte)StrawFlags.Discarded, flags[1]);
            Assert.AreEqual(0, flags[2]);
            Assert.AreEqual(0, flags[3]);
        }

        [Test]
        public void MarkExposed_SkipsBuriedGrabbedDiscardedAndNeedle()
        {
            var flags = new[]
            {
                (byte)StrawFlags.Buried,
                (byte)StrawFlags.Grabbed,
                (byte)StrawFlags.Discarded,
                (byte)StrawFlags.IsNeedle,
                (byte)0
            };

            int marked = HaystackDiscard.MarkExposed(flags, flags.Length, 8);

            Assert.AreEqual(1, marked);
            Assert.AreEqual((byte)StrawFlags.Buried, flags[0]);
            Assert.AreEqual((byte)StrawFlags.Grabbed, flags[1]);
            Assert.AreEqual((byte)StrawFlags.Discarded, flags[2]);
            Assert.AreEqual((byte)StrawFlags.IsNeedle, flags[3]);
            Assert.AreEqual((byte)StrawFlags.Discarded, flags[4]);
        }

        [Test]
        public void MarkExposed_PeelThenNewShellCanBeDiscarded()
        {
            const int side = 7;
            const float spacing = 0.2f;
            var matrices = new Matrix4x4[side * side * side];
            var flags = new byte[matrices.Length];
            int count = 0;
            int half = side / 2;
            for (int y = 0; y < side; y++)
            {
                for (int z = 0; z < side; z++)
                {
                    for (int x = 0; x < side; x++)
                    {
                        matrices[count] = Matrix4x4.TRS(
                            new Vector3((x - half) * spacing, 0.1f + y * spacing, (z - half) * spacing),
                            Quaternion.identity,
                            new Vector3(0.03f, 0.08f, 0.03f));
                        count++;
                    }
                }
            }

            var mask = new StrawSurfaceMask();
            mask.Apply(HaystackShape.Cylinder, 4f, 2.2f, matrices, flags, count, 0.25f);
            Assert.Greater(mask.BuriedCount, 0);

            int first = HaystackDiscard.MarkExposed(flags, count, count);
            Assert.AreEqual(mask.ExposedCount, first);

            mask.Apply(HaystackShape.Cylinder, 4f, 2.2f, matrices, flags, count, 0.25f);
            Assert.Greater(mask.ExposedCount, 0);

            int second = HaystackDiscard.MarkExposed(flags, count, count);
            Assert.Greater(second, 0);
            Assert.AreEqual(mask.ExposedCount, second);
        }

        [Test]
        public void MarkExposed_ZeroWantedOrEmpty_ReturnsZero()
        {
            var flags = new byte[] { 0, 0 };
            Assert.AreEqual(0, HaystackDiscard.MarkExposed(flags, flags.Length, 0));
            Assert.AreEqual(0, HaystackDiscard.MarkExposed(null, 2, 1));
            Assert.AreEqual(0, HaystackDiscard.MarkExposed(flags, 0, 1));
        }
    }
}
