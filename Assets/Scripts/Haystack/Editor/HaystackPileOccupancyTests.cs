using Needle.Haystack;
using NUnit.Framework;
using UnityEngine;

namespace Needle.Haystack.Editor.Tests
{
    public class HaystackPileOccupancyTests
    {
        const float Diameter = 4f;
        const float Height = 2.2f;
        const float Padding = 0.175f;

        [Test]
        public void FullPile_Height_CoversHighestStraw()
        {
            var matrices = new[]
            {
                Matrix4x4.TRS(new Vector3(0f, 0.4f, 0f), Quaternion.identity, Vector3.one),
                Matrix4x4.TRS(new Vector3(0.2f, 2.0f, 0f), Quaternion.identity, Vector3.one)
            };
            var flags = new byte[2];

            HaystackPileOccupancyResult result = Measure(matrices, flags);
            Assert.AreEqual(2, result.Remaining);
            Assert.AreEqual(
                HaystackPileOccupancy.QuantizeDiameter(0.2f * 2f * HaystackPileOccupancy.HorizontalSlack, Diameter),
                result.Diameter,
                0.001f);
            Assert.AreEqual(Height, result.Height, 0.001f);
        }

        [Test]
        public void RimStraw_KeepsAuthoredDiameter()
        {
            var matrices = new[]
            {
                Matrix4x4.TRS(new Vector3(0f, 0.4f, 0f), Quaternion.identity, Vector3.one),
                Matrix4x4.TRS(new Vector3(2f, 1.0f, 0f), Quaternion.identity, Vector3.one)
            };
            var flags = new byte[2];

            HaystackPileOccupancyResult result = Measure(matrices, flags);
            Assert.AreEqual(Diameter, result.Diameter, 0.001f);
        }

        [Test]
        public void GrabOuter_ShrinksDiameter()
        {
            var matrices = new[]
            {
                Matrix4x4.TRS(new Vector3(0.4f, 0.5f, 0f), Quaternion.identity, Vector3.one),
                Matrix4x4.TRS(new Vector3(2f, 0.5f, 0f), Quaternion.identity, Vector3.one)
            };

            HaystackPileOccupancyResult full = Measure(matrices, new byte[2]);
            HaystackPileOccupancyResult after = Measure(matrices, new[] { (byte)0, (byte)StrawFlags.Grabbed });

            Assert.AreEqual(Diameter, full.Diameter, 0.001f);
            Assert.AreEqual(1, after.Remaining);
            Assert.Less(after.Diameter, full.Diameter);
            Assert.AreEqual(
                HaystackPileOccupancy.QuantizeDiameter(0.4f * 2f * HaystackPileOccupancy.HorizontalSlack, Diameter),
                after.Diameter,
                0.001f);
        }

        [Test]
        public void GrabInner_KeepsDiameter()
        {
            var matrices = new[]
            {
                Matrix4x4.TRS(new Vector3(0.2f, 0.5f, 0f), Quaternion.identity, Vector3.one),
                Matrix4x4.TRS(new Vector3(2f, 0.5f, 0f), Quaternion.identity, Vector3.one)
            };

            HaystackPileOccupancyResult full = Measure(matrices, new byte[2]);
            HaystackPileOccupancyResult after = Measure(matrices, new[] { (byte)StrawFlags.Grabbed, (byte)0 });

            Assert.AreEqual(1, after.Remaining);
            Assert.AreEqual(full.Diameter, after.Diameter, 0.001f);
        }

        [Test]
        public void QuantizeDiameter_CeilsAndCaps()
        {
            Assert.AreEqual(0f, HaystackPileOccupancy.QuantizeDiameter(0f, Diameter));
            Assert.AreEqual(0.1f, HaystackPileOccupancy.QuantizeDiameter(0.01f, Diameter), 0.0001f);
            Assert.AreEqual(1.2f, HaystackPileOccupancy.QuantizeDiameter(1.11f, Diameter), 0.0001f);
            Assert.AreEqual(Diameter, HaystackPileOccupancy.QuantizeDiameter(8f, Diameter));
        }

        [Test]
        public void GrabHighest_LowersHeight()
        {
            var matrices = new[]
            {
                Matrix4x4.TRS(new Vector3(0f, 0.5f, 0f), Quaternion.identity, Vector3.one),
                Matrix4x4.TRS(new Vector3(0f, 1.8f, 0f), Quaternion.identity, Vector3.one)
            };
            var flags = new[] { (byte)0, (byte)StrawFlags.Grabbed };

            HaystackPileOccupancyResult full = Measure(matrices, new byte[2]);
            HaystackPileOccupancyResult after = Measure(matrices, flags);

            Assert.AreEqual(1, after.Remaining);
            Assert.Less(after.Height, full.Height);
            Assert.AreEqual(
                HaystackPileOccupancy.QuantizeHeight(0.5f + Padding, Height),
                after.Height,
                0.001f);
        }

        [Test]
        public void GrabLowest_KeepsHeight()
        {
            var matrices = new[]
            {
                Matrix4x4.TRS(new Vector3(0f, 0.4f, 0f), Quaternion.identity, Vector3.one),
                Matrix4x4.TRS(new Vector3(0f, 1.9f, 0f), Quaternion.identity, Vector3.one)
            };
            var flags = new[] { (byte)StrawFlags.Grabbed, (byte)0 };

            HaystackPileOccupancyResult full = Measure(matrices, new byte[2]);
            HaystackPileOccupancyResult after = Measure(matrices, flags);

            Assert.AreEqual(1, after.Remaining);
            Assert.AreEqual(full.Height, after.Height, 0.001f);
        }

        [Test]
        public void ThrownOutsideVolume_DoesNotKeepHeight()
        {
            var matrices = new[]
            {
                Matrix4x4.TRS(new Vector3(0f, 0.4f, 0f), Quaternion.identity, Vector3.one),
                Matrix4x4.TRS(new Vector3(8f, 1.9f, 0f), Quaternion.identity, Vector3.one)
            };
            var flags = new byte[2];

            HaystackPileOccupancyResult result = Measure(matrices, flags);
            Assert.AreEqual(1, result.Remaining);
            Assert.AreEqual(
                HaystackPileOccupancy.QuantizeHeight(0.4f + Padding, Height),
                result.Height,
                0.001f);
        }

        [Test]
        public void Discarded_IsIgnored()
        {
            var matrices = new[]
            {
                Matrix4x4.TRS(new Vector3(0f, 0.4f, 0f), Quaternion.identity, Vector3.one),
                Matrix4x4.TRS(new Vector3(0f, 1.9f, 0f), Quaternion.identity, Vector3.one)
            };
            var flags = new[] { (byte)0, (byte)StrawFlags.Discarded };

            HaystackPileOccupancyResult result = Measure(matrices, flags);
            Assert.AreEqual(1, result.Remaining);
            Assert.Less(result.Height, Height);
        }

        [Test]
        public void AllRemoved_IsEmpty()
        {
            var matrices = new[]
            {
                Matrix4x4.TRS(new Vector3(0f, 1f, 0f), Quaternion.identity, Vector3.one)
            };
            var flags = new[] { (byte)StrawFlags.Grabbed };

            HaystackPileOccupancyResult result = Measure(matrices, flags);
            Assert.IsTrue(result.IsEmpty);
            Assert.AreEqual(0, result.Remaining);
            Assert.AreEqual(0f, result.Height);
        }

        [Test]
        public void QuantizeHeight_CeilsAndCaps()
        {
            Assert.AreEqual(0f, HaystackPileOccupancy.QuantizeHeight(0f, Height));
            Assert.AreEqual(0.05f, HaystackPileOccupancy.QuantizeHeight(0.01f, Height), 0.0001f);
            Assert.AreEqual(1.15f, HaystackPileOccupancy.QuantizeHeight(1.11f, Height), 0.0001f);
            Assert.AreEqual(Height, HaystackPileOccupancy.QuantizeHeight(3f, Height));
        }

        static HaystackPileOccupancyResult Measure(Matrix4x4[] matrices, byte[] flags)
        {
            return HaystackPileOccupancy.Measure(
                HaystackShape.Cylinder,
                Diameter,
                Height,
                matrices,
                flags,
                matrices.Length,
                Padding);
        }
    }
}
