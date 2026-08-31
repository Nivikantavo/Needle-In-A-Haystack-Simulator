using Needle.Haystack;
using NUnit.Framework;
using UnityEngine;

namespace Needle.Haystack.Editor.Tests
{
    public class StrawSurfaceMaskTests
    {
        const float Diameter = 4f;
        const float Height = 2.2f;
        const float Cell = 0.25f;

        static Matrix4x4 StrawAt(Vector3 p)
        {
            return Matrix4x4.TRS(p, Quaternion.identity, new Vector3(0.03f, 0.08f, 0.03f));
        }

        static bool Buried(byte flag)
        {
            return (flag & (byte)StrawFlags.Buried) != 0;
        }

        static int BuildCube(int side, float spacing, float y0, Matrix4x4[] matrices, out int center, out int top, out int sideFace, out int bottomCenter)
        {
            int half = side / 2;
            center = top = sideFace = bottomCenter = -1;
            int written = 0;
            for (int y = 0; y < side; y++)
            {
                for (int z = 0; z < side; z++)
                {
                    for (int x = 0; x < side; x++)
                    {
                        matrices[written] = StrawAt(new Vector3((x - half) * spacing, y0 + y * spacing, (z - half) * spacing));
                        if (x == half && y == half && z == half)
                            center = written;
                        if (x == half && y == side - 1 && z == half)
                            top = written;
                        if (x == 0 && y == half && z == half)
                            sideFace = written;
                        if (x == half && y == 0 && z == half)
                            bottomCenter = written;
                        written++;
                    }
                }
            }

            return written;
        }

        [Test]
        public void Cube_CenterBuried_FacesExposed()
        {
            const int side = 7;
            const float spacing = 0.2f;
            var matrices = new Matrix4x4[side * side * side];
            var flags = new byte[matrices.Length];
            int count = BuildCube(side, spacing, 0.1f, matrices, out int center, out int top, out int sideFace, out int bottomCenter);

            var mask = new StrawSurfaceMask();
            mask.Apply(HaystackShape.Cylinder, Diameter, Height, matrices, flags, count, Cell);

            Assert.Greater(mask.BuriedCount, 0);
            Assert.Greater(mask.ExposedCount, 0);
            Assert.IsTrue(Buried(flags[center]), "interior center should be buried");
            Assert.IsFalse(Buried(flags[top]), "top face should be pickable");
            Assert.IsFalse(Buried(flags[sideFace]), "side face should be pickable");
            Assert.IsTrue(Buried(flags[bottomCenter]), "ground contact should stay buried");
        }

        [Test]
        public void GrabExposedShell_RevealsNewSurface()
        {
            const int side = 7;
            const float spacing = 0.2f;
            var matrices = new Matrix4x4[side * side * side];
            var flags = new byte[matrices.Length];
            int count = BuildCube(side, spacing, 0.1f, matrices, out _, out _, out _, out _);

            var mask = new StrawSurfaceMask();
            mask.Apply(HaystackShape.Cylinder, Diameter, Height, matrices, flags, count, Cell);
            int buriedBefore = mask.BuriedCount;
            Assert.Greater(buriedBefore, 0);

            var wasBuried = new bool[count];
            for (int i = 0; i < count; i++)
            {
                if (Buried(flags[i]))
                    wasBuried[i] = true;
                else
                    flags[i] = (byte)StrawFlags.Grabbed;
            }

            mask.Apply(HaystackShape.Cylinder, Diameter, Height, matrices, flags, count, Cell);
            Assert.Greater(mask.ExposedCount, 0, "peeling the shell must expose a new surface");
            Assert.Less(mask.BuriedCount, buriedBefore);

            int newlyExposed = 0;
            for (int i = 0; i < count; i++)
            {
                if (wasBuried[i] && !Buried(flags[i]) && (flags[i] & (byte)StrawFlags.Grabbed) == 0)
                    newlyExposed++;
            }

            Assert.Greater(newlyExposed, 0, "previously buried straws on the new shell must become pickable");
        }

        [Test]
        public void ThrownOutside_IsNotBuried()
        {
            var matrices = new[]
            {
                StrawAt(new Vector3(0f, 0.4f, 0f)),
                StrawAt(new Vector3(8f, 0.2f, 0f))
            };
            var flags = new byte[2];
            var mask = new StrawSurfaceMask();
            mask.Apply(HaystackShape.Cylinder, Diameter, Height, matrices, flags, 2, Cell);

            Assert.IsFalse(Buried(flags[1]));
        }

        [Test]
        public void SingleStraw_IsExposed()
        {
            var matrices = new[] { StrawAt(new Vector3(0f, 0.4f, 0f)) };
            var flags = new[] { (byte)StrawFlags.IsNeedle };
            var mask = new StrawSurfaceMask();
            mask.Apply(HaystackShape.Cylinder, Diameter, Height, matrices, flags, 1, Cell);

            Assert.IsFalse(Buried(flags[0]));
            Assert.AreEqual((byte)StrawFlags.IsNeedle, flags[0]);
            Assert.AreEqual(1, mask.ExposedCount);
        }

        [Test]
        public void LayoutPile_HasBuriedInteriorAndExposedShell()
        {
            var settings = new HaystackLayoutSettings
            {
                Shape = HaystackShape.Cylinder,
                Diameter = 2.4f,
                Height = 1.4f,
                StrawCount = 500,
                Seed = 11,
                StrawLength = 0.28f,
                StrawRadius = 0.02f,
                HorizontalFraction = 0.85f
            };
            Straw[] straws = HaystackLayout.Generate(settings);
            var matrices = new Matrix4x4[straws.Length];
            var flags = new byte[straws.Length];
            for (int i = 0; i < straws.Length; i++)
            {
                matrices[i] = straws[i].LocalToWorld;
                flags[i] = straws[i].Flags;
            }

            var mask = new StrawSurfaceMask();
            mask.Apply(settings.Shape, settings.Diameter, settings.Height, matrices, flags, straws.Length);

            Assert.Greater(mask.BuriedCount, 20, "core of a filled pile should stay buried");
            Assert.Greater(mask.ExposedCount, 40, "outer shell must be pickable");
            Assert.Less(mask.ExposedCount, straws.Length, "not every straw is surface");

            int exposedSide = 0;
            float half = settings.Diameter * 0.5f;
            for (int i = 0; i < straws.Length; i++)
            {
                if (Buried(flags[i]))
                    continue;
                Vector3 p = matrices[i].GetPosition();
                float r = Mathf.Sqrt(p.x * p.x + p.z * p.z);
                if (r > half * 0.55f && p.y < settings.Height * 0.7f)
                    exposedSide++;
            }

            Assert.Greater(exposedSide, 8, "side shell must be pickable, not only the crown");
        }
    }
}
