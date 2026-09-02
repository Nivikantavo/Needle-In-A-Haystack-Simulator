using System;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;

namespace Needle.UI
{
    [DisallowMultipleComponent]
    public sealed class InterstitialAdController : MonoBehaviour
    {
        public const string LocalizationTable = "Game";
        public const string LocalizationKey = "Ad will launch after";
        public const string Placement = "gameplay_interval";

        [SerializeField] TextMeshProUGUI _label;

        readonly InterstitialAdSchedule _schedule = new InterstitialAdSchedule();
        readonly PlaygamaAdvertisementService _ads = new PlaygamaAdvertisementService();
        bool _sessionActive;
        bool _waitingForAd;
        bool _eventsBound;

        public event Action PauseRequested;
        public event Action ResumeRequested;

        public static string FormatCountdown(string localizedPrefix, int seconds)
        {
            if (string.IsNullOrEmpty(localizedPrefix))
                localizedPrefix = LocalizationKey;

            return $"{localizedPrefix} {seconds}";
        }

        public void StartSession()
        {
            BindEvents();
            _ads.Subscribe();
            _sessionActive = true;
            _waitingForAd = false;
            _schedule.Reset();
            _schedule.SetFrozen(false);
            HideAlarm();
        }

        public void StopSession()
        {
            _sessionActive = false;
            _waitingForAd = false;
            _schedule.Reset();
            _schedule.SetFrozen(false);
            HideAlarm();
            _ads.Unsubscribe();
            UnbindEvents();
        }

        public void SetFrozen(bool frozen)
        {
            _schedule.SetFrozen(frozen);
        }

        public void Tick(float unscaledDeltaTime)
        {
            if (!_sessionActive)
                return;

            _schedule.Tick(unscaledDeltaTime);
        }

        void OnDestroy()
        {
            StopSession();
        }

        void BindEvents()
        {
            if (_eventsBound)
                return;

            _schedule.CountdownChanged += OnCountdownChanged;
            _schedule.ShowRequested += OnShowRequested;
            _ads.InterstitialStateChanged += OnAdStateChanged;
            _eventsBound = true;
        }

        void UnbindEvents()
        {
            if (!_eventsBound)
                return;

            _schedule.CountdownChanged -= OnCountdownChanged;
            _schedule.ShowRequested -= OnShowRequested;
            _ads.InterstitialStateChanged -= OnAdStateChanged;
            _eventsBound = false;
        }

        void OnCountdownChanged(int seconds)
        {
            if (!_sessionActive)
                return;

            if (!RequiredRefs.Assign(_label, nameof(_label), this))
                return;

            if (!gameObject.activeSelf)
                gameObject.SetActive(true);

            _label.text = FormatCountdown(GetLocalizedPrefix(), seconds);
        }

        void OnShowRequested()
        {
            if (!_sessionActive)
                return;

            HideAlarm();
            _waitingForAd = true;
            PauseRequested?.Invoke();
            _ads.ShowInterstitial(Placement);
        }

        void OnAdStateChanged(InterstitialAdState state)
        {
            if (!_sessionActive || !_waitingForAd)
                return;

            if (state != InterstitialAdState.Closed && state != InterstitialAdState.Failed)
                return;

            _waitingForAd = false;
            _schedule.NotifyAdFinished();
            ResumeRequested?.Invoke();
        }

        void HideAlarm()
        {
            if (gameObject.activeSelf)
                gameObject.SetActive(false);
        }

        static string GetLocalizedPrefix()
        {
            string value = LocalizationSettings.StringDatabase.GetLocalizedString(
                LocalizationTable,
                LocalizationKey);
            return string.IsNullOrEmpty(value) ? LocalizationKey : value;
        }
    }
}
