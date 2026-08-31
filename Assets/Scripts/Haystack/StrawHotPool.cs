using UnityEngine;
using UnityEngine.Rendering;

namespace Needle.Haystack
{
    public sealed class StrawHotPool
    {
        public const int DefaultSize = 4;

        Slot[] _slots;
        Transform _root;
        PhysicsMaterial _material;
        Mesh _strawMesh;
        Material _strawMaterial;
        int _hotLayer;
        int _heldSlot = -1;
        int _hoverSlot = -1;
        Color _hoverColor = new Color(1f, 0.92f, 0.38f, 1f);
        Color _baseColor = new Color(0.76f, 0.62f, 0.28f, 1f);

        struct Slot
        {
            public GameObject Go;
            public Transform Transform;
            public Rigidbody Body;
            public CapsuleCollider Capsule;
            public MeshFilter Filter;
            public MeshRenderer Renderer;
            public StrawHotBody Tag;
            public MaterialPropertyBlock Block;
            public float StillTime;
            public float Age;
        }

        public int HeldStrawIndex =>
            _heldSlot >= 0 && _slots != null ? _slots[_heldSlot].Tag.StrawIndex : -1;

        public bool IsHolding => _heldSlot >= 0;

        public void Configure(Color baseColor, Color hoverColor)
        {
            _baseColor = baseColor;
            _hoverColor = hoverColor;
        }

        public void Ensure(Transform parent, HaystackSystem owner, Mesh mesh, Material material, int size)
        {
            int wanted = Mathf.Clamp(size, 1, 8);
            _strawMesh = mesh;
            _strawMaterial = material;
            if (_slots != null && _slots.Length == wanted && _root != null)
            {
                BindFreeVisuals(mesh, material);
                return;
            }

            Release();
            _hotLayer = LayerMask.NameToLayer("HayHot");
            if (_hotLayer < 0)
                _hotLayer = 0;

            _root = new GameObject("HotPool").transform;
            _root.SetParent(parent, false);
            _material = CreatePhysicsMaterial();

            _slots = new Slot[wanted];
            for (int i = 0; i < wanted; i++)
                _slots[i] = CreateSlot(owner, mesh, material);
        }

        public void Release()
        {
            _heldSlot = -1;
            _hoverSlot = -1;
            if (_root != null)
            {
                Object.Destroy(_root.gameObject);
                _root = null;
            }

            _slots = null;
            _material = null;
            _strawMesh = null;
            _strawMaterial = null;
        }

        public bool TryPickPhysics(Ray ray, float maxDistance, out int strawIndex, out float distance)
        {
            strawIndex = -1;
            distance = 0f;
            if (_slots == null)
                return false;

            int mask = 1 << _hotLayer;
            if (!Physics.Raycast(ray, out RaycastHit hit, maxDistance, mask, QueryTriggerInteraction.Ignore))
                return false;

            var body = hit.collider != null ? hit.collider.GetComponent<StrawHotBody>() : null;
            if (body == null || body.StrawIndex < 0 || body.State == StrawHotState.Free)
                return false;
            if (body.State == StrawHotState.Held)
                return false;

            strawIndex = body.StrawIndex;
            distance = hit.distance;
            return true;
        }

        public bool TryPromote(
            HaystackSystem owner,
            int strawIndex,
            Matrix4x4 world,
            Mesh mesh,
            Material material,
            Collider[] ignoreWith)
        {
            if (IsHolding || strawIndex < 0)
                return false;

            int existing = FindSlot(strawIndex);
            if (existing >= 0)
                return TakeHeld(existing, ignoreWith);

            int free = FindFreeOrForceDemote(owner);
            if (free < 0)
                return false;

            Slot slot = _slots[free];
            ApplyPose(ref slot, world);
            slot.Filter.sharedMesh = mesh;
            slot.Renderer.sharedMaterial = material;
            slot.Tag.Owner = owner;
            slot.Tag.StrawIndex = strawIndex;
            _slots[free] = slot;
            return TakeHeld(free, ignoreWith);
        }

        public void FollowHold(Vector3 position, Quaternion rotation)
        {
            if (_heldSlot < 0)
                return;

            _slots[_heldSlot].Transform.SetPositionAndRotation(position, rotation);
        }

        public bool TryThrow(Vector3 velocity, out int strawIndex)
        {
            strawIndex = -1;
            if (_heldSlot < 0)
                return false;

            int slotIndex = _heldSlot;
            Slot slot = _slots[slotIndex];
            strawIndex = slot.Tag.StrawIndex;
            slot.Tag.State = StrawHotState.Settling;
            slot.StillTime = 0f;
            slot.Age = 0f;
            slot.Body.isKinematic = false;
            slot.Body.detectCollisions = true;
            slot.Body.useGravity = true;
            slot.Body.collisionDetectionMode = CollisionDetectionMode.Continuous;
            slot.Body.linearVelocity = velocity;
            slot.Body.angularVelocity = Vector3.zero;
            slot.Go.SetActive(true);
            _slots[slotIndex] = slot;
            _heldSlot = -1;
            return true;
        }

        public void TickSettling(
            HaystackSystem owner,
            float dt,
            float settleSpeed,
            float settleAngular,
            float settleHold,
            float maxAge)
        {
            if (_slots == null)
                return;

            float speedSq = settleSpeed * settleSpeed;
            float angSq = settleAngular * settleAngular;

            for (int i = 0; i < _slots.Length; i++)
            {
                Slot slot = _slots[i];
                if (slot.Tag.State != StrawHotState.Settling)
                    continue;

                slot.Age += dt;
                float lin = slot.Body.linearVelocity.sqrMagnitude;
                float ang = slot.Body.angularVelocity.sqrMagnitude;
                bool still = slot.Body.IsSleeping() || (lin <= speedSq && ang <= angSq);
                slot.StillTime = still ? slot.StillTime + dt : 0f;
                _slots[i] = slot;

                if (slot.StillTime >= settleHold || slot.Age >= maxAge)
                    Demote(owner, i);
            }
        }

        public void SetHovered(int strawIndex)
        {
            int next = strawIndex >= 0 ? FindSlot(strawIndex) : -1;
            if (next == _hoverSlot)
                return;

            if (_hoverSlot >= 0)
                ApplyHeldColor(_hoverSlot);
            _hoverSlot = next;
            if (_hoverSlot >= 0)
                ApplyColor(_hoverSlot, _hoverColor, true);
        }

        public void CaptureWorld(int strawIndex, out Matrix4x4 world)
        {
            int slot = FindSlot(strawIndex);
            if (slot < 0)
            {
                world = Matrix4x4.identity;
                return;
            }

            Transform t = _slots[slot].Transform;
            world = Matrix4x4.TRS(t.position, t.rotation, t.lossyScale);
        }

        void Demote(HaystackSystem owner, int slotIndex)
        {
            Slot slot = _slots[slotIndex];
            if (slot.Tag.State == StrawHotState.Free)
                return;

            int strawIndex = slot.Tag.StrawIndex;
            Matrix4x4 world = Matrix4x4.TRS(slot.Transform.position, slot.Transform.rotation, slot.Transform.lossyScale);
            if (owner != null && strawIndex >= 0)
                owner.DemoteStraw(strawIndex, world);

            slot.Tag.State = StrawHotState.Free;
            slot.Tag.StrawIndex = -1;
            slot.StillTime = 0f;
            slot.Age = 0f;
            slot.Body.isKinematic = true;
            slot.Body.detectCollisions = false;
            slot.Body.linearVelocity = Vector3.zero;
            slot.Body.angularVelocity = Vector3.zero;
            if (_strawMesh != null)
                slot.Filter.sharedMesh = _strawMesh;
            if (_strawMaterial != null)
                slot.Renderer.sharedMaterial = _strawMaterial;
            slot.Go.SetActive(false);
            _slots[slotIndex] = slot;
            ApplyColor(slotIndex, _baseColor, false);
            if (_heldSlot == slotIndex)
                _heldSlot = -1;
            if (_hoverSlot == slotIndex)
                _hoverSlot = -1;
        }

        int FindFreeOrForceDemote(HaystackSystem owner)
        {
            int oldest = -1;
            float oldestAge = -1f;
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i].Tag.State == StrawHotState.Free)
                    return i;
                if (_slots[i].Tag.State == StrawHotState.Settling && _slots[i].Age > oldestAge)
                {
                    oldestAge = _slots[i].Age;
                    oldest = i;
                }
            }

            if (oldest < 0)
                return -1;

            Demote(owner, oldest);
            return oldest;
        }

        bool TakeHeld(int slotIndex, Collider[] ignoreWith)
        {
            Slot slot = _slots[slotIndex];
            slot.Go.SetActive(true);
            slot.Tag.State = StrawHotState.Held;
            slot.StillTime = 0f;
            slot.Age = 0f;
            slot.Body.collisionDetectionMode = CollisionDetectionMode.Discrete;
            slot.Body.isKinematic = true;
            slot.Body.detectCollisions = false;
            slot.Body.useGravity = false;
            slot.Body.linearVelocity = Vector3.zero;
            slot.Body.angularVelocity = Vector3.zero;
            Ignore(slot.Capsule, ignoreWith, true);
            ApplyHeldColor(slotIndex);
            _slots[slotIndex] = slot;
            _heldSlot = slotIndex;
            return true;
        }

        int FindSlot(int strawIndex)
        {
            if (_slots == null || strawIndex < 0)
                return -1;
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i].Tag.StrawIndex == strawIndex)
                    return i;
            }

            return -1;
        }

        Slot CreateSlot(HaystackSystem owner, Mesh mesh, Material material)
        {
            var go = new GameObject("HotStraw");
            go.transform.SetParent(_root, false);
            go.layer = _hotLayer;
            go.SetActive(false);

            var filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;

            var capsule = go.AddComponent<CapsuleCollider>();
            capsule.direction = 1;
            capsule.center = Vector3.zero;
            capsule.sharedMaterial = _material;

            var body = go.AddComponent<Rigidbody>();
            body.mass = 0.03f;
            body.linearDamping = 0.35f;
            body.angularDamping = 0.55f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.Discrete;
            body.useGravity = false;
            body.isKinematic = true;
            body.detectCollisions = false;

            var tag = go.AddComponent<StrawHotBody>();
            tag.Owner = owner;
            tag.StrawIndex = -1;
            tag.State = StrawHotState.Free;

            return new Slot
            {
                Go = go,
                Transform = go.transform,
                Body = body,
                Capsule = capsule,
                Filter = filter,
                Renderer = renderer,
                Tag = tag,
                Block = new MaterialPropertyBlock()
            };
        }

        void BindFreeVisuals(Mesh mesh, Material material)
        {
            if (_slots == null)
                return;
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i].Tag.State != StrawHotState.Free)
                    continue;
                _slots[i].Filter.sharedMesh = mesh;
                _slots[i].Renderer.sharedMaterial = material;
            }
        }

        void ApplyHeldColor(int slotIndex)
        {
            if (UsesNeedleVisual(slotIndex))
            {
                _slots[slotIndex].Renderer.SetPropertyBlock(null);
                return;
            }

            ApplyColor(slotIndex, _baseColor, false);
        }

        bool UsesNeedleVisual(int slotIndex)
        {
            return _strawMesh != null && _slots[slotIndex].Filter.sharedMesh != _strawMesh;
        }

        static void ApplyPose(ref Slot slot, Matrix4x4 world)
        {
            Vector3 position = world.GetPosition();
            Quaternion rotation = world.rotation;
            Vector3 scale = new Vector3(
                world.GetColumn(0).magnitude,
                world.GetColumn(1).magnitude,
                world.GetColumn(2).magnitude);
            slot.Transform.SetPositionAndRotation(position, rotation);
            slot.Transform.localScale = scale;
            slot.Capsule.direction = 1;
            slot.Capsule.center = Vector3.zero;
            slot.Capsule.radius = 0.5f;
            slot.Capsule.height = 1f;
        }

        void ApplyColor(int slotIndex, Color color, bool emit)
        {
            if (UsesNeedleVisual(slotIndex))
                return;

            Slot slot = _slots[slotIndex];
            slot.Block.SetColor("_Color", color);
            slot.Block.SetColor("_EmissionColor", emit ? color * 0.35f : Color.black);
            slot.Renderer.SetPropertyBlock(slot.Block);
        }

        static void Ignore(Collider capsule, Collider[] others, bool ignore)
        {
            if (capsule == null || others == null)
                return;
            for (int i = 0; i < others.Length; i++)
            {
                if (others[i] != null)
                    Physics.IgnoreCollision(capsule, others[i], ignore);
            }
        }

        static PhysicsMaterial CreatePhysicsMaterial()
        {
            return new PhysicsMaterial("HayHot")
            {
                bounciness = 0.05f,
                staticFriction = 0.7f,
                dynamicFriction = 0.55f,
                frictionCombine = PhysicsMaterialCombine.Average,
                bounceCombine = PhysicsMaterialCombine.Minimum
            };
        }
    }
}
