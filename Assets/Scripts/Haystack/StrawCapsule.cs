using UnityEngine;

namespace Needle.Haystack
{
    public static class StrawCapsule
    {
        public static void FromMatrix(Matrix4x4 localToWorld, out Vector3 a, out Vector3 b, out float radius)
        {
            Vector3 center = localToWorld.GetPosition();
            Vector3 axis = localToWorld.GetColumn(1);
            float length = axis.magnitude;
            if (length < 1e-5f)
            {
                a = center;
                b = center;
                radius = localToWorld.GetColumn(0).magnitude * 0.5f;
                return;
            }

            axis /= length;
            radius = localToWorld.GetColumn(0).magnitude * 0.5f;
            float half = length * 0.5f;
            a = center - axis * half;
            b = center + axis * half;
        }

        public static bool Raycast(
            Vector3 origin,
            Vector3 direction,
            float maxDistance,
            Matrix4x4 localToWorld,
            float radiusMultiplier,
            out float distance)
        {
            FromMatrix(localToWorld, out Vector3 a, out Vector3 b, out float radius);
            radius *= Mathf.Max(1f, radiusMultiplier);
            return Raycast(origin, direction, maxDistance, a, b, radius, out distance);
        }

        public static bool Raycast(
            Vector3 origin,
            Vector3 direction,
            float maxDistance,
            Vector3 a,
            Vector3 b,
            float radius,
            out float distance)
        {
            distance = 0f;
            if (maxDistance <= 0f || radius <= 0f)
                return false;

            Vector3 rd = direction;
            float rdSq = rd.sqrMagnitude;
            if (rdSq < 1e-12f)
                return false;

            rd /= Mathf.Sqrt(rdSq);

            Vector3 ba = b - a;
            Vector3 oa = origin - a;
            float baba = Vector3.Dot(ba, ba);
            float bard = Vector3.Dot(ba, rd);
            float baoa = Vector3.Dot(ba, oa);
            float rdoa = Vector3.Dot(rd, oa);
            float oaoa = Vector3.Dot(oa, oa);
            float r2 = radius * radius;

            float hit = float.PositiveInfinity;

            if (baba > 1e-12f)
            {
                float qa = baba - bard * bard;
                float qb = baba * rdoa - baoa * bard;
                float qc = baba * oaoa - baoa * baoa - r2 * baba;
                float qh = qb * qb - qa * qc;
                if (qh >= 0f && Mathf.Abs(qa) > 1e-12f)
                {
                    float t = (-qb - Mathf.Sqrt(qh)) / qa;
                    float y = baoa + t * bard;
                    if (t >= 0f && t <= maxDistance && y > 0f && y < baba)
                        hit = t;
                }
            }

            TrySphere(origin, rd, maxDistance, a, r2, ref hit);
            TrySphere(origin, rd, maxDistance, b, r2, ref hit);

            if (float.IsPositiveInfinity(hit))
                return false;

            distance = hit;
            return true;
        }

        static void TrySphere(Vector3 origin, Vector3 rd, float maxDistance, Vector3 center, float r2, ref float hit)
        {
            Vector3 oc = origin - center;
            float b = Vector3.Dot(rd, oc);
            float c = Vector3.Dot(oc, oc) - r2;
            float h = b * b - c;
            if (h < 0f)
                return;

            float t = -b - Mathf.Sqrt(h);
            if (t >= 0f && t <= maxDistance && t < hit)
                hit = t;
        }
    }
}
