using Needle.Haystack;
using NUnit.Framework;
using UnityEngine;

namespace Needle.Haystack.Editor.Tests
{
    public class HaystackPresetTests
    {
        [Test]
        public void ToLayoutSettings_UsesCallerShape_AndMapsRadiusToDiameter()
        {
            var preset = ScriptableObject.CreateInstance<HaystackPreset>();
            var so = new UnityEditor.SerializedObject(preset);
            so.FindProperty("_radius").floatValue = 3f;
            so.FindProperty("_height").floatValue = 2.5f;
            so.FindProperty("_strawCount").intValue = 1200;
            so.FindProperty("_seed").intValue = 9;
            so.ApplyModifiedPropertiesWithoutUndo();

            HaystackLayoutSettings settings = preset.ToLayoutSettings(
                HaystackShape.Pyramid, 0.35f, 0.012f, 0.85f);
            Assert.AreEqual(HaystackShape.Pyramid, settings.Shape);
            Assert.AreEqual(6f, settings.Diameter, 0.001f);
            Assert.AreEqual(2.5f, settings.Height, 0.001f);
            Assert.AreEqual(1200, settings.StrawCount);
            Assert.AreEqual(9, settings.Seed);
            Assert.AreEqual(0.35f, settings.StrawLength, 0.001f);
            Object.DestroyImmediate(preset);
        }

        [Test]
        public void Generate_FromPreset_DoesNotNeedAPoseAsset()
        {
            var preset = ScriptableObject.CreateInstance<HaystackPreset>();
            var so = new UnityEditor.SerializedObject(preset);
            so.FindProperty("_radius").floatValue = 1.5f;
            so.FindProperty("_height").floatValue = 1.2f;
            so.FindProperty("_strawCount").intValue = 80;
            so.FindProperty("_seed").intValue = 4;
            so.ApplyModifiedPropertiesWithoutUndo();

            Straw[] straws = HaystackLayout.Generate(
                preset.ToLayoutSettings(HaystackShape.Cylinder, 0.3f, 0.012f, 0.85f));
            Assert.AreEqual(80, straws.Length);
            Assert.GreaterOrEqual(StrawNeedle.FindIndex(FlagsOf(straws), straws.Length), 0);
            Object.DestroyImmediate(preset);
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
