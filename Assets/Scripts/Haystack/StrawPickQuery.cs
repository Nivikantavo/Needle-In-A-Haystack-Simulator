using UnityEngine;

namespace Needle.Haystack
{
    public sealed class StrawPickQuery
    {
        public const int CandidateCapacity = 1024;

        readonly StrawSpatialHash _hash;
        readonly int[] _candidates = new int[CandidateCapacity];

        public StrawPickQuery(float cellSize)
        {
            _hash = new StrawSpatialHash(cellSize);
        }

        public float CellSize => _hash.CellSize;

        public void Rebuild(Matrix4x4[] locals, byte[] flags, int count, Matrix4x4 parent)
        {
            _hash.Rebuild(locals, flags, count, parent);
        }

        public bool TryPick(
            Ray ray,
            float maxDistance,
            Matrix4x4[] locals,
            byte[] flags,
            int count,
            Matrix4x4 parent,
            float radiusMultiplier,
            out int index,
            out float distance)
        {
            index = -1;
            distance = 0f;
            if (locals == null || count <= 0 || maxDistance <= 0f)
                return false;

            Vector3 origin = ray.origin;
            Vector3 dir = ray.direction;
            if (dir.sqrMagnitude < 1e-12f)
                return false;

            float pad = _hash.CellSize + 0.25f;
            int n = _hash.CollectAlongRay(origin, dir, maxDistance, pad, _candidates);
            float best = maxDistance;
            int bestIndex = -1;

            for (int i = 0; i < n; i++)
            {
                int strawIndex = _candidates[i];
                if ((uint)strawIndex >= (uint)count)
                    continue;

                byte flag = flags != null && strawIndex < flags.Length ? flags[strawIndex] : (byte)0;
                if ((flag & (byte)(StrawFlags.Buried | StrawFlags.Grabbed | StrawFlags.Discarded)) != 0)
                    continue;

                Matrix4x4 world = parent * locals[strawIndex];
                if (!StrawCapsule.Raycast(origin, dir, maxDistance, world, radiusMultiplier, out float t))
                    continue;
                if (t >= best)
                    continue;

                best = t;
                bestIndex = strawIndex;
            }

            if (bestIndex < 0)
                return false;

            index = bestIndex;
            distance = best;
            return true;
        }
    }
}
