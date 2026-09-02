using System.Collections.Generic;
using ECM2.Examples.FirstPerson;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Needle.UI
{
    [DisallowMultipleComponent]
    public sealed class TouchLookInput : MonoBehaviour
    {
        [SerializeField] bool _invertLook = true;
        [SerializeField] Vector2 _sensitivity = new Vector2(0.12f, 0.12f);
        [SerializeField] float _minPitch = -80f;
        [SerializeField] float _maxPitch = 80f;

        readonly List<RaycastResult> _uiHits = new List<RaycastResult>(8);
        PointerEventData _pointerEventData;
        FirstPersonCharacter _character;
        int _lookFingerId = -1;
        bool _mouseLook;
        Vector2 _lastMousePosition;

        void Awake()
        {
            _character = GetComponent<FirstPersonCharacter>();
        }

        void OnDisable()
        {
            _lookFingerId = -1;
            _mouseLook = false;
        }

        void Update()
        {
            if (_character == null)
                return;

            if (Input.touchCount > 0)
            {
                HandleTouches();
                return;
            }

            HandleMouse();
        }

        void HandleTouches()
        {
            _mouseLook = false;
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                if (touch.phase == TouchPhase.Began)
                {
                    if (_lookFingerId < 0 && !IsOverUi(touch.position))
                        _lookFingerId = touch.fingerId;
                    continue;
                }

                if (touch.fingerId != _lookFingerId)
                    continue;

                if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
                    ApplyLook(touch.deltaPosition);
                else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                    _lookFingerId = -1;
            }
        }

        void HandleMouse()
        {
            _lookFingerId = -1;
            if (!Input.GetMouseButton(0))
            {
                _mouseLook = false;
                return;
            }

            Vector2 position = Input.mousePosition;
            if (!_mouseLook)
            {
                if (IsOverUi(position))
                    return;

                _mouseLook = true;
                _lastMousePosition = position;
                return;
            }

            ApplyLook(position - _lastMousePosition);
            _lastMousePosition = position;
        }

        void ApplyLook(Vector2 deltaPixels)
        {
            float yaw = deltaPixels.x * _sensitivity.x;
            float pitch = deltaPixels.y * _sensitivity.y;
            _character.AddControlYawInput(yaw);
            _character.AddControlPitchInput(_invertLook ? -pitch : pitch, _minPitch, _maxPitch);
        }

        bool IsOverUi(Vector2 screenPosition)
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
                return false;

            if (_pointerEventData == null)
                _pointerEventData = new PointerEventData(eventSystem);
            else
                _pointerEventData.Reset();

            _pointerEventData.position = screenPosition;
            _uiHits.Clear();
            eventSystem.RaycastAll(_pointerEventData, _uiHits);
            return _uiHits.Count > 0;
        }
    }
}
