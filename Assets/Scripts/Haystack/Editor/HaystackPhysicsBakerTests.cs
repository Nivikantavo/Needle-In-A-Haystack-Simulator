using Needle.Haystack;
using NUnit.Framework;
using UnityEngine;

namespace Needle.Haystack.Editor.Tests
{
    public class HaystackPhysicsBakerTests
    {
        static HaystackLayoutSettings Settings(int count)
        {
            return new HaystackLayoutSettings
            {
                Shape = HaystackShape.Cylinder,
                Diameter = 1.6f,
                Height = 1.1f,
                StrawCount = count,
                Seed = 7,
                StrawLength = 0.22f,
                StrawRadius = 0.02f,
                HorizontalFraction = 0.8f
            };
        }

        [Test]
        public void Bake_PreservesCountAndNeedle()
        {
            var settings = Settings(72);
            Straw[] baked = HaystackPhysicsBaker.Bake(settings, 1f);

            Assert.AreEqual(settings.StrawCount, baked.Length);
            int needles = 0;
            for (int i = 0; i < baked.Length; i++)
            {
                if ((baked[i].Flags & (byte)StrawFlags.IsNeedle) != 0)
                    needles++;
                Assert.IsTrue(IsFinite(baked[i].LocalToWorld), $"straw {i} has a non-finite matrix");
            }

            Assert.AreEqual(1, needles);
        }

        [Test]
        public void Bake_KeepsAThreeDimensionalPile()
        {
            var settings = Settings(80);
            Straw[] baked = HaystackPhysicsBaker.Bake(settings, 1.2f);

            float minY = float.PositiveInfinity;
            float maxY = float.NegativeInfinity;
            float sumY = 0f;
            int upper = 0;
            for (int i = 0; i < baked.Length; i++)
            {
                float y = baked[i].LocalToWorld.GetPosition().y;
                minY = Mathf.Min(minY, y);
                maxY = Mathf.Max(maxY, y);
                sumY += y;
                if (y > settings.Height * 0.35f)
                    upper++;
            }

            float meanY = sumY / baked.Length;
            Assert.Greater(maxY, settings.Height * 0.4f, "pile collapsed toward the floor");
            Assert.Greater(meanY, settings.Height * 0.18f, "mean height looks like a pancake");
            Assert.Greater(upper, baked.Length / 8, "too few straws remain in the upper pile");
            Assert.Greater(minY, -0.05f);
        }

        static bool IsFinite(Matrix4x4 m)
        {
            for (int i = 0; i < 16; i++)
            {
                float v = m[i];
                if (float.IsNaN(v) || float.IsInfinity(v))
                    return false;
            }

            return true;
        }
    }
}
