using System;
using UnityEngine;
using UnityEngine.UI;

namespace Needle.UI
{
    [DisallowMultipleComponent]
    public sealed class SettingsPanel : MonoBehaviour
    {
        [SerializeField] Button _muteButton;
        [SerializeField] Slider _volumeSlider;
        [SerializeField] Button _languageButton;
        [SerializeField] Image _countryIcon;
        [SerializeField] Button _rateButton;
        [SerializeField] Button _otherGamesButton;
        [SerializeField] Button _closeButton;
        [SerializeField] Button _exitButton;
        [SerializeField] LanguagePanel _languagePanel;

        readonly PlaygamaSocialService _social = new PlaygamaSocialService();
        AudioSettingsService _audioSettings;
        LocaleSettingsService _localeSettings;
        bool _listenersBound;
        bool _suppressSliderCallback;

        public event Action Closed;
        public event Action ExitRequested;

        void Awake()
        {
            Validate();
        }

        void OnEnable()
        {
            BindListeners();
            ApplyPlatformButtons();
            SyncAudioWidgets();
        }

        void OnDisable()
        {
            UnbindListeners();
        }

        void OnDestroy()
        {
            if (_localeSettings != null)
                _localeSettings.Changed -= OnLocaleChanged;
        }

        public void Construct(AudioSettingsService audioSettings, LocaleSettingsService localeSettings)
        {
            if (audioSettings == null)
            {
                Debug.LogError("SettingsPanel: Construct received a null AudioSettingsService.", this);
                return;
            }

            if (localeSettings == null)
            {
                Debug.LogError("SettingsPanel: Construct received a null LocaleSettingsService.", this);
                return;
            }

            if (_localeSettings != null)
                _localeSettings.Changed -= OnLocaleChanged;

            _audioSettings = audioSettings;
            _localeSettings = localeSettings;
            _localeSettings.Changed += OnLocaleChanged;
            if (_languagePanel != null)
                _languagePanel.Construct(localeSettings);

            SyncCountryIcon();
        }

        public void Open(bool exitEnabled = false)
        {
            SetExitEnabled(exitEnabled);
            gameObject.SetActive(true);
            SyncCountryIcon();
        }

        public void Close()
        {
            if (_languagePanel != null)
                _languagePanel.Close();

            gameObject.SetActive(false);
        }

        void Validate()
        {
            RequiredRefs.Assign(_muteButton, nameof(_muteButton), this);
            RequiredRefs.Assign(_volumeSlider, nameof(_volumeSlider), this);
            RequiredRefs.Assign(_languageButton, nameof(_languageButton), this);
            CacheCountryIcon();
            RequiredRefs.Assign(_countryIcon, nameof(_countryIcon), this);
            RequiredRefs.Assign(_rateButton, nameof(_rateButton), this);
            RequiredRefs.Assign(_otherGamesButton, nameof(_otherGamesButton), this);
            RequiredRefs.Assign(_closeButton, nameof(_closeButton), this);
            RequiredRefs.Assign(_exitButton, nameof(_exitButton), this);
            RequiredRefs.Assign(_languagePanel, nameof(_languagePanel), this);
        }

        void BindListeners()
        {
            if (_listenersBound)
                return;

            if (_muteButton != null)
                _muteButton.onClick.AddListener(OnMuteClicked);
            if (_volumeSlider != null)
                _volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
            if (_languageButton != null)
                _languageButton.onClick.AddListener(OnLanguageClicked);
            if (_rateButton != null)
                _rateButton.onClick.AddListener(OnRateClicked);
            if (_closeButton != null)
                _closeButton.onClick.AddListener(OnCloseClicked);
            if (_exitButton != null)
                _exitButton.onClick.AddListener(OnExitClicked);

            _listenersBound = true;
        }

        void UnbindListeners()
        {
            if (!_listenersBound)
                return;

            if (_muteButton != null)
                _muteButton.onClick.RemoveListener(OnMuteClicked);
            if (_volumeSlider != null)
                _volumeSlider.onValueChanged.RemoveListener(OnVolumeChanged);
            if (_languageButton != null)
                _languageButton.onClick.RemoveListener(OnLanguageClicked);
            if (_rateButton != null)
                _rateButton.onClick.RemoveListener(OnRateClicked);
            if (_closeButton != null)
                _closeButton.onClick.RemoveListener(OnCloseClicked);
            if (_exitButton != null)
                _exitButton.onClick.RemoveListener(OnExitClicked);

            _listenersBound = false;
        }

        void CacheCountryIcon()
        {
            if (_countryIcon != null || _languageButton == null)
                return;

            Transform icon = _languageButton.transform.Find("Icon_Country");
            if (icon == null)
                return;

            _countryIcon = icon.GetComponent<Image>();
        }

        void SetExitEnabled(bool enabled)
        {
            if (_exitButton != null)
                _exitButton.gameObject.SetActive(enabled);
        }

        void ApplyPlatformButtons()
        {
            if (_otherGamesButton != null)
                _otherGamesButton.gameObject.SetActive(false);

            if (_rateButton != null)
                _rateButton.gameObject.SetActive(_social.IsRateSupported);
        }

        void SyncAudioWidgets()
        {
            if (_audioSettings == null)
            {
                Debug.LogError("SettingsPanel: AudioSettingsService was not constructed.", this);
                return;
            }

            if (_volumeSlider == null)
                return;

            _suppressSliderCallback = true;
            _volumeSlider.value = _audioSettings.Volume;
            _suppressSliderCallback = false;
        }

        void OnMuteClicked()
        {
            if (_audioSettings == null)
            {
                Debug.LogError("SettingsPanel: AudioSettingsService was not constructed.", this);
                return;
            }

            _audioSettings.ToggleMuted();
            SyncAudioWidgets();
        }

        void OnVolumeChanged(float value)
        {
            if (_suppressSliderCallback)
                return;

            if (_audioSettings == null)
            {
                Debug.LogError("SettingsPanel: AudioSettingsService was not constructed.", this);
                return;
            }

            _audioSettings.SetVolume(value);
        }

        void OnLocaleChanged(string _)
        {
            SyncCountryIcon();
        }

        void SyncCountryIcon()
        {
            if (_countryIcon == null)
                return;

            if (_languagePanel == null || _localeSettings == null)
                return;

            if (!_languagePanel.TryGetFlagSprite(_localeSettings.CurrentCode, out Sprite sprite))
                return;

            _countryIcon.sprite = sprite;
        }

        void OnLanguageClicked()
        {
            if (!RequiredRefs.Assign(_languagePanel, nameof(_languagePanel), this))
                return;

            _languagePanel.Open();
        }

        void OnRateClicked()
        {
            _social.Rate();
        }

        void OnCloseClicked()
        {
            Close();
            Closed?.Invoke();
        }

        void OnExitClicked()
        {
            ExitRequested?.Invoke();
        }
    }
}
