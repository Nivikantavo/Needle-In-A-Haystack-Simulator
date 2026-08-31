using UnityEngine;

namespace Needle.Haystack
{
    /// <summary>
    /// Remaining walkable pile size from straw data. Height follows the highest
    /// in-volume straw; diameter follows the remaining horizontal extent so the
    /// player can reach the exposed shell (top and sides), not only the crown.
    /// </summary>
    public static class HaystackPileOccupancy
    {
        public const float HeightStep = 0.05f;
        public const float DiameterStep = 0.1f;
        public const float HorizontalSlack = 1.1f;
        const byte SkipMask = (byte)(StrawFlags.Grabbed | StrawFlags.Discarded);

        public static HaystackPileOccupancyResult Measure(
            HaystackShape shape,
            float authoredDiameter,
            float authoredHeight,
            Matrix4x4[] localMatrices,
            byte[] flags,
            int count,
            float paddingY)
        {
            authoredDiameter = Mathf.Max(0.1f, authoredDiameter);
            authoredHeight = Mathf.Max(0.1f, authoredHeight);
            paddingY = Mathf.Max(0f, paddingY);

            if (localMatrices == null || count <= 0)
                return HaystackPileOccupancyResult.Empty(authoredDiameter);

            float containDiameter = authoredDiameter * HorizontalSlack;
            float containHeight = authoredHeight + Mathf.Max(0.05f, paddingY);
            int remaining = 0;
            float maxTop = 0f;
            float maxRadiusSq = 0f;
            float maxAxis = 0f;

            int n = count < localMatrices.Length ? count : localMatrices.Length;
            for (int i = 0; i < n; i++)
            {
                byte flag = flags != null && i < flags.Length ? flags[i] : (byte)0;
                if ((flag & SkipMask) != 0)
                    continue;

                Vector3 local = localMatrices[i].GetPosition();
                if (!HaystackVolume.Contains(shape, containDiameter, containHeight, local))
                    continue;

                remaining++;
                float top = local.y + paddingY;
                if (top > maxTop)
                    maxTop = top;

                float axis = Mathf.Max(Mathf.Abs(local.x), Mathf.Abs(local.z));
                if (axis > maxAxis)
                    maxAxis = axis;

                float radiusSq = local.x * local.x + local.z * local.z;
                if (radiusSq > maxRadiusSq)
                    maxRadiusSq = radiusSq;
            }

            if (remaining == 0)
                return HaystackPileOccupancyResult.Empty(authoredDiameter);

            float rawDiameter = UsesAxisExtent(shape)
                ? maxAxis * 2f
                : Mathf.Sqrt(maxRadiusSq) * 2f;
            float diameter = QuantizeDiameter(
                Mathf.Min(authoredDiameter, rawDiameter * HorizontalSlack),
                authoredDiameter);
            float height = QuantizeHeight(Mathf.Min(authoredHeight, maxTop), authoredHeight);
            return new HaystackPileOccupancyResult(remaining, diameter, height);
        }

        public static float QuantizeHeight(float height, float authoredHeight)
        {
            if (height <= 0f)
                return 0f;

            authoredHeight = Mathf.Max(HeightStep, authoredHeight);
            float stepped = Mathf.Ceil(height / HeightStep) * HeightStep;
            return Mathf.Clamp(stepped, HeightStep, authoredHeight);
        }

        public static float QuantizeDiameter(float diameter, float authoredDiameter)
        {
            if (diameter <= 0f)
                return 0f;

            authoredDiameter = Mathf.Max(0.1f, authoredDiameter);
            float stepped = Mathf.Ceil(diameter / DiameterStep) * DiameterStep;
            return Mathf.Clamp(stepped, DiameterStep, authoredDiameter);
        }

        static bool UsesAxisExtent(HaystackShape shape)
        {
            return shape == HaystackShape.Box || shape == HaystackShape.Pyramid;
        }
    }

    public readonly struct HaystackPileOccupancyResult
    {
        public readonly int Remaining;
        public readonly float Diameter;
        public readonly float Height;

        public HaystackPileOccupancyResult(int remaining, float diameter, float height)
        {
            Remaining = remaining;
            Diameter = diameter;
            Height = height;
        }

        public bool IsEmpty => Remaining <= 0 || Height <= 0f;

        public static HaystackPileOccupancyResult Empty(float diameter)
        {
            return new HaystackPileOccupancyResult(0, diameter, 0f);
        }
    }
}
