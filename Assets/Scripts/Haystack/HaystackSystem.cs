using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;

namespace Needle.Haystack
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class HaystackSystem : MonoBehaviour
    {
        public const int DrawBatchSize = 1023;

        [Header("Render")]
        [SerializeField] Mesh _strawMesh;
        [SerializeField] Material _strawMaterial;
        [SerializeField] Mesh _needleMesh;
        [SerializeField] Material _needleMaterial;
        [SerializeField, Tooltip("Optional baked poses. Leave empty to generate from the selected difficulty preset.")]
        HaystackPoseAsset _poseAsset;

        [Header("Difficulty")]
        [SerializeField] HaystackPreset[] _presets = System.Array.Empty<HaystackPreset>();
        [SerializeField] int _presetIndex;

        [Header("Authoring")]
        [SerializeField] HaystackShape _shape = HaystackShape.Cylinder;
        [SerializeField, Min(0.1f)] float _diameter = 4f;
        [SerializeField, Min(0.1f)] float _height = 2.2f;
        [SerializeField] int _strawCount = 4000;
        [SerializeField, Tooltip("RNG start value. Same seed rebuilds the same positions, rotations, variants, and needle index.")]
        int _seed = 1;
        [SerializeField, Range(0f, 1f), Tooltip("Fraction of straws that lie mostly flat. The rest stay closer to vertical.")]
        float _horizontalFraction = 0.85f;
        [SerializeField, Min(0.01f)] float _strawLength = 0.35f;
        [SerializeField, Min(0.001f)] float _strawRadius = 0.012f;
        [SerializeField, Min(0.5f), Tooltip("Optional pose-settle quality. Player collision uses the PileVolume collider, not this bake.")]
        float _bakeSeconds = 4f;

        [Header("Pick")]
        [SerializeField, Min(1f), Tooltip("Pick capsule is thicker than the visual straw so a first-person aim can hit it.")]
        float _pickRadiusMultiplier = 3.5f;
        [SerializeField] Color _hoverColor = new Color(1f, 0.93f, 0.35f, 1f);
        [SerializeField, Range(1, 8)] int _hotPoolSize = StrawHotPool.DefaultSize;
        [SerializeField, Min(0.01f)] float _settleSpeed = 0.18f;
        [SerializeField, Min(0.01f)] float _settleAngular = 0.4f;
        [SerializeField, Min(0.05f)] float _settleHold = 0.35f;
        [SerializeField, Min(1f)] float _settleTimeout = 8f;
        [SerializeField] UnityEvent _onNeedleFound;

        readonly Matrix4x4[] _batch = new Matrix4x4[DrawBatchSize];
        readonly HaystackPileVolumeCollider _pileVolume = new HaystackPileVolumeCollider();
        readonly StrawPickQuery _pick = new StrawPickQuery(0.4f);
        readonly StrawHotPool _hotPool = new StrawHotPool();
        readonly StrawSurfaceMask _surface = new StrawSurfaceMask();

        Matrix4x4[] _runtimeMatrices;
        byte[] _runtimeFlags;
        int _runtimeCount;
        int _visualLayer;
        int _hoverIndex = -1;
        float _hoverDistance;
        int _needleIndex = -1;
        bool _needleFound;
        bool _volumeDirty;
        bool _hashDirty;
        bool _runtimeLoaded;
        float _collisionDiameter;
        float _collisionHeight;
        Material _hoverMaterial;

        public Mesh StrawMesh { get => _strawMesh; set => _strawMesh = value; }
        public Material StrawMaterial { get => _strawMaterial; set => _strawMaterial = value; }
        public Mesh NeedleMesh { get => _needleMesh; set => _needleMesh = value; }
        public Material NeedleMaterial { get => _needleMaterial; set => _needleMaterial = value; }
        public HaystackPoseAsset PoseAsset { get => _poseAsset; set => _poseAsset = value; }
        public HaystackPreset[] Presets { get => _presets; set => _presets = value; }
        public int PresetIndex { get => _presetIndex; set => SelectPreset(value); }
        public HaystackPreset ActivePreset => ResolvedPreset();
        public HaystackShape Shape { get => _shape; set => _shape = value; }
        public float Diameter
        {
            get
            {
                HaystackPreset preset = ResolvedPreset();
                return preset != null ? preset.Diameter : _diameter;
            }
            set => _diameter = value;
        }
        public float Height
        {
            get
            {
                HaystackPreset preset = ResolvedPreset();
                return preset != null ? preset.Height : _height;
            }
            set => _height = value;
        }
        public int StrawCount
        {
            get
            {
                HaystackPreset preset = ResolvedPreset();
                return preset != null ? preset.StrawCount : _strawCount;
            }
            set => _strawCount = value;
        }
        public int Seed
        {
            get
            {
                HaystackPreset preset = ResolvedPreset();
                return preset != null ? preset.Seed : _seed;
            }
            set => _seed = value;
        }
        public float HorizontalFraction { get => _horizontalFraction; set => _horizontalFraction = value; }
        public float StrawLength { get => _strawLength; set => _strawLength = value; }
        public float StrawRadius { get => _strawRadius; set => _strawRadius = value; }
        public float BakeSeconds { get => _bakeSeconds; set => _bakeSeconds = value; }

        public int HoverIndex => _hoverIndex;
        public int HeldStrawIndex => _hotPool.HeldStrawIndex;
        public bool IsHolding => _hotPool.IsHolding;
        public int NeedleIndex => _needleIndex;
        public bool NeedleWasFound => _needleFound;
        public bool IsHoldingNeedle => IsHolding && StrawNeedle.HasFlag(_runtimeFlags, HeldStrawIndex);
        public float CollisionDiameter => _collisionDiameter;
        public float CollisionHeight => _collisionHeight;
        public event Action<int> NeedleFound;

        public HaystackLayoutSettings GetLayoutSettings()
        {
            HaystackPreset preset = ResolvedPreset();
            if (preset != null)
                return preset.ToLayoutSettings(_shape, _strawLength, _strawRadius, _horizontalFraction);

            return new HaystackLayoutSettings
            {
                Shape = _shape,
                Diameter = _diameter,
                Height = _height,
                StrawCount = _strawCount,
                Seed = _seed,
                StrawLength = _strawLength,
                StrawRadius = _strawRadius,
                HorizontalFraction = _horizontalFraction
            };
        }

        public void SelectPreset(int index)
        {
            int count = _presets != null ? _presets.Length : 0;
            _presetIndex = count <= 0 ? 0 : Mathf.Clamp(index, 0, count - 1);
            _volumeDirty = true;
        }

        public void Rebuild()
        {
            if (!Application.isPlaying)
            {
                Debug.LogError("HaystackSystem: Rebuild() requires Play Mode.", this);
                return;
            }

            LoadRuntime();
        }

        public void ApplyGenerated(Straw[] straws)
        {
            int n = straws != null ? straws.Length : 0;
            _runtimeCount = n;
            _runtimeMatrices = n > 0 ? new Matrix4x4[n] : null;
            _runtimeFlags = n > 0 ? new byte[n] : null;
            for (int i = 0; i < n; i++)
            {
                _runtimeMatrices[i] = straws[i].LocalToWorld;
                _runtimeFlags[i] = straws[i].Flags;
            }

            _hoverIndex = -1;
            _hoverDistance = float.PositiveInfinity;
            _needleFound = false;
            _needleIndex = StrawNeedle.FindIndex(_runtimeFlags, _runtimeCount);
            _hashDirty = true;
            _runtimeLoaded = n > 0;
            RefreshSurface();
            SyncPileVolume();
        }

        void OnEnable()
        {
            CacheLayer();
            if (Application.isPlaying)
                LoadRuntime();
            SyncPileVolume();
        }

        void OnDisable()
        {
            _pileVolume.Release();
            if (Application.isPlaying)
                _hotPool.Release();
            DestroyHoverMaterial();
            _hoverIndex = -1;
            _runtimeLoaded = false;
        }

        void OnValidate()
        {
            _diameter = Mathf.Max(0.1f, _diameter);
            _height = Mathf.Max(0.1f, _height);
            _strawCount = Mathf.Max(1, _strawCount);
            _strawLength = Mathf.Max(0.01f, _strawLength);
            _strawRadius = Mathf.Max(0.001f, _strawRadius);
            _horizontalFraction = Mathf.Clamp01(_horizontalFraction);
            int presetCount = _presets != null ? _presets.Length : 0;
            _presetIndex = presetCount <= 0 ? 0 : Mathf.Clamp(_presetIndex, 0, presetCount - 1);
            _bakeSeconds = Mathf.Max(0.5f, _bakeSeconds);
            _pickRadiusMultiplier = Mathf.Max(1f, _pickRadiusMultiplier);
            _hotPoolSize = Mathf.Clamp(_hotPoolSize, 1, 8);
            CacheLayer();
            _volumeDirty = true;
        }

        void LateUpdate()
        {
            if (_volumeDirty)
            {
                _volumeDirty = false;
                SyncPileVolume();
            }

            DrawInstances();
        }

        void FixedUpdate()
        {
            if (!Application.isPlaying)
                return;

            _hotPool.TickSettling(this, Time.fixedDeltaTime, _settleSpeed, _settleAngular, _settleHold, _settleTimeout);
        }

        public void SyncPileVolume()
        {
            _volumeDirty = false;
            if (!isActiveAndEnabled)
                return;

            float diameter = Diameter;
            float height = Height;
            if (Application.isPlaying && _runtimeLoaded)
            {
                HaystackPileOccupancyResult occupancy = HaystackPileOccupancy.Measure(
                    Shape,
                    Diameter,
                    Height,
                    _runtimeMatrices,
                    _runtimeFlags,
                    _runtimeCount,
                    _strawLength * 0.5f);
                diameter = occupancy.Diameter;
                height = occupancy.Height;
            }

            _collisionDiameter = diameter;
            _collisionHeight = height;
            _pileVolume.Sync(transform, Shape, diameter, height);
        }

        public void TickHover(Ray ray, float maxDistance)
        {
            if (!Application.isPlaying || _runtimeCount <= 0)
            {
                SetHover(-1);
                return;
            }

            if (_hotPool.IsHolding)
            {
                SetHover(-1);
                return;
            }

            EnsureHotPool();
            RebuildHashIfNeeded();

            int next = -1;
            float distance = float.PositiveInfinity;
            if (_hotPool.TryPickPhysics(ray, maxDistance, out int hotIndex, out float hotDistance))
            {
                next = hotIndex;
                distance = hotDistance;
            }
            else if (_pick.TryPick(
                ray,
                maxDistance,
                _runtimeMatrices,
                _runtimeFlags,
                _runtimeCount,
                transform.localToWorldMatrix,
                _pickRadiusMultiplier,
                out int instanceIndex,
                out float pickDistance))
            {
                next = instanceIndex;
                distance = pickDistance;
            }

            _hoverDistance = distance;
            SetHover(next);
        }

        public bool TryGrabHovered(Collider[] ignoreColliders)
        {
            if (!Application.isPlaying || _hotPool.IsHolding || _hoverIndex < 0)
                return false;
            if ((uint)_hoverIndex >= (uint)_runtimeCount)
                return false;

            byte flag = _runtimeFlags[_hoverIndex];
            if ((flag & (byte)(StrawFlags.Buried | StrawFlags.Discarded)) != 0)
                return false;

            EnsureHotPool();
            Matrix4x4 world = WorldMatrix(_hoverIndex);
            ResolveHotVisual(flag, out Mesh mesh, out Material material);
            int grabbed = _hoverIndex;
            if (!_hotPool.TryPromote(this, grabbed, world, mesh, material, ignoreColliders))
                return false;

            _runtimeFlags[grabbed] = (byte)(flag | (byte)StrawFlags.Grabbed);
            RefreshSurface();
            SetHover(-1);
            SyncPileVolume();
            NotifyNeedleFound(grabbed);
            return true;
        }

        public bool TryThrow(Vector3 velocity)
        {
            if (!_hotPool.TryThrow(velocity, out _))
                return false;

            SetHover(-1);
            return true;
        }

        public void FollowHold(Vector3 position, Quaternion rotation)
        {
            _hotPool.FollowHold(position, rotation);
        }

        public void ClearHeld()
        {
            if (!Application.isPlaying || !_hotPool.IsHolding)
                return;

            _hotPool.DiscardHeld();
            SetHover(-1);
        }

        public int DiscardExposedHay(int count)
        {
            if (!Application.isPlaying || !_runtimeLoaded || count <= 0 || _runtimeFlags == null)
                return 0;

            int discarded = 0;
            int guard = _runtimeCount;
            while (discarded < count && guard-- > 0)
            {
                int marked = HaystackDiscard.MarkExposed(_runtimeFlags, _runtimeCount, count - discarded);
                if (marked <= 0)
                    break;

                discarded += marked;
                RefreshSurface();
            }

            if (discarded <= 0)
                return 0;

            if (_hoverIndex >= 0 &&
                _hoverIndex < _runtimeCount &&
                (_runtimeFlags[_hoverIndex] & (byte)StrawFlags.Discarded) != 0)
                SetHover(-1);

            SyncPileVolume();
            return discarded;
        }

        public void DemoteStraw(int strawIndex, Matrix4x4 world)
        {
            if ((uint)strawIndex >= (uint)_runtimeCount)
                return;

            Vector3 localPos = transform.InverseTransformPoint(world.GetPosition());
            Quaternion localRot = Quaternion.Inverse(transform.rotation) * world.rotation;
            Vector3 scale = new Vector3(
                world.GetColumn(0).magnitude,
                world.GetColumn(1).magnitude,
                world.GetColumn(2).magnitude);
            _runtimeMatrices[strawIndex] = Matrix4x4.TRS(localPos, localRot, scale);
            _runtimeFlags[strawIndex] = (byte)(_runtimeFlags[strawIndex] & ~(byte)StrawFlags.Grabbed);
            RefreshSurface();
            SyncPileVolume();
        }

        void SetHover(int index)
        {
            _hoverIndex = index;
            if (index < 0)
                _hoverDistance = float.PositiveInfinity;
            _hotPool.SetHovered(index);
        }

        void ResolveHotVisual(byte flag, out Mesh mesh, out Material material)
        {
            bool needle = StrawNeedle.HasFlag(flag) && _needleMesh != null;
            mesh = needle ? _needleMesh : _strawMesh;
            material = needle && _needleMaterial != null ? _needleMaterial : _strawMaterial;
        }

        void NotifyNeedleFound(int index)
        {
            if (_needleFound || !StrawNeedle.HasFlag(_runtimeFlags, index))
                return;

            _needleFound = true;
            NeedleFound?.Invoke(index);
            _onNeedleFound?.Invoke();
        }

        void LoadRuntime()
        {
            HaystackPreset preset = ResolvedPreset();
            if (preset != null || _poseAsset == null || _poseAsset.Count <= 0)
            {
                ApplyGenerated(HaystackLayout.Generate(GetLayoutSettings()));
                return;
            }

            _hoverIndex = -1;
            _hoverDistance = float.PositiveInfinity;
            _needleIndex = -1;
            _needleFound = false;
            _hashDirty = true;
            Matrix4x4[] source = _poseAsset.Matrices;
            _runtimeCount = source != null ? source.Length : 0;
            _runtimeMatrices = new Matrix4x4[_runtimeCount];
            _runtimeFlags = new byte[_runtimeCount];
            byte[] flags = _poseAsset.Flags;
            for (int i = 0; i < _runtimeCount; i++)
            {
                _runtimeMatrices[i] = source[i];
                _runtimeFlags[i] = flags != null && i < flags.Length ? flags[i] : (byte)0;
            }

            _needleIndex = StrawNeedle.FindIndex(_runtimeFlags, _runtimeCount);
            _runtimeLoaded = _runtimeCount > 0;
            RefreshSurface();
        }

        HaystackPreset ResolvedPreset()
        {
            if (_presets == null || _presets.Length == 0)
                return null;

            int index = Mathf.Clamp(_presetIndex, 0, _presets.Length - 1);
            return _presets[index];
        }

        void RefreshSurface()
        {
            _surface.Apply(Shape, Diameter, Height, _runtimeMatrices, _runtimeFlags, _runtimeCount);
            _hashDirty = true;
        }

        void RebuildHashIfNeeded()
        {
            if (!_hashDirty)
                return;
            _pick.Rebuild(_runtimeMatrices, _runtimeFlags, _runtimeCount, transform.localToWorldMatrix);
            _hashDirty = false;
        }

        void EnsureHotPool()
        {
            if (_strawMesh == null || _strawMaterial == null)
                return;

            Color baseColor = _strawMaterial.HasProperty("_Color")
                ? _strawMaterial.GetColor("_Color")
                : new Color(0.76f, 0.62f, 0.28f, 1f);
            _hotPool.Configure(baseColor, _hoverColor);
            _hotPool.Ensure(transform, this, _strawMesh, _strawMaterial, _hotPoolSize);
        }

        Matrix4x4 WorldMatrix(int index)
        {
            return transform.localToWorldMatrix * _runtimeMatrices[index];
        }

        void CacheLayer()
        {
            int layer = LayerMask.NameToLayer("HayVisual");
            _visualLayer = layer >= 0 ? layer : 0;
        }

        void DrawInstances()
        {
            if (!isActiveAndEnabled)
                return;
            if (_strawMesh == null || _strawMaterial == null)
                return;

            Matrix4x4[] locals;
            byte[] flags;
            int count;
            int hover = -1;

            if (_runtimeMatrices != null && _runtimeCount > 0)
            {
                locals = _runtimeMatrices;
                flags = _runtimeFlags;
                count = _runtimeCount;
                hover = Application.isPlaying ? _hoverIndex : -1;
            }
            else
            {
                if (_poseAsset == null)
                    return;
                locals = _poseAsset.Matrices;
                flags = _poseAsset.Flags;
                count = locals != null ? locals.Length : 0;
            }

            if (locals == null || count == 0)
                return;

            if (!_strawMaterial.enableInstancing)
            {
#if UNITY_EDITOR
                _strawMaterial.enableInstancing = true;
#else
                return;
#endif
            }

            Matrix4x4 parent = transform.localToWorldMatrix;
            int index = 0;
            while (index < count)
            {
                int batchCount = 0;
                while (batchCount < DrawBatchSize && index < count)
                {
                    byte flag = flags != null && index < flags.Length ? flags[index] : (byte)0;
                    bool hidden = (flag & (byte)(StrawFlags.Grabbed | StrawFlags.Discarded)) != 0;
                    bool hovered = index == hover;
                    bool isNeedle = StrawNeedle.HasFlag(flag);
                    if (!hidden && !hovered && !isNeedle)
                    {
                        _batch[batchCount] = parent * locals[index];
                        batchCount++;
                    }

                    index++;
                }

                if (batchCount == 0)
                    continue;

                Graphics.DrawMeshInstanced(
                    _strawMesh,
                    0,
                    _strawMaterial,
                    _batch,
                    batchCount,
                    null,
                    ShadowCastingMode.Off,
                    false,
                    _visualLayer,
                    null);
            }

            int needle = _runtimeLoaded ? _needleIndex : StrawNeedle.FindIndex(flags, count);
            if (needle >= 0 && needle < count && needle != hover)
            {
                byte needleFlag = flags != null && needle < flags.Length ? flags[needle] : (byte)0;
                if ((needleFlag & (byte)(StrawFlags.Grabbed | StrawFlags.Discarded)) == 0)
                    DrawSingle(parent * locals[needle], true, false);
            }

            if (hover >= 0 && hover < count)
            {
                byte flag = flags != null && hover < flags.Length ? flags[hover] : (byte)0;
                if ((flag & (byte)(StrawFlags.Grabbed | StrawFlags.Discarded)) == 0)
                    DrawSingle(parent * locals[hover], StrawNeedle.HasFlag(flag), true);
            }
        }

        void DrawSingle(Matrix4x4 world, bool isNeedle, bool hovered)
        {
            Mesh mesh = isNeedle && _needleMesh != null ? _needleMesh : _strawMesh;
            Material material;
            if (isNeedle && _needleMaterial != null)
                material = _needleMaterial;
            else if (hovered)
                material = HoverMaterial();
            else
                material = _strawMaterial;

            if (mesh == null || material == null)
                return;

            Graphics.DrawMesh(
                mesh,
                world,
                material,
                _visualLayer,
                null,
                0,
                null,
                ShadowCastingMode.Off,
                false);
        }

        Material HoverMaterial()
        {
            if (_strawMaterial == null)
                return null;

            if (_hoverMaterial == null)
            {
                _hoverMaterial = new Material(_strawMaterial);
                _hoverMaterial.name = "StrawHover";
                _hoverMaterial.enableInstancing = false;
                _hoverMaterial.EnableKeyword("_EMISSION");
            }

            _hoverMaterial.SetColor("_Color", _hoverColor);
            _hoverMaterial.SetColor("_EmissionColor", _hoverColor * 0.45f);
            return _hoverMaterial;
        }

        void DestroyHoverMaterial()
        {
            if (_hoverMaterial == null)
                return;

            if (Application.isPlaying)
                Destroy(_hoverMaterial);
            else
                DestroyImmediate(_hoverMaterial);
            _hoverMaterial = null;
        }

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            Matrix4x4 restore = Gizmos.matrix;
            Color restoreColor = Gizmos.color;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(0.85f, 0.7f, 0.2f, 0.35f);
            float gizmoDiameter = Application.isPlaying ? _collisionDiameter : Diameter;
            float gizmoHeight = Application.isPlaying ? _collisionHeight : Height;
            if (gizmoHeight <= 0.001f)
            {
                Gizmos.matrix = restore;
                Gizmos.color = restoreColor;
                return;
            }

            switch (Shape)
            {
                case HaystackShape.Box:
                    Gizmos.DrawWireCube(new Vector3(0f, gizmoHeight * 0.5f, 0f), new Vector3(gizmoDiameter, gizmoHeight, gizmoDiameter));
                    break;
                case HaystackShape.Cylinder:
                    DrawWireCylinder(gizmoDiameter, gizmoHeight);
                    break;
                case HaystackShape.Hemisphere:
                    DrawWireHemisphere(gizmoDiameter, gizmoHeight);
                    break;
                case HaystackShape.Pyramid:
                    DrawWirePyramid(gizmoDiameter, gizmoHeight);
                    break;
            }

            Gizmos.matrix = restore;
            Gizmos.color = restoreColor;
        }

        void DrawWireCylinder(float diameter, float height)
        {
            float radius = diameter * 0.5f;
            const int rings = 4;
            for (int i = 0; i <= rings; i++)
            {
                float y = height * i / rings;
                DrawWireCircle(new Vector3(0f, y, 0f), radius);
            }
        }

        void DrawWireHemisphere(float diameter, float height)
        {
            float radius = diameter * 0.5f;
            const int rings = 4;
            for (int i = 0; i <= rings; i++)
            {
                float t = i / (float)rings;
                float y = height * t;
                float scale = Mathf.Sqrt(Mathf.Max(0f, 1f - t * t));
                DrawWireCircle(new Vector3(0f, y, 0f), radius * scale);
            }
        }

        void DrawWirePyramid(float diameter, float height)
        {
            float half = diameter * 0.5f;
            Vector3 apex = new Vector3(0f, height, 0f);
            Vector3 a = new Vector3(-half, 0f, -half);
            Vector3 b = new Vector3(half, 0f, -half);
            Vector3 c = new Vector3(half, 0f, half);
            Vector3 d = new Vector3(-half, 0f, half);
            Gizmos.DrawLine(a, b);
            Gizmos.DrawLine(b, c);
            Gizmos.DrawLine(c, d);
            Gizmos.DrawLine(d, a);
            Gizmos.DrawLine(a, apex);
            Gizmos.DrawLine(b, apex);
            Gizmos.DrawLine(c, apex);
            Gizmos.DrawLine(d, apex);
        }

        static void DrawWireCircle(Vector3 center, float radius)
        {
            const int segments = 24;
            Vector3 prev = center + new Vector3(radius, 0f, 0f);
            for (int i = 1; i <= segments; i++)
            {
                float a = i / (float)segments * Mathf.PI * 2f;
                Vector3 next = center + new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius);
                Gizmos.DrawLine(prev, next);
                prev = next;
            }
        }
#endif
    }
}
