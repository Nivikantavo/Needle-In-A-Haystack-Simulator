using System.Collections.Generic;
using UnityEngine;

namespace Needle.Haystack
{
    public sealed class StrawSpatialHash
    {
        const int Bias = 512;
        const byte OccupiedMask = (byte)(StrawFlags.Buried | StrawFlags.Grabbed);

        readonly Dictionary<int, int> _heads = new Dictionary<int, int>(256);
        readonly float _cellSize;
        int[] _next = System.Array.Empty<int>();

        public StrawSpatialHash(float cellSize)
        {
            _cellSize = Mathf.Max(0.05f, cellSize);
        }

        public float CellSize => _cellSize;

        public void Rebuild(Matrix4x4[] locals, byte[] flags, int count, Matrix4x4 parent)
        {
            _heads.Clear();
            if (count <= 0 || locals == null)
                return;

            if (_next.Length < count)
                _next = new int[count];

            for (int i = 0; i < count; i++)
            {
                _next[i] = -1;
                byte flag = flags != null && i < flags.Length ? flags[i] : (byte)0;
                if ((flag & OccupiedMask) != 0)
                    continue;

                Vector3 p = (parent * locals[i]).GetPosition();
                int key = Pack(Floor(p.x), Floor(p.y), Floor(p.z));
                if (_heads.TryGetValue(key, out int head))
                    _next[i] = head;
                _heads[key] = i;
            }
        }

        public int CollectAlongRay(
            Vector3 origin,
            Vector3 direction,
            float maxDistance,
            float pad,
            int[] results)
        {
            if (results == null || results.Length == 0 || _heads.Count == 0)
                return 0;

            Vector3 dir = direction;
            float mag = dir.magnitude;
            if (mag < 1e-8f)
                return 0;
            dir /= mag;

            float reach = Mathf.Max(0f, maxDistance);
            Vector3 end = origin + dir * reach;
            pad = Mathf.Max(0f, pad);
            Vector3 min = Vector3.Min(origin, end) - Vector3.one * pad;
            Vector3 max = Vector3.Max(origin, end) + Vector3.one * pad;

            int x0 = Floor(min.x);
            int y0 = Floor(min.y);
            int z0 = Floor(min.z);
            int x1 = Floor(max.x);
            int y1 = Floor(max.y);
            int z1 = Floor(max.z);

            int written = 0;
            for (int x = x0; x <= x1; x++)
            {
                for (int y = y0; y <= y1; y++)
                {
                    for (int z = z0; z <= z1; z++)
                    {
                        if (!_heads.TryGetValue(Pack(x, y, z), out int index))
                            continue;

                        while (index >= 0)
                        {
                            if (written >= results.Length)
                                return written;
                            results[written++] = index;
                            index = _next[index];
                        }
                    }
                }
            }

            return written;
        }

        int Floor(float v)
        {
            return Mathf.FloorToInt(v / _cellSize);
        }

        static int Pack(int x, int y, int z)
        {
            return ((x + Bias) << 20) | ((y + Bias) << 10) | (z + Bias);
        }
    }
}
