using UnityEngine;

namespace Needle.Haystack
{
    public static class HaystackVolume
    {
        const float SurfaceEpsilon = 1e-4f;

        public static bool Contains(HaystackShape shape, float diameter, float height, Vector3 localPoint)
        {
            if (diameter <= 0f || height <= 0f)
                return false;

            float y = localPoint.y;
            if (y < -SurfaceEpsilon || y > height + SurfaceEpsilon)
                return false;

            float half = diameter * 0.5f;
            float x = localPoint.x;
            float z = localPoint.z;

            switch (shape)
            {
                case HaystackShape.Cylinder:
                    return x * x + z * z <= half * half + SurfaceEpsilon;
                case HaystackShape.Hemisphere:
                    return EllipsoidHemisphereContains(x, y, z, half, height);
                case HaystackShape.Pyramid:
                    return PyramidContains(x, y, z, half, height);
                case HaystackShape.Box:
                    return Mathf.Abs(x) <= half + SurfaceEpsilon && Mathf.Abs(z) <= half + SurfaceEpsilon;
                default:
                    return false;
            }
        }

        public static float SliceArea(HaystackShape shape, float diameter, float height, float y)
        {
            if (diameter <= 0f || height <= 0f || y < 0f || y > height)
                return 0f;

            float half = diameter * 0.5f;
            switch (shape)
            {
                case HaystackShape.Cylinder:
                    return Mathf.PI * half * half;
                case HaystackShape.Hemisphere:
                {
                    float scale = HorizontalEllipsoidScale(y, height);
                    float r = half * scale;
                    return Mathf.PI * r * r;
                }
                case HaystackShape.Pyramid:
                {
                    float side = diameter * PyramidScale(y, height);
                    return side * side;
                }
                case HaystackShape.Box:
                    return diameter * diameter;
                default:
                    return 0f;
            }
        }

        public static Vector2 RandomPointInSlice(HaystackShape shape, float diameter, float height, float y, System.Random rng)
        {
            float half = diameter * 0.5f;
            switch (shape)
            {
                case HaystackShape.Cylinder:
                    return RandomInDisk(half, rng);
                case HaystackShape.Hemisphere:
                    return RandomInDisk(half * HorizontalEllipsoidScale(y, height), rng);
                case HaystackShape.Pyramid:
                {
                    float h = half * PyramidScale(y, height);
                    return new Vector2(NextFloat(rng, -h, h), NextFloat(rng, -h, h));
                }
                case HaystackShape.Box:
                    return new Vector2(NextFloat(rng, -half, half), NextFloat(rng, -half, half));
                default:
                    return Vector2.zero;
            }
        }

        public static void GetBounds(float diameter, float height, out Vector3 center, out Vector3 size)
        {
            center = new Vector3(0f, height * 0.5f, 0f);
            size = new Vector3(diameter, height, diameter);
        }

        public static float InteriorDistance(HaystackShape shape, float diameter, float height, Vector3 localPoint)
        {
            if (!Contains(shape, diameter, height, localPoint))
                return 0f;

            float half = diameter * 0.5f;
            float x = localPoint.x;
            float y = localPoint.y;
            float z = localPoint.z;

            switch (shape)
            {
                case HaystackShape.Box:
                    return Mathf.Max(0f, Mathf.Min(half - Mathf.Abs(x), half - Mathf.Abs(z), y, height - y));
                case HaystackShape.Cylinder:
                {
                    float radial = Mathf.Sqrt(x * x + z * z);
                    return Mathf.Max(0f, Mathf.Min(half - radial, y, height - y));
                }
                case HaystackShape.Hemisphere:
                {
                    float nx = x / half;
                    float ny = height > 0f ? y / height : 1f;
                    float nz = z / half;
                    float rho = Mathf.Sqrt(nx * nx + ny * ny + nz * nz);
                    if (rho >= 1f)
                        return 0f;
                    float radial = (1f - rho) * Mathf.Min(half, height);
                    return Mathf.Max(0f, Mathf.Min(radial, y));
                }
                case HaystackShape.Pyramid:
                {
                    float h = half * PyramidScale(y, height);
                    return Mathf.Max(0f, Mathf.Min(h - Mathf.Abs(x), h - Mathf.Abs(z), y));
                }
                default:
                    return 0f;
            }
        }

        public static float SliceRadius(HaystackShape shape, float diameter, float height, float y)
        {
            float half = Mathf.Max(0.001f, diameter * 0.5f);
            switch (shape)
            {
                case HaystackShape.Hemisphere:
                    return half * HorizontalEllipsoidScale(y, height);
                case HaystackShape.Pyramid:
                    return half * PyramidScale(y, height);
                default:
                    return half;
            }
        }

        public static float AxisNormalized(HaystackShape shape, float diameter, float height, Vector3 localPoint)
        {
            float r = Mathf.Sqrt(localPoint.x * localPoint.x + localPoint.z * localPoint.z);
            float maxR = SliceRadius(shape, diameter, height, localPoint.y);
            if (maxR <= 1e-5f)
                return r <= 1e-5f ? 0f : 1f;
            return Mathf.Clamp01(r / maxR);
        }

        static bool EllipsoidHemisphereContains(float x, float y, float z, float radius, float height)
        {
            float nx = x / radius;
            float ny = y / height;
            float nz = z / radius;
            return nx * nx + ny * ny + nz * nz <= 1f + SurfaceEpsilon;
        }

        static bool PyramidContains(float x, float y, float z, float halfBase, float height)
        {
            float scale = PyramidScale(y, height);
            float half = halfBase * scale;
            return Mathf.Abs(x) <= half + SurfaceEpsilon && Mathf.Abs(z) <= half + SurfaceEpsilon;
        }

        static float PyramidScale(float y, float height)
        {
            if (height <= 0f)
                return 0f;
            return Mathf.Clamp01(1f - y / height);
        }

        static float HorizontalEllipsoidScale(float y, float height)
        {
            if (height <= 0f)
                return 0f;
            float t = Mathf.Clamp01(y / height);
            float inner = 1f - t * t;
            return inner <= 0f ? 0f : Mathf.Sqrt(inner);
        }

        static Vector2 RandomInDisk(float radius, System.Random rng)
        {
            if (radius <= 0f)
                return Vector2.zero;

            float angle = NextFloat(rng, 0f, Mathf.PI * 2f);
            float r = radius * Mathf.Sqrt((float)rng.NextDouble());
            return new Vector2(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r);
        }

        public static float NextFloat(System.Random rng, float min, float max)
        {
            return min + (float)rng.NextDouble() * (max - min);
        }
    }
}
