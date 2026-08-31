using Needle.Haystack;
using NUnit.Framework;
using UnityEngine;

namespace Needle.Haystack.Editor.Tests
{
    public class HaystackLayoutTests
    {
        static HaystackLayoutSettings Settings(HaystackShape shape, int count = 250)
        {
            return new HaystackLayoutSettings
            {
                Shape = shape,
                Diameter = 4f,
                Height = 2f,
                StrawCount = count,
                Seed = 42,
                StrawLength = 0.35f,
                StrawRadius = 0.012f,
                HorizontalFraction = 0.85f
            };
        }

        [Test]
        public void Generate_Count_MatchesRequest_ForEveryShape()
        {
            foreach (HaystackShape shape in System.Enum.GetValues(typeof(HaystackShape)))
            {
                Straw[] straws = HaystackLayout.Generate(Settings(shape, 180));
                Assert.AreEqual(180, straws.Length, shape.ToString());
            }
        }

        [Test]
        public void Generate_Centers_AreInsideVolume()
        {
            foreach (HaystackShape shape in System.Enum.GetValues(typeof(HaystackShape)))
            {
                var settings = Settings(shape, 200);
                Straw[] straws = HaystackLayout.Generate(settings);
                for (int i = 0; i < straws.Length; i++)
                {
                    Vector3 p = straws[i].LocalToWorld.GetPosition();
                    Assert.IsTrue(
                        HaystackVolume.Contains(settings.Shape, settings.Diameter, settings.Height, p),
                        $"{shape} straw {i} at {p}");
                    Assert.IsTrue(IsFinite(straws[i].LocalToWorld), $"{shape} straw {i} has non-finite matrix");
                }
            }
        }

        [Test]
        public void Generate_MarksExactlyOneNeedle()
        {
            Straw[] straws = HaystackLayout.Generate(Settings(HaystackShape.Cylinder, 300));
            int needles = 0;
            for (int i = 0; i < straws.Length; i++)
            {
                if ((straws[i].Flags & (byte)StrawFlags.IsNeedle) != 0)
                    needles++;
            }

            Assert.AreEqual(1, needles);
        }

        [Test]
        public void Generate_Needle_SitsNearTheAxis_ForEveryShape()
        {
            foreach (HaystackShape shape in System.Enum.GetValues(typeof(HaystackShape)))
            {
                var settings = Settings(shape, 400);
                Straw[] straws = HaystackLayout.Generate(settings);
                int needle = StrawNeedle.FindIndex(FlagsOf(straws), straws.Length);
                Assert.GreaterOrEqual(needle, 0, shape.ToString());

                Vector3 p = straws[needle].LocalToWorld.GetPosition();
                float needleAxis = HaystackVolume.AxisNormalized(
                    settings.Shape, settings.Diameter, settings.Height, p);
                float meanAxis = 0f;
                for (int i = 0; i < straws.Length; i++)
                {
                    meanAxis += HaystackVolume.AxisNormalized(
                        settings.Shape,
                        settings.Diameter,
                        settings.Height,
                        straws[i].LocalToWorld.GetPosition());
                }

                meanAxis /= straws.Length;
                float y01 = p.y / settings.Height;
                Assert.Greater(y01, StrawNeedle.VerticalPadding - 0.02f, shape.ToString());
                Assert.Less(y01, 1f - StrawNeedle.VerticalPadding + 0.02f, shape.ToString());
                Assert.Less(needleAxis, 0.35f, $"{shape} axis={needleAxis}");
                Assert.Less(needleAxis, meanAxis * 0.55f, shape.ToString());
            }
        }

        [Test]
        public void Generate_SameSeed_SameNeedleIndex()
        {
            Straw[] a = HaystackLayout.Generate(Settings(HaystackShape.Cylinder, 300));
            Straw[] b = HaystackLayout.Generate(Settings(HaystackShape.Cylinder, 300));
            Assert.AreEqual(StrawNeedle.FindIndex(FlagsOf(a), a.Length), StrawNeedle.FindIndex(FlagsOf(b), b.Length));
        }

        [Test]
        public void Generate_MostStraws_LieMostlyHorizontal()
        {
            Straw[] straws = HaystackLayout.Generate(Settings(HaystackShape.Cylinder, 400));
            int flat = 0;
            int upright = 0;
            for (int i = 0; i < straws.Length; i++)
            {
                Vector3 axis = ((Vector3)straws[i].LocalToWorld.GetColumn(1)).normalized;
                float vertical = Mathf.Abs(axis.y);
                if (vertical < 0.45f)
                    flat++;
                else if (vertical > 0.75f)
                    upright++;
            }

            Assert.Greater(flat, straws.Length * 0.65f);
            Assert.Greater(upright, 0);
            Assert.Less(upright, straws.Length * 0.35f);
        }

        [Test]
        public void Generate_BottomFifth_IsDenserThanUniform()
        {
            var settings = Settings(HaystackShape.Cylinder, 800);
            Straw[] straws = HaystackLayout.Generate(settings);
            float baseTop = settings.Height * 0.2f;
            int inBase = 0;
            for (int i = 0; i < straws.Length; i++)
            {
                if (straws[i].LocalToWorld.GetPosition().y < baseTop)
                    inBase++;
            }

            Assert.Greater(inBase, straws.Length * 0.28f);
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

        static byte[] FlagsOf(Straw[] straws)
        {
            var flags = new byte[straws.Length];
            for (int i = 0; i < straws.Length; i++)
                flags[i] = straws[i].Flags;
            return flags;
        }
    }
}
