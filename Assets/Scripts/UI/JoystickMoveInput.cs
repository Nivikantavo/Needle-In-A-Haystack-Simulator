using ECM2;
using UnityEngine;

namespace Needle.UI
{
    [DisallowMultipleComponent]
    public sealed class JoystickMoveInput : MonoBehaviour
    {
        [SerializeField] Joystick _joystick;

        Character _character;

        public Joystick Joystick
        {
            get => _joystick;
            set => _joystick = value;
        }

        void Awake()
        {
            _character = GetComponent<Character>();
        }

        void OnDisable()
        {
            if (_character != null)
                _character.SetMovementDirection(Vector3.zero);
        }

        void Update()
        {
            if (_character == null || _joystick == null)
                return;

            Vector3 movementDirection = Vector3.zero;
            movementDirection += _character.GetRightVector() * _joystick.Horizontal;
            movementDirection += _character.GetForwardVector() * _joystick.Vertical;
            _character.SetMovementDirection(movementDirection);
        }
    }
}
