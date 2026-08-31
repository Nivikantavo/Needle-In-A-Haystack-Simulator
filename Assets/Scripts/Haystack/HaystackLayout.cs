using UnityEngine;

namespace Needle.Haystack
{
    public static class HaystackLayout
    {
        const int MinCount = 1;
        const float BaseHeightFraction = 0.2f;
        const float BaseDensityMultiplier = 2.5f;

        public static Straw[] Generate(HaystackLayoutSettings settings)
        {
            int count = Mathf.Max(MinCount, settings.StrawCount);
            float diameter = Mathf.Max(0.01f, settings.Diameter);
            float height = Mathf.Max(0.01f, settings.Height);
            float length = Mathf.Max(0.01f, settings.StrawLength);
            float radius = Mathf.Max(0.001f, settings.StrawRadius);

            var straws = new Straw[count];
            var rng = new System.Random(settings.Seed);

            float layerHeight = Mathf.Max(radius * 2.5f, 0.01f);
            int layerCount = Mathf.Max(1, Mathf.FloorToInt(height / layerHeight));

            var areas = new float[layerCount];
            var layerYs = new float[layerCount];
            float totalArea = 0f;
            for (int layer = 0; layer < layerCount; layer++)
            {
                float y = (layer + 0.5f) / layerCount * height;
                y = Mathf.Clamp(y, radius, Mathf.Max(radius, height - radius));
                layerYs[layer] = y;
                float sliceArea = HaystackVolume.SliceArea(settings.Shape, diameter, height, y);
                areas[layer] = sliceArea * HeightDensity(y, height);
                totalArea += areas[layer];
            }

            if (totalArea <= 0f)
            {
                for (int i = 0; i < layerCount; i++)
                    areas[i] = 1f;
                totalArea = layerCount;
            }

            Vector3 scale = new Vector3(radius * 2f, length, radius * 2f);

            int written = 0;
            for (int layer = 0; layer < layerCount; layer++)
            {
                int remainingLayers = layerCount - layer;
                int remaining = count - written;
                int n = layer == layerCount - 1
                    ? remaining
                    : Mathf.Clamp(Mathf.RoundToInt(count * (areas[layer] / totalArea)), 0, remaining - (remainingLayers - 1));

                float y = layerYs[layer];
                for (int i = 0; i < n; i++)
                {
                    Vector2 xz = HaystackVolume.RandomPointInSlice(settings.Shape, diameter, height, y, rng);
                    float jitterY = HaystackVolume.NextFloat(rng, -layerHeight * 0.25f, layerHeight * 0.25f);
                    Vector3 position = new Vector3(xz.x, Mathf.Clamp(y + jitterY, radius * 0.5f, height), xz.y);

                    int pull = 0;
                    while (!HaystackVolume.Contains(settings.Shape, diameter, height, position) && pull < 8)
                    {
                        position.x *= 0.5f;
                        position.z *= 0.5f;
                        pull++;
                    }

                    if (!HaystackVolume.Contains(settings.Shape, diameter, height, position))
                        position = new Vector3(0f, Mathf.Clamp(y, 0f, height), 0f);

                    Quaternion rotation = RandomStrawRotation(rng, settings.HorizontalFraction);

                    int index = written + i;
                    straws[index].LocalToWorld = Matrix4x4.TRS(position, rotation, scale);
                    straws[index].Variant = (ushort)rng.Next(0, 8);
                    straws[index].Flags = (byte)StrawFlags.None;
                }

                written += n;
            }

            StrawNeedle.StampInDepth(straws, settings);
            return straws;
        }

        static float HeightDensity(float y, float height)
        {
            if (height <= 0f)
                return 1f;
            return y / height < BaseHeightFraction ? BaseDensityMultiplier : 1f;
        }

        static Quaternion RandomStrawRotation(System.Random rng, float horizontalFraction)
        {
            float horizontalChance = Mathf.Clamp01(horizontalFraction);
            bool lieFlat = rng.NextDouble() < horizontalChance;

            Vector3 axis;
            if (lieFlat)
            {
                float yaw = HaystackVolume.NextFloat(rng, 0f, Mathf.PI * 2f);
                float lift = HaystackVolume.NextFloat(rng, -0.22f, 0.22f);
                axis = new Vector3(Mathf.Cos(yaw), lift, Mathf.Sin(yaw));
            }
            else
            {
                axis = new Vector3(
                    HaystackVolume.NextFloat(rng, -0.28f, 0.28f),
                    1f,
                    HaystackVolume.NextFloat(rng, -0.28f, 0.28f));
            }

            Quaternion align = Quaternion.FromToRotation(Vector3.up, axis.normalized);
            float spin = HaystackVolume.NextFloat(rng, 0f, 360f);
            return align * Quaternion.AngleAxis(spin, Vector3.up);
        }

        public static int NeedleIndexFor(int seed, int count)
        {
            count = Mathf.Max(MinCount, count);
            unchecked
            {
                uint hashed = (uint)seed * 2654435761u ^ 0x9E3779B9u;
                return (int)(hashed % (uint)count);
            }
        }
    }
}
