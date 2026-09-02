using UnityEngine;

namespace Needle.Haystack
{
    [DisallowMultipleComponent]
    public sealed class HaystackGrabInput : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] HaystackSystem _haystack;
        [SerializeField] Camera _camera;
        [SerializeField] Transform _holdPoint;

        [Header("Reach")]
        [SerializeField, Min(0.2f)] float _pickDistance = 2.4f;
        [SerializeField] Vector3 _holdOffset = new Vector3(0.28f, -0.2f, 0.55f);

        [Header("Throw")]
        [SerializeField, Min(0.1f)] float _throwSpeed = 8f;
        [SerializeField] float _throwUp = 1.2f;

        Collider[] _ignoreColliders;
        bool _useMouseGrab = true;

        public HaystackSystem Haystack { get => _haystack; set => _haystack = value; }
        public Camera AimCamera { get => _camera; set => _camera = value; }
        public float PickDistance { get => _pickDistance; set => _pickDistance = value; }
        public bool UseMouseGrab { get => _useMouseGrab; set => _useMouseGrab = value; }

        void Awake()
        {
            if (_haystack == null)
                _haystack = FindFirstObjectByType<HaystackSystem>();
            if (_camera == null)
                _camera = ResolveCamera();
            _ignoreColliders = GetComponentsInChildren<Collider>(true);
        }

        void Update()
        {
            if (_haystack == null)
                _haystack = FindFirstObjectByType<HaystackSystem>();
            if (_camera == null)
                _camera = ResolveCamera();
            if (_haystack == null || _camera == null)
                return;

            Ray ray = CenterRay();
            _haystack.TickHover(ray, _pickDistance);

            if (_useMouseGrab && PressedGrab())
                Collect();
        }

        public void Collect()
        {
            if (!isActiveAndEnabled || _haystack == null || _camera == null)
                return;

            if (_haystack.IsHolding)
            {
                Vector3 velocity = _camera.transform.forward * _throwSpeed + Vector3.up * _throwUp;
                _haystack.TryThrow(velocity);
                return;
            }

            _haystack.TryGrabHovered(_ignoreColliders);
        }

        void LateUpdate()
        {
            if (_haystack == null || _camera == null || !_haystack.IsHolding)
                return;

            GetHoldPose(out Vector3 position, out Quaternion rotation);
            _haystack.FollowHold(position, rotation);
        }

        bool PressedGrab()
        {
            return Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1);
        }

        Ray CenterRay()
        {
            return _camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        }

        void GetHoldPose(out Vector3 position, out Quaternion rotation)
        {
            if (_holdPoint != null)
            {
                position = _holdPoint.position;
                rotation = _holdPoint.rotation;
                return;
            }

            Transform cam = _camera.transform;
            position = cam.TransformPoint(_holdOffset);
            rotation = cam.rotation * Quaternion.Euler(80f, 0f, 20f);
        }

        Camera ResolveCamera()
        {
            Camera child = GetComponentInChildren<Camera>();
            if (child != null)
                return child;

            return Camera.main;
        }
    }
}
