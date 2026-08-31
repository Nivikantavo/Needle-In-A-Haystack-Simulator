using UnityEngine;

namespace Needle.Haystack
{
    public static class StrawNeedle
    {
        public const byte Flag = (byte)StrawFlags.IsNeedle;
        public const float AxisCoreFraction = 0.15f;
        public const float VerticalPadding = 0.18f;
        const byte SkipMask = (byte)(StrawFlags.Grabbed | StrawFlags.Discarded);
        static readonly float[] AxisRelax = { AxisCoreFraction, 0.22f, 0.32f, 0.5f };

        public static bool HasFlag(byte flags)
        {
            return (flags & Flag) != 0;
        }

        public static bool HasFlag(byte[] flags, int index)
        {
            if (flags == null || (uint)index >= (uint)flags.Length)
                return false;
            return HasFlag(flags[index]);
        }

        public static int FindIndex(byte[] flags, int count)
        {
            if (flags == null || count <= 0)
                return -1;

            int n = count < flags.Length ? count : flags.Length;
            for (int i = 0; i < n; i++)
            {
                if (HasFlag(flags[i]))
                    return i;
            }

            return -1;
        }

        public static int StampInDepth(Straw[] straws, HaystackLayoutSettings settings)
        {
            if (straws == null || straws.Length == 0)
                return -1;
            return StampInDepth(straws, settings.Shape, settings.Diameter, settings.Height, settings.Seed);
        }

        public static int StampInDepth(
            Straw[] straws,
            HaystackShape shape,
            float diameter,
            float height,
            int seed)
        {
            if (straws == null || straws.Length == 0)
                return -1;

            int n = straws.Length;
            OccupiedY(straws, n, shape, diameter, height, out float yMin, out float yMax);
            int chosen = PickByAxis(
                n,
                seed,
                shape,
                diameter,
                height,
                yMin,
                yMax,
                i => (straws[i].Flags & SkipMask) != 0,
                i => straws[i].LocalToWorld.GetPosition());

            for (int i = 0; i < n; i++)
                straws[i].Flags = (byte)(straws[i].Flags & ~Flag);

            if (chosen >= 0)
                straws[chosen].Flags = (byte)(straws[chosen].Flags | Flag);

            return chosen;
        }

        public static int StampInDepth(
            Matrix4x4[] locals,
            byte[] flags,
            int count,
            HaystackShape shape,
            float diameter,
            float height,
            int seed)
        {
            if (locals == null || flags == null || count <= 0)
                return -1;

            int n = count;
            if (n > locals.Length)
                n = locals.Length;
            if (n > flags.Length)
                n = flags.Length;

            OccupiedY(locals, flags, n, shape, diameter, height, out float yMin, out float yMax);
            int chosen = PickByAxis(
                n,
                seed,
                shape,
                diameter,
                height,
                yMin,
                yMax,
                i => (flags[i] & SkipMask) != 0,
                i => locals[i].GetPosition());

            for (int i = 0; i < n; i++)
                flags[i] = (byte)(flags[i] & ~Flag);

            if (chosen >= 0)
                flags[chosen] = (byte)(flags[chosen] | Flag);

            return chosen;
        }

        public static bool IsAxisCandidate(
            HaystackShape shape,
            float diameter,
            float height,
            Vector3 localPoint,
            float axisMax)
        {
            return IsAxisCandidate(shape, diameter, height, 0f, height, localPoint, axisMax);
        }

        public static bool IsAxisCandidate(
            HaystackShape shape,
            float diameter,
            float height,
            float occupiedYMin,
            float occupiedYMax,
            Vector3 localPoint,
            float axisMax)
        {
            if (height <= 0.001f || diameter <= 0.001f)
                return false;
            if (!HaystackVolume.Contains(shape, diameter, height, localPoint))
                return false;
            if (!InOccupiedBand(occupiedYMin, occupiedYMax, localPoint.y))
                return false;

            return HaystackVolume.AxisNormalized(shape, diameter, height, localPoint) <= axisMax;
        }

        static int PickByAxis(
            int n,
            int seed,
            HaystackShape shape,
            float diameter,
            float height,
            float yMin,
            float yMax,
            System.Func<int, bool> skip,
            System.Func<int, Vector3> position)
        {
            int chosen = -1;
            for (int r = 0; r < AxisRelax.Length && chosen < 0; r++)
            {
                float axisMax = AxisRelax[r];
                int eligible = 0;
                for (int i = 0; i < n; i++)
                {
                    if (skip(i))
                        continue;
                    if (IsAxisCandidate(shape, diameter, height, yMin, yMax, position(i), axisMax))
                        eligible++;
                }

                if (eligible <= 0)
                    continue;

                int slot = HaystackLayout.NeedleIndexFor(seed, eligible);
                int cursor = 0;
                for (int i = 0; i < n; i++)
                {
                    if (skip(i))
                        continue;
                    if (!IsAxisCandidate(shape, diameter, height, yMin, yMax, position(i), axisMax))
                        continue;
                    if (cursor == slot)
                    {
                        chosen = i;
                        break;
                    }

                    cursor++;
                }
            }

            if (chosen >= 0)
                return chosen;

            int best = -1;
            float bestAxis = float.PositiveInfinity;
            for (int i = 0; i < n; i++)
            {
                if (skip(i))
                    continue;
                float axis = HaystackVolume.AxisNormalized(shape, diameter, height, position(i));
                if (axis < bestAxis)
                {
                    bestAxis = axis;
                    best = i;
                }
            }

            return best;
        }

        static void OccupiedY(
            Straw[] straws,
            int n,
            HaystackShape shape,
            float diameter,
            float height,
            out float yMin,
            out float yMax)
        {
            yMin = float.PositiveInfinity;
            yMax = float.NegativeInfinity;
            for (int i = 0; i < n; i++)
            {
                if ((straws[i].Flags & SkipMask) != 0)
                    continue;
                Vector3 p = straws[i].LocalToWorld.GetPosition();
                if (!HaystackVolume.Contains(shape, diameter, height, p))
                    continue;
                if (p.y < yMin)
                    yMin = p.y;
                if (p.y > yMax)
                    yMax = p.y;
            }

            if (float.IsInfinity(yMin))
            {
                yMin = 0f;
                yMax = height;
            }
        }

        static void OccupiedY(
            Matrix4x4[] locals,
            byte[] flags,
            int n,
            HaystackShape shape,
            float diameter,
            float height,
            out float yMin,
            out float yMax)
        {
            yMin = float.PositiveInfinity;
            yMax = float.NegativeInfinity;
            for (int i = 0; i < n; i++)
            {
                if ((flags[i] & SkipMask) != 0)
                    continue;
                Vector3 p = locals[i].GetPosition();
                if (!HaystackVolume.Contains(shape, diameter, height, p))
                    continue;
                if (p.y < yMin)
                    yMin = p.y;
                if (p.y > yMax)
                    yMax = p.y;
            }

            if (float.IsInfinity(yMin))
            {
                yMin = 0f;
                yMax = height;
            }
        }

        static bool InOccupiedBand(float yMin, float yMax, float y)
        {
            float span = yMax - yMin;
            if (span <= 0.05f)
                return true;

            float pad = VerticalPadding * span;
            return y >= yMin + pad && y <= yMax - pad;
        }
    }
}
