using Needle.Haystack;
using NUnit.Framework;
using UnityEngine;

namespace Needle.Haystack.Editor.Tests
{
    public class HaystackPoseAssetTests
    {
        [Test]
        public void EnsureNeedle_StampsExactlyOneWhenMissing()
        {
            var asset = ScriptableObject.CreateInstance<HaystackPoseAsset>();
            var straws = new Straw[12];
            for (int i = 0; i < straws.Length; i++)
                straws[i].LocalToWorld = Matrix4x4.identity;

            asset.Apply(straws, new HaystackLayoutSettings
            {
                Shape = HaystackShape.Cylinder,
                Diameter = 2f,
                Height = 1f,
                StrawCount = straws.Length,
                Seed = 7,
                StrawLength = 0.3f,
                StrawRadius = 0.01f
            });

            Assert.AreEqual(-1, StrawNeedle.FindIndex(asset.Flags, asset.Count));
            Assert.IsTrue(asset.EnsureNeedle(7));
            Assert.GreaterOrEqual(StrawNeedle.FindIndex(asset.Flags, asset.Count), 0);
            Assert.IsFalse(asset.EnsureNeedle(7));
            Object.DestroyImmediate(asset);
        }

        [Test]
        public void DefaultPoseAsset_NeedleIsNearTheAxis()
        {
            var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<HaystackPoseAsset>(
                HaystackAssetUtility.DefaultPosePath);
            Assert.IsNotNull(asset);
            Assert.Greater(asset.Count, 0);

            int needle = StrawNeedle.FindIndex(asset.Flags, asset.Count);
            Assert.GreaterOrEqual(needle, 0);

            Vector3 p = asset.Matrices[needle].GetPosition();
            float axis = HaystackVolume.AxisNormalized(asset.Shape, asset.Diameter, asset.Height, p);
            float y01 = asset.Height > 0f ? p.y / asset.Height : 0f;
            Assert.Less(axis, StrawNeedle.AxisCoreFraction + 0.05f, $"needle {needle} axis={axis} at {p}");
            Assert.Greater(y01, StrawNeedle.VerticalPadding - 0.05f);
            Assert.Less(y01, 1f - StrawNeedle.VerticalPadding + 0.05f);
        }
    }
}
