using Needle.Haystack;
using UnityEngine;
using UnityEngine.UI;

namespace Needle.UI
{
    [DisallowMultipleComponent]
    public sealed class HaystackMobileControls : MonoBehaviour
    {
        [SerializeField] GameObject _joystickRoot;
        [SerializeField] Button _collectButton;
        [SerializeField] HaystackGrabInput _grabInput;
        [SerializeField] GraphicRaycaster _raycaster;
        [SerializeField] CanvasGroup _canvasGroup;

        bool _listenersBound;

        public GameObject JoystickRoot { get => _joystickRoot; set => _joystickRoot = value; }
        public Button CollectButton { get => _collectButton; set => _collectButton = value; }
        public HaystackGrabInput GrabInput { get => _grabInput; set => _grabInput = value; }

        void Awake()
        {
            if (_raycaster == null)
                _raycaster = GetComponent<GraphicRaycaster>();
            if (_canvasGroup == null)
                _canvasGroup = GetComponent<CanvasGroup>();

            DisableDecorativeRaycasts();
            SetGameplayActive(false);
        }

        void OnEnable()
        {
            BindCollect();
        }

        void OnDisable()
        {
            UnbindCollect();
        }

        public void SetGameplayActive(bool active)
        {
            if (_joystickRoot != null)
                _joystickRoot.SetActive(active);
            if (_collectButton != null)
                _collectButton.gameObject.SetActive(active);
            if (_raycaster != null)
                _raycaster.enabled = active;
            if (_canvasGroup != null)
            {
                _canvasGroup.interactable = active;
                _canvasGroup.blocksRaycasts = active;
            }
        }

        void BindCollect()
        {
            if (_listenersBound || _collectButton == null)
                return;

            _collectButton.onClick.AddListener(OnCollectClicked);
            _listenersBound = true;
        }

        void UnbindCollect()
        {
            if (!_listenersBound || _collectButton == null)
                return;

            _collectButton.onClick.RemoveListener(OnCollectClicked);
            _listenersBound = false;
        }

        void OnCollectClicked()
        {
            if (_grabInput != null)
                _grabInput.Collect();
        }

        void DisableDecorativeRaycasts()
        {
            Transform scope = transform.Find("scope");
            if (scope == null)
                return;

            var image = scope.GetComponent<Image>();
            if (image != null)
                image.raycastTarget = false;
        }
    }
}
