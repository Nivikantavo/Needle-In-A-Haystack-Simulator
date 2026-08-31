using Needle.Haystack;
using NUnit.Framework;
using UnityEngine;

namespace Needle.Haystack.Editor.Tests
{
    public class HaystackVolumeTests
    {
        [Test]
        public void Cylinder_CenterAndRim_AreInside()
        {
            Assert.IsTrue(HaystackVolume.Contains(HaystackShape.Cylinder, 4f, 2f, new Vector3(0f, 1f, 0f)));
            Assert.IsTrue(HaystackVolume.Contains(HaystackShape.Cylinder, 4f, 2f, new Vector3(2f, 0f, 0f)));
            Assert.IsFalse(HaystackVolume.Contains(HaystackShape.Cylinder, 4f, 2f, new Vector3(2.2f, 1f, 0f)));
            Assert.IsFalse(HaystackVolume.Contains(HaystackShape.Cylinder, 4f, 2f, new Vector3(0f, -0.1f, 0f)));
            Assert.IsFalse(HaystackVolume.Contains(HaystackShape.Cylinder, 4f, 2f, new Vector3(0f, 2.2f, 0f)));
        }

        [Test]
        public void Hemisphere_PoleAndEquator_AreInside()
        {
            Assert.IsTrue(HaystackVolume.Contains(HaystackShape.Hemisphere, 4f, 2f, new Vector3(0f, 2f, 0f)));
            Assert.IsTrue(HaystackVolume.Contains(HaystackShape.Hemisphere, 4f, 2f, new Vector3(2f, 0f, 0f)));
            Assert.IsTrue(HaystackVolume.Contains(HaystackShape.Hemisphere, 4f, 2f, Vector3.zero));
            Assert.IsFalse(HaystackVolume.Contains(HaystackShape.Hemisphere, 4f, 2f, new Vector3(2f, 2f, 0f)));
            Assert.IsFalse(HaystackVolume.Contains(HaystackShape.Hemisphere, 4f, 2f, new Vector3(0f, -0.1f, 0f)));
        }

        [Test]
        public void Pyramid_BaseInside_ApexInside_OutsideFails()
        {
            Assert.IsTrue(HaystackVolume.Contains(HaystackShape.Pyramid, 4f, 2f, Vector3.zero));
            Assert.IsTrue(HaystackVolume.Contains(HaystackShape.Pyramid, 4f, 2f, new Vector3(0f, 2f, 0f)));
            Assert.IsTrue(HaystackVolume.Contains(HaystackShape.Pyramid, 4f, 2f, new Vector3(1.9f, 0.01f, 0f)));
            Assert.IsFalse(HaystackVolume.Contains(HaystackShape.Pyramid, 4f, 2f, new Vector3(1.5f, 1.5f, 0f)));
        }

        [Test]
        public void Box_InteriorAndOutside()
        {
            Assert.IsTrue(HaystackVolume.Contains(HaystackShape.Box, 4f, 2f, new Vector3(0f, 1f, 0f)));
            Assert.IsTrue(HaystackVolume.Contains(HaystackShape.Box, 4f, 2f, new Vector3(2f, 0f, 2f)));
            Assert.IsFalse(HaystackVolume.Contains(HaystackShape.Box, 4f, 2f, new Vector3(2.2f, 1f, 0f)));
        }

        [Test]
        public void SliceArea_IsPositiveInsideVolume()
        {
            Assert.Greater(HaystackVolume.SliceArea(HaystackShape.Cylinder, 4f, 2f, 1f), 0f);
            Assert.Greater(HaystackVolume.SliceArea(HaystackShape.Hemisphere, 4f, 2f, 0.5f), 0f);
            Assert.Greater(HaystackVolume.SliceArea(HaystackShape.Pyramid, 4f, 2f, 0.5f), 0f);
            Assert.Greater(HaystackVolume.SliceArea(HaystackShape.Box, 4f, 2f, 1f), 0f);
            Assert.AreEqual(0f, HaystackVolume.SliceArea(HaystackShape.Cylinder, 4f, 2f, -1f));
        }

        [Test]
        public void InteriorDistance_CenterIsDeeperThanRim()
        {
            float center = HaystackVolume.InteriorDistance(HaystackShape.Cylinder, 4f, 2f, new Vector3(0f, 1f, 0f));
            float rim = HaystackVolume.InteriorDistance(HaystackShape.Cylinder, 4f, 2f, new Vector3(1.9f, 1f, 0f));
            float top = HaystackVolume.InteriorDistance(HaystackShape.Cylinder, 4f, 2f, new Vector3(0f, 1.95f, 0f));
            float outside = HaystackVolume.InteriorDistance(HaystackShape.Cylinder, 4f, 2f, new Vector3(3f, 1f, 0f));
            Assert.Greater(center, rim);
            Assert.Greater(center, top);
            Assert.AreEqual(0f, outside);
        }

        [Test]
        public void AxisNormalized_IsZeroOnAxis_AndHighAtRim()
        {
            float axis = HaystackVolume.AxisNormalized(HaystackShape.Cylinder, 4f, 2f, new Vector3(0f, 1f, 0f));
            float rim = HaystackVolume.AxisNormalized(HaystackShape.Cylinder, 4f, 2f, new Vector3(2f, 1f, 0f));
            float mid = HaystackVolume.AxisNormalized(HaystackShape.Cylinder, 4f, 2f, new Vector3(1f, 1f, 0f));
            Assert.AreEqual(0f, axis);
            Assert.Greater(rim, 0.95f);
            Assert.Greater(mid, axis);
            Assert.Less(mid, rim);
        }

        [Test]
        public void AxisNormalized_IsZeroOnAxis_ForEveryShape()
        {
            var mid = new Vector3(0f, 1f, 0f);
            foreach (HaystackShape shape in System.Enum.GetValues(typeof(HaystackShape)))
            {
                Assert.AreEqual(0f, HaystackVolume.AxisNormalized(shape, 4f, 2f, mid), shape.ToString());
            }
        }
    }
}
