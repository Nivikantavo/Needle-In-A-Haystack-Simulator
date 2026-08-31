using UnityEngine;

namespace Needle.Haystack
{
    /// <summary>
    /// Marks interior straws <see cref="StrawFlags.Buried"/> from a voxel flood.
    /// Exposed = remaining pile adjacent to outside air (top and sides). Ground does not unbury.
    /// </summary>
    public sealed class StrawSurfaceMask
    {
        public const float DefaultCellSize = 0.25f;
        public const float GroundY = 0f;
        const byte Empty = 0;
        const byte Occupied = 1;
        const byte Air = 2;
        const byte SkipMask = (byte)(StrawFlags.Grabbed | StrawFlags.Discarded);

        static readonly int[] Neighbor = { 1, 0, 0, -1, 0, 0, 0, 1, 0, 0, -1, 0, 0, 0, 1, 0, 0, -1 };

        byte[] _cells = System.Array.Empty<byte>();
        int[] _queue = System.Array.Empty<int>();
        int _sizeX;
        int _sizeY;
        int _sizeZ;
        Vector3 _origin;
        float _cellSize = DefaultCellSize;

        public int ExposedCount { get; private set; }
        public int BuriedCount { get; private set; }

        public void Apply(
            HaystackShape shape,
            float authoredDiameter,
            float authoredHeight,
            Matrix4x4[] locals,
            byte[] flags,
            int count)
        {
            Apply(shape, authoredDiameter, authoredHeight, locals, flags, count, DefaultCellSize);
        }

        public void Apply(
            HaystackShape shape,
            float authoredDiameter,
            float authoredHeight,
            Matrix4x4[] locals,
            byte[] flags,
            int count,
            float cellSize)
        {
            ExposedCount = 0;
            BuriedCount = 0;
            if (locals == null || flags == null || count <= 0)
                return;

            int n = count < locals.Length ? count : locals.Length;
            if (n > flags.Length)
                n = flags.Length;

            _cellSize = Mathf.Max(0.08f, cellSize);
            float containDiameter = authoredDiameter * HaystackPileOccupancy.HorizontalSlack;
            float containHeight = authoredHeight + Mathf.Max(0.05f, _cellSize);

            if (!TryBuildGrid(shape, containDiameter, containHeight, locals, flags, n))
            {
                ClearBuriedOnRemaining(flags, n);
                return;
            }

            FloodAir();
            Classify(shape, containDiameter, containHeight, locals, flags, n);
        }

        void ClearBuriedOnRemaining(byte[] flags, int n)
        {
            for (int i = 0; i < n; i++)
            {
                byte flag = flags[i];
                if ((flag & SkipMask) != 0)
                    continue;

                flags[i] = (byte)(flag & ~(byte)StrawFlags.Buried);
                ExposedCount++;
            }
        }

        bool TryBuildGrid(
            HaystackShape shape,
            float containDiameter,
            float containHeight,
            Matrix4x4[] locals,
            byte[] flags,
            int n)
        {
            float minX = float.PositiveInfinity;
            float minY = float.PositiveInfinity;
            float minZ = float.PositiveInfinity;
            float maxX = float.NegativeInfinity;
            float maxY = float.NegativeInfinity;
            float maxZ = float.NegativeInfinity;
            int inPile = 0;

            for (int i = 0; i < n; i++)
            {
                if ((flags[i] & SkipMask) != 0)
                    continue;

                Vector3 p = locals[i].GetPosition();
                if (!HaystackVolume.Contains(shape, containDiameter, containHeight, p))
                    continue;

                inPile++;
                Encapsulate(p, ref minX, ref minY, ref minZ, ref maxX, ref maxY, ref maxZ);
                StrawCapsule.FromMatrix(locals[i], out Vector3 a, out Vector3 b, out _);
                Encapsulate(a, ref minX, ref minY, ref minZ, ref maxX, ref maxY, ref maxZ);
                Encapsulate(b, ref minX, ref minY, ref minZ, ref maxX, ref maxY, ref maxZ);
            }

            if (inPile <= 0 || minX > maxX)
                return false;

            float pad = _cellSize;
            _origin = new Vector3(minX - pad, minY - pad, minZ - pad);
            Vector3 max = new Vector3(maxX + pad, maxY + pad, maxZ + pad);
            _sizeX = Mathf.Max(3, Mathf.CeilToInt((max.x - _origin.x) / _cellSize) + 1);
            _sizeY = Mathf.Max(3, Mathf.CeilToInt((max.y - _origin.y) / _cellSize) + 1);
            _sizeZ = Mathf.Max(3, Mathf.CeilToInt((max.z - _origin.z) / _cellSize) + 1);

            int cellCount = _sizeX * _sizeY * _sizeZ;
            if (_cells.Length < cellCount)
                _cells = new byte[cellCount];
            else
                System.Array.Clear(_cells, 0, cellCount);

            if (_queue.Length < cellCount)
                _queue = new int[cellCount];

            for (int i = 0; i < n; i++)
            {
                if ((flags[i] & SkipMask) != 0)
                    continue;

                Vector3 p = locals[i].GetPosition();
                if (!HaystackVolume.Contains(shape, containDiameter, containHeight, p))
                    continue;

                StrawCapsule.FromMatrix(locals[i], out Vector3 a, out Vector3 b, out _);
                StampCapsule(a, b);
            }

            return true;
        }

        void FloodAir()
        {
            int qh = 0;
            int qt = 0;
            int cellCount = _sizeX * _sizeY * _sizeZ;

            for (int z = 0; z < _sizeZ; z++)
            {
                for (int y = 0; y < _sizeY; y++)
                {
                    for (int x = 0; x < _sizeX; x++)
                    {
                        if (!IsBorder(x, y, z))
                            continue;

                        int index = Index(x, y, z);
                        if (_cells[index] != Empty || IsGround(y))
                            continue;

                        _cells[index] = Air;
                        _queue[qt++] = index;
                    }
                }
            }

            while (qh < qt)
            {
                int index = _queue[qh++];
                Decode(index, out int x, out int y, out int z);
                for (int n = 0; n < 6; n++)
                {
                    int nx = x + Neighbor[n * 3];
                    int ny = y + Neighbor[n * 3 + 1];
                    int nz = z + Neighbor[n * 3 + 2];
                    if ((uint)nx >= (uint)_sizeX || (uint)ny >= (uint)_sizeY || (uint)nz >= (uint)_sizeZ)
                        continue;
                    if (IsGround(ny))
                        continue;

                    int ni = Index(nx, ny, nz);
                    if (_cells[ni] != Empty)
                        continue;

                    _cells[ni] = Air;
                    if (qt < cellCount)
                        _queue[qt++] = ni;
                }
            }
        }

        void Classify(
            HaystackShape shape,
            float containDiameter,
            float containHeight,
            Matrix4x4[] locals,
            byte[] flags,
            int n)
        {
            for (int i = 0; i < n; i++)
            {
                byte flag = flags[i];
                if ((flag & SkipMask) != 0)
                    continue;

                Vector3 p = locals[i].GetPosition();
                bool exposed;
                if (!HaystackVolume.Contains(shape, containDiameter, containHeight, p))
                    exposed = true;
                else
                    exposed = IsStrawExposed(locals[i]);

                if (exposed)
                {
                    flags[i] = (byte)(flag & ~(byte)StrawFlags.Buried);
                    ExposedCount++;
                }
                else
                {
                    flags[i] = (byte)(flag | (byte)StrawFlags.Buried);
                    BuriedCount++;
                }
            }
        }

        bool IsStrawExposed(Matrix4x4 local)
        {
            StrawCapsule.FromMatrix(local, out Vector3 a, out Vector3 b, out _);
            return PointExposed(local.GetPosition()) || PointExposed(a) || PointExposed(b);
        }

        bool PointExposed(Vector3 p)
        {
            int x = Cell(p.x, _origin.x);
            int y = Cell(p.y, _origin.y);
            int z = Cell(p.z, _origin.z);
            if ((uint)x >= (uint)_sizeX || (uint)y >= (uint)_sizeY || (uint)z >= (uint)_sizeZ)
                return true;

            if (_cells[Index(x, y, z)] == Air)
                return true;

            for (int n = 0; n < 6; n++)
            {
                int nx = x + Neighbor[n * 3];
                int ny = y + Neighbor[n * 3 + 1];
                int nz = z + Neighbor[n * 3 + 2];
                if ((uint)nx >= (uint)_sizeX || (uint)ny >= (uint)_sizeY || (uint)nz >= (uint)_sizeZ)
                    return true;
                if (_cells[Index(nx, ny, nz)] == Air)
                    return true;
            }

            return false;
        }

        void StampCapsule(Vector3 a, Vector3 b)
        {
            float dist = Vector3.Distance(a, b);
            int steps = Mathf.Max(1, Mathf.CeilToInt(dist / Mathf.Max(0.08f, _cellSize * 0.5f)));
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                StampPoint(a + (b - a) * t);
            }
        }

        void StampPoint(Vector3 p)
        {
            int x = Cell(p.x, _origin.x);
            int y = Cell(p.y, _origin.y);
            int z = Cell(p.z, _origin.z);
            if ((uint)x >= (uint)_sizeX || (uint)y >= (uint)_sizeY || (uint)z >= (uint)_sizeZ)
                return;
            _cells[Index(x, y, z)] = Occupied;
        }

        static void Encapsulate(
            Vector3 p,
            ref float minX,
            ref float minY,
            ref float minZ,
            ref float maxX,
            ref float maxY,
            ref float maxZ)
        {
            if (p.x < minX) minX = p.x;
            if (p.y < minY) minY = p.y;
            if (p.z < minZ) minZ = p.z;
            if (p.x > maxX) maxX = p.x;
            if (p.y > maxY) maxY = p.y;
            if (p.z > maxZ) maxZ = p.z;
        }

        bool IsBorder(int x, int y, int z)
        {
            return x == 0 || y == 0 || z == 0 ||
                   x == _sizeX - 1 || y == _sizeY - 1 || z == _sizeZ - 1;
        }

        bool IsGround(int y)
        {
            float worldY = _origin.y + (y + 0.5f) * _cellSize;
            return worldY < GroundY;
        }

        int Index(int x, int y, int z)
        {
            return x + _sizeX * (y + _sizeY * z);
        }

        void Decode(int index, out int x, out int y, out int z)
        {
            int area = _sizeX * _sizeY;
            z = index / area;
            int rem = index - z * area;
            y = rem / _sizeX;
            x = rem - y * _sizeX;
        }

        int Cell(float value, float origin)
        {
            return Mathf.FloorToInt((value - origin) / _cellSize);
        }
    }
}
