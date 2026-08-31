using UnityEngine;

namespace Needle.Haystack
{
    [CreateAssetMenu(fileName = "HaystackPoses", menuName = "Haystack/Pose Asset")]
    public class HaystackPoseAsset : ScriptableObject
    {
        [SerializeField] Matrix4x4[] _matrices = System.Array.Empty<Matrix4x4>();
        [SerializeField] byte[] _flags = System.Array.Empty<byte>();
        [SerializeField] ushort[] _variants = System.Array.Empty<ushort>();
        [SerializeField] HaystackShape _shape = HaystackShape.Cylinder;
        [SerializeField] float _diameter = 4f;
        [SerializeField] float _height = 2.2f;
        [SerializeField] int _seed;
        [SerializeField] float _strawLength = 0.35f;
        [SerializeField] float _strawRadius = 0.012f;

        public Matrix4x4[] Matrices => _matrices;
        public byte[] Flags => _flags;
        public ushort[] Variants => _variants;
        public HaystackShape Shape => _shape;
        public float Diameter => _diameter;
        public float Height => _height;
        public int Seed => _seed;
        public int Count => _matrices != null ? _matrices.Length : 0;

        public Straw[] CopyStraws()
        {
            int n = Count;
            var straws = new Straw[n];
            for (int i = 0; i < n; i++)
            {
                straws[i].LocalToWorld = _matrices[i];
                straws[i].Flags = _flags != null && i < _flags.Length ? _flags[i] : (byte)0;
                straws[i].Variant = _variants != null && i < _variants.Length ? _variants[i] : (ushort)0;
            }

            return straws;
        }

        public void Apply(Straw[] straws, HaystackLayoutSettings settings)
        {
            int n = straws != null ? straws.Length : 0;
            _matrices = new Matrix4x4[n];
            _flags = new byte[n];
            _variants = new ushort[n];

            for (int i = 0; i < n; i++)
            {
                _matrices[i] = straws[i].LocalToWorld;
                _flags[i] = straws[i].Flags;
                _variants[i] = straws[i].Variant;
            }

            _shape = settings.Shape;
            _diameter = settings.Diameter;
            _height = settings.Height;
            _seed = settings.Seed;
            _strawLength = settings.StrawLength;
            _strawRadius = settings.StrawRadius;
        }

        public bool EnsureNeedle(int seed)
        {
            int n = Count;
            if (n <= 0)
                return false;

            EnsureFlagBuffer(n);
            if (StrawNeedle.FindIndex(_flags, n) >= 0)
                return false;

            StrawNeedle.StampInDepth(_matrices, _flags, n, _shape, _diameter, _height, seed);
            return StrawNeedle.FindIndex(_flags, n) >= 0;
        }

        public bool StampNeedleInDepth()
        {
            int n = Count;
            if (n <= 0)
                return false;

            EnsureFlagBuffer(n);
            int before = StrawNeedle.FindIndex(_flags, n);
            int after = StrawNeedle.StampInDepth(_matrices, _flags, n, _shape, _diameter, _height, _seed);
            return after != before;
        }

        void EnsureFlagBuffer(int n)
        {
            if (_flags != null && _flags.Length == n)
                return;

            var next = new byte[n];
            if (_flags != null)
                System.Array.Copy(_flags, next, System.Math.Min(_flags.Length, n));
            _flags = next;
        }
    }
}
