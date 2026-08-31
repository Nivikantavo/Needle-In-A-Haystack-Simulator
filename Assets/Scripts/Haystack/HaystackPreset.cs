using UnityEngine;

namespace Needle.Haystack
{
    [CreateAssetMenu(fileName = "HaystackPreset", menuName = "Haystack/Difficulty Preset")]
    public class HaystackPreset : ScriptableObject
    {
        [SerializeField] string _displayName;
        [SerializeField, Min(0.05f), Tooltip("Horizontal radius of the pile. Diameter is 2× this.")]
        float _radius = 2f;
        [SerializeField, Min(0.1f)] float _height = 2.2f;
        [SerializeField, Min(1), Tooltip("How many straws to generate. Larger piles take longer to sort.")]
        int _strawCount = 4000;
        [SerializeField, Tooltip("RNG start. Same seed rebuilds the same pile and needle index.")]
        int _seed = 1;

        public string DisplayName
        {
            get { return string.IsNullOrEmpty(_displayName) ? name : _displayName; }
        }

        public float Radius => Mathf.Max(0.05f, _radius);
        public float Diameter => Radius * 2f;
        public float Height => Mathf.Max(0.1f, _height);
        public int StrawCount => Mathf.Max(1, _strawCount);
        public int Seed => _seed;

        public HaystackLayoutSettings ToLayoutSettings(
            HaystackShape shape,
            float strawLength,
            float strawRadius,
            float horizontalFraction)
        {
            return new HaystackLayoutSettings
            {
                Shape = shape,
                Diameter = Diameter,
                Height = Height,
                StrawCount = StrawCount,
                Seed = Seed,
                StrawLength = strawLength,
                StrawRadius = strawRadius,
                HorizontalFraction = horizontalFraction
            };
        }

        void OnValidate()
        {
            _radius = Mathf.Max(0.05f, _radius);
            _height = Mathf.Max(0.1f, _height);
            _strawCount = Mathf.Max(1, _strawCount);
        }
    }
}
