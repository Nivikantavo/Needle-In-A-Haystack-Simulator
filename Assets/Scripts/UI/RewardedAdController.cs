using System;
using Needle.Haystack;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

namespace Needle.UI
{
    [DisallowMultipleComponent]
    public sealed class RewardedAdController : MonoBehaviour
    {
        public const string LocalizationTable = "Game";
        public const string LocalizationKey = "remove pieces";
        public const string FallbackTemplate = "remove {0} pieces";
        public const string Placement = "gameplay_rewarded_discard";

        [SerializeField] Button _button;
        [SerializeField] TextMeshProUGUI _label;
        [SerializeField] HaystackSystem _haystack;
        [SerializeField, Min(1)] int _strawRewardCount = 200;

        readonly PlaygamaAdvertisementService _ads = new PlaygamaAdvertisementService();
        bool _sessionActive;
        bool _waitingForAd;
        bool _rewardGranted;
        bool _eventsBound;

        public event Action PauseRequested;
        public event Action ResumeRequested;

        public int StrawRewardCount => Mathf.Max(1, _strawRewardCount);

        public static string FormatLabel(string template, int count)
        {
            if (string.IsNullOrEmpty(template))
                template = FallbackTemplate;

            return string.Format(template, count);
        }

        public void StartSession()
        {
            BindEvents();
            _ads.Subscribe();
            _sessionActive = true;
            _waitingForAd = false;
            _rewardGranted = false;
            RefreshLabel();
            SetButtonInteractable(true);
        }

        public void StopSession()
        {
            _sessionActive = false;
            _waitingForAd = false;
            _rewardGranted = false;
            SetButtonInteractable(true);
            _ads.Unsubscribe();
            UnbindEvents();
        }

        public void SetVisible(bool visible)
        {
            if (_button != null)
                _button.gameObject.SetActive(visible);
        }

        void OnEnable()
        {
            if (_button != null)
                _button.onClick.AddListener(OnClicked);
            LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
        }

        void OnDisable()
        {
            if (_button != null)
                _button.onClick.RemoveListener(OnClicked);
            LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
        }

        void OnDestroy()
        {
            StopSession();
        }

        void OnValidate()
        {
            _strawRewardCount = Mathf.Max(1, _strawRewardCount);
        }

        void BindEvents()
        {
            if (_eventsBound)
                return;

            _ads.RewardedStateChanged += OnAdStateChanged;
            _eventsBound = true;
        }

        void UnbindEvents()
        {
            if (!_eventsBound)
                return;

            _ads.RewardedStateChanged -= OnAdStateChanged;
            _eventsBound = false;
        }

        void OnClicked()
        {
            if (!_sessionActive || _waitingForAd)
                return;
            if (!RequiredRefs.Assign(_haystack, nameof(_haystack), this))
                return;

            _waitingForAd = true;
            _rewardGranted = false;
            SetButtonInteractable(false);
            PauseRequested?.Invoke();

            if (!_ads.IsRewardedSupported)
            {
                GrantReward();
                FinishAd();
                return;
            }

            _ads.ShowRewarded(Placement);
        }

        void OnAdStateChanged(RewardedAdState state)
        {
            if (!_sessionActive || !_waitingForAd)
                return;

            if (state == RewardedAdState.Rewarded)
            {
                GrantReward();
                return;
            }

            if (state != RewardedAdState.Closed && state != RewardedAdState.Failed)
                return;

            FinishAd();
        }

        void GrantReward()
        {
            if (_rewardGranted)
                return;

            _rewardGranted = true;
            if (_haystack != null)
                _haystack.DiscardExposedHay(StrawRewardCount);
        }

        void FinishAd()
        {
            _waitingForAd = false;
            SetButtonInteractable(true);
            ResumeRequested?.Invoke();
        }

        void OnLocaleChanged(Locale _)
        {
            RefreshLabel();
        }

        void RefreshLabel()
        {
            if (!RequiredRefs.Assign(_label, nameof(_label), this))
                return;

            _label.text = FormatLabel(GetLocalizedTemplate(), StrawRewardCount);
        }

        void SetButtonInteractable(bool interactable)
        {
            if (_button != null)
                _button.interactable = interactable;
        }

        static string GetLocalizedTemplate()
        {
            string value = LocalizationSettings.StringDatabase.GetLocalizedString(
                LocalizationTable,
                LocalizationKey);
            return string.IsNullOrEmpty(value) ? FallbackTemplate : value;
        }
    }
}
