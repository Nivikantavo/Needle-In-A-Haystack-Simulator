using System;
using UnityEngine;
using UnityEngine.UI;

namespace Needle.UI
{
    [DisallowMultipleComponent]
    public sealed class LanguagePanel : MonoBehaviour
    {
        [SerializeField] LanguageOption[] _options;
        [SerializeField] Button _closeButton;

        LocaleSettingsService _localeSettings;
        bool _listenersBound;

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

        public void Construct(LocaleSettingsService localeSettings)
        {
            if (localeSettings == null)
            {
                Debug.LogError("LanguagePanel: Construct received a null LocaleSettingsService.", this);
                return;
            }

            _localeSettings = localeSettings;
        }

        public void Open()
        {
            gameObject.SetActive(true);
        }

        public void Close()
        {
            gameObject.SetActive(false);
        }

        void Validate()
        {
            RequiredRefs.Assign(_closeButton, nameof(_closeButton), this);
            if (_options == null || _options.Length == 0)
            {
                Debug.LogError("LanguagePanel: serialized field '_options' is empty.", this);
                return;
            }

            for (int i = 0; i < _options.Length; i++)
            {
                LanguageOption option = _options[i];
                if (option == null)
                {
                    Debug.LogError($"LanguagePanel: serialized field '_options[{i}]' is not assigned.", this);
                    continue;
                }

                RequiredRefs.Assign(option.FlagButton, $"_options[{i}].FlagButton", this);
                RequiredRefs.AssignString(option.LocaleCode, $"_options[{i}].LocaleCode", this);
            }
        }

        void BindListeners()
        {
            if (_listenersBound)
                return;

            if (_closeButton != null)
                _closeButton.onClick.AddListener(Close);

            if (_options != null)
            {
                for (int i = 0; i < _options.Length; i++)
                    BindOption(_options[i], i);
            }

            _listenersBound = true;
        }

        void BindOption(LanguageOption option, int index)
        {
            if (option == null || option.FlagButton == null)
                return;

            int captured = index;
            option.FlagButton.onClick.AddListener(() => OnLocaleClicked(captured));
        }

        void UnbindListeners()
        {
            if (!_listenersBound)
                return;

            if (_closeButton != null)
                _closeButton.onClick.RemoveListener(Close);

            if (_options != null)
            {
                for (int i = 0; i < _options.Length; i++)
                {
                    if (_options[i] != null && _options[i].FlagButton != null)
                        _options[i].FlagButton.onClick.RemoveAllListeners();
                }
            }

            _listenersBound = false;
        }

        void OnLocaleClicked(int index)
        {
            if (_localeSettings == null)
            {
                Debug.LogError("LanguagePanel: LocaleSettingsService was not constructed.", this);
                return;
            }

            if (_options == null || index < 0 || index >= _options.Length)
            {
                Debug.LogError($"LanguagePanel: locale option index {index} is invalid.", this);
                return;
            }

            LanguageOption option = _options[index];
            if (option == null)
            {
                Debug.LogError($"LanguagePanel: serialized field '_options[{index}]' is not assigned.", this);
                return;
            }

            if (!RequiredRefs.AssignString(option.LocaleCode, $"_options[{index}].LocaleCode", this))
                return;

            _localeSettings.Apply(option.LocaleCode);
            Close();
        }

        public bool TryGetFlagSprite(string localeCode, out Sprite sprite)
        {
            sprite = null;
            if (string.IsNullOrWhiteSpace(localeCode) || _options == null)
                return false;

            for (int i = 0; i < _options.Length; i++)
            {
                LanguageOption option = _options[i];
                if (option == null ||
                    !string.Equals(option.LocaleCode, localeCode, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (option.FlagButton == null)
                    return false;

                Image flagImage = option.FlagButton.image;
                sprite = flagImage != null ? flagImage.sprite : null;
                return sprite != null;
            }

            return false;
        }

        [Serializable]
        public sealed class LanguageOption
        {
            [SerializeField] Button _flagButton;
            [SerializeField] string _localeCode;

            public Button FlagButton => _flagButton;
            public string LocaleCode => _localeCode;
        }
    }
}
