using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Needle.UI
{
    [DisallowMultipleComponent]
    public sealed class VictoryPanel : MonoBehaviour
    {
        [SerializeField] Button _exitButton;
        [SerializeField] GameObject _congratulationsTitle;
        [SerializeField] GameObject _timeFrame;
        [SerializeField] TextMeshProUGUI _timerValue;

        bool _listenersBound;

        public event Action ExitRequested;

        void Awake()
        {
            Validate();
        }

        void OnEnable()
        {
            BindListeners();
        }

        void OnDisable()
        {
            UnbindListeners();
        }

        public void Open(bool timed = false, string formattedTime = null)
        {
            ApplyTimedLayout(timed, formattedTime);
            gameObject.SetActive(true);
        }

        public void Close()
        {
            gameObject.SetActive(false);
        }

        void ApplyTimedLayout(bool timed, string formattedTime)
        {
            if (_congratulationsTitle != null)
                _congratulationsTitle.SetActive(!timed);
            if (_timeFrame != null)
                _timeFrame.SetActive(timed);
            if (timed && _timerValue != null)
                _timerValue.text = formattedTime ?? string.Empty;
        }

        void Validate()
        {
            RequiredRefs.Assign(_exitButton, nameof(_exitButton), this);
            RequiredRefs.Assign(_congratulationsTitle, nameof(_congratulationsTitle), this);
            RequiredRefs.Assign(_timeFrame, nameof(_timeFrame), this);
            RequiredRefs.Assign(_timerValue, nameof(_timerValue), this);
        }

        void BindListeners()
        {
            if (_listenersBound)
                return;

            if (_exitButton != null)
                _exitButton.onClick.AddListener(OnExitClicked);

            _listenersBound = true;
        }

        void UnbindListeners()
        {
            if (!_listenersBound)
                return;

            if (_exitButton != null)
                _exitButton.onClick.RemoveListener(OnExitClicked);

            _listenersBound = false;
        }

        void OnExitClicked()
        {
            ExitRequested?.Invoke();
        }
    }
}
