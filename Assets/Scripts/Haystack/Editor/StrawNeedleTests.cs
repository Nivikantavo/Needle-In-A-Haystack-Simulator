using Needle.Haystack;
using NUnit.Framework;
using UnityEngine;

namespace Needle.Haystack.Editor.Tests
{
    public class StrawNeedleTests
    {
        [Test]
        public void HasFlag_ReadsIsNeedleBit()
        {
            Assert.IsFalse(StrawNeedle.HasFlag((byte)StrawFlags.None));
            Assert.IsFalse(StrawNeedle.HasFlag((byte)StrawFlags.Buried));
            Assert.IsTrue(StrawNeedle.HasFlag((byte)StrawFlags.IsNeedle));
            Assert.IsTrue(StrawNeedle.HasFlag((byte)(StrawFlags.IsNeedle | StrawFlags.Grabbed)));
        }

        [Test]
        public void FindIndex_ReturnsTheNeedleSlot()
        {
            var flags = new byte[] { 0, 0, (byte)StrawFlags.IsNeedle, 0 };
            Assert.AreEqual(2, StrawNeedle.FindIndex(flags, flags.Length));
        }

        [Test]
        public void FindIndex_MissingNeedle_ReturnsMinusOne()
        {
            var flags = new byte[] { 0, (byte)StrawFlags.Buried };
            Assert.AreEqual(-1, StrawNeedle.FindIndex(flags, flags.Length));
            Assert.AreEqual(-1, StrawNeedle.FindIndex(null, 4));
        }

        [Test]
        public void StampInDepth_PicksAnInteriorStraw()
        {
            var locals = new Matrix4x4[5];
            var flags = new byte[5];
            locals[0] = Matrix4x4.TRS(new Vector3(1.9f, 0.05f, 0f), Quaternion.identity, Vector3.one);
            locals[1] = Matrix4x4.TRS(new Vector3(0f, 1f, 0f), Quaternion.identity, Vector3.one);
            locals[2] = Matrix4x4.TRS(new Vector3(1.8f, 1.8f, 0f), Quaternion.identity, Vector3.one);
            locals[3] = Matrix4x4.TRS(new Vector3(0.1f, 1.1f, 0f), Quaternion.identity, Vector3.one);
            locals[4] = Matrix4x4.TRS(new Vector3(-1.85f, 0.2f, 0f), Quaternion.identity, Vector3.one);

            int index = StrawNeedle.StampInDepth(locals, flags, 5, HaystackShape.Cylinder, 4f, 2f, 3);
            Assert.GreaterOrEqual(index, 0);
            Assert.IsTrue(StrawNeedle.HasFlag(flags[index]));

            float chosen = HaystackVolume.AxisNormalized(HaystackShape.Cylinder, 4f, 2f, locals[index].GetPosition());
            float rim = HaystackVolume.AxisNormalized(HaystackShape.Cylinder, 4f, 2f, locals[0].GetPosition());
            Assert.Less(chosen, StrawNeedle.AxisCoreFraction + 0.01f);
            Assert.Less(chosen, rim);
        }

        [Test]
        public void StampInDepth_SameSeed_SameIndex()
        {
            Straw[] a = HaystackLayout.Generate(new HaystackLayoutSettings
            {
                Shape = HaystackShape.Cylinder,
                Diameter = 4f,
                Height = 2f,
                StrawCount = 220,
                Seed = 11,
                StrawLength = 0.3f,
                StrawRadius = 0.012f
            });
            Straw[] b = HaystackLayout.Generate(new HaystackLayoutSettings
            {
                Shape = HaystackShape.Cylinder,
                Diameter = 4f,
                Height = 2f,
                StrawCount = 220,
                Seed = 11,
                StrawLength = 0.3f,
                StrawRadius = 0.012f
            });
            Assert.AreEqual(
                StrawNeedle.FindIndex(FlagsOf(a), a.Length),
                StrawNeedle.FindIndex(FlagsOf(b), b.Length));
        }

        static byte[] FlagsOf(Straw[] straws)
        {
            var flags = new byte[straws.Length];
            for (int i = 0; i < straws.Length; i++)
                flags[i] = straws[i].Flags;
            return flags;
        }
    }
}
