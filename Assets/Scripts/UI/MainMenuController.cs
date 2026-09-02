using System.Collections;
using System.Collections.Generic;
using ECM2;
using ECM2.Examples.FirstPerson;
using Needle.Haystack;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Needle.UI
{
    [DisallowMultipleComponent]
    public sealed class MainMenuController : MonoBehaviour
    {
        const int TimedPresetIndex = 2;
        const string TimeModeLocalizationTable = "Game";
        const string TimeModeLocalizationKey = "Time Mode";

        [Header("Menu")]
        [SerializeField] GameObject _menuRoot;
        [SerializeField] Button _settingsButton;
        [SerializeField] Button _leaderboardButton;
        [SerializeField] Button _easyButton;
        [SerializeField] Button _mediumButton;
        [SerializeField] Button _hardButton;
        [SerializeField] Button _expertButton;
        [SerializeField] Button _timeButton;
        [SerializeField] GameObject _duelButton;

        [Header("HUD")]
        [SerializeField] Button _homeButton;
        [SerializeField] GameObject _timerHud;
        [SerializeField] TextMeshProUGUI _timerHudText;
        [SerializeField] InterstitialAdController _interstitialAds;
        [SerializeField] RewardedAdController _rewardedAds;

        [Header("Panels")]
        [SerializeField] SettingsPanel _settingsPanel;
        [SerializeField] LeaderboardPanel _leaderboardPanel;
        [SerializeField] VictoryPanel _victoryPanel;
        [SerializeField] UiConfetti _confetti;
        [SerializeField, Min(0f)] float _victoryDelay = 2f;

        [Header("Gameplay")]
        [SerializeField] HaystackSystem _haystack;
        [SerializeField] Character _character;
        [SerializeField] HaystackGrabInput _grabInput;
        [SerializeField] FirstPersonCharacterInput _moveInput;
        [SerializeField] FirstPersonCharacterLookInput _lookInput;
        [SerializeField] JoystickMoveInput _joystickMove;
        [SerializeField] TouchLookInput _touchLook;
        [SerializeField] HaystackMobileControls _mobileControls;
        [SerializeField, Tooltip("Editor only. Playgama reports Desktop in the Editor, so tick this to preview mobile controls.")]
        bool _forceTouchControls;

        readonly AudioSettingsService _audioSettings = new AudioSettingsService();
        readonly PlaygamaDeviceService _device = new PlaygamaDeviceService();
        readonly LocaleSettingsService _localeSettings = new LocaleSettingsService();
        readonly PlaygamaPlatformLifecycle _platformLifecycle = new PlaygamaPlatformLifecycle();
        readonly TimeAttackClock _clock = new TimeAttackClock();
        readonly TimeAttackRecordService _records = new TimeAttackRecordService();
        readonly LeaderboardEntry[] _emptyLeaderboard = System.Array.Empty<LeaderboardEntry>();
        readonly List<RaycastResult> _uiRaycastHits = new List<RaycastResult>(8);
        PointerEventData _pointerEventData;
        LocalizedStringWatcher _timeModeName;
        bool _isValid;
        bool _listenersBound;
        bool _isGameplaySession;
        bool _isPaused;
        bool _isAdPaused;
        bool _isPlatformPaused;
        bool _isVictorySequence;
        bool _isTimedSession;
        Coroutine _victoryRoutine;
        Coroutine _gameReadyRoutine;
        bool _usesTouchControls;

        bool UsesTouchControls => _usesTouchControls;

        bool IsGameplayFrozen => _isPaused || _isAdPaused || _isPlatformPaused || _isVictorySequence;

        void Awake()
        {
            _usesTouchControls = ResolveTouchControls();
            Validate();
            _timeModeName = new LocalizedStringWatcher(
                TimeModeLocalizationTable,
                TimeModeLocalizationKey,
                TimeModeLocalizationKey);
            _audioSettings.LoadAndApply();
            _localeSettings.Ready += OnLocaleReady;
            _localeSettings.LoadAndApply();
            DisableUnimplementedModes();
            ConstructPanels();
            ReturnToMenu();
        }

        void Start()
        {
            if (!_isGameplaySession)
                ReturnToMenu();
        }

        void OnDestroy()
        {
            _localeSettings.Ready -= OnLocaleReady;
            if (_gameReadyRoutine != null)
            {
                StopCoroutine(_gameReadyRoutine);
                _gameReadyRoutine = null;
            }

            _timeModeName?.Dispose();
            _timeModeName = null;
        }

        void Update()
        {
            UpdateGameplayCursorAndLook();

            if (_interstitialAds != null)
                _interstitialAds.Tick(Time.unscaledDeltaTime);

            if (!_isTimedSession || !_isGameplaySession || IsGameplayFrozen)
                return;

            _clock.Tick(Time.unscaledDeltaTime);
            RefreshTimerHud();
        }

        void OnEnable()
        {
            BindListeners();
            if (_haystack != null)
                _haystack.NeedleFound += OnNeedleFound;
        }

        void OnDisable()
        {
            UnbindListeners();
            if (_haystack != null)
                _haystack.NeedleFound -= OnNeedleFound;
        }

        void Validate()
        {
            _isValid = true;
            _isValid &= RequiredRefs.Assign(_menuRoot, nameof(_menuRoot), this);
            _isValid &= RequiredRefs.Assign(_settingsButton, nameof(_settingsButton), this);
            _isValid &= RequiredRefs.Assign(_leaderboardButton, nameof(_leaderboardButton), this);
            _isValid &= RequiredRefs.Assign(_easyButton, nameof(_easyButton), this);
            _isValid &= RequiredRefs.Assign(_mediumButton, nameof(_mediumButton), this);
            _isValid &= RequiredRefs.Assign(_hardButton, nameof(_hardButton), this);
            _isValid &= RequiredRefs.Assign(_expertButton, nameof(_expertButton), this);
            _isValid &= RequiredRefs.Assign(_timeButton, nameof(_timeButton), this);
            _isValid &= RequiredRefs.Assign(_duelButton, nameof(_duelButton), this);
            _isValid &= RequiredRefs.Assign(_homeButton, nameof(_homeButton), this);
            _isValid &= RequiredRefs.Assign(_timerHud, nameof(_timerHud), this);
            _isValid &= RequiredRefs.Assign(_timerHudText, nameof(_timerHudText), this);
            _isValid &= RequiredRefs.Assign(_interstitialAds, nameof(_interstitialAds), this);
            _isValid &= RequiredRefs.Assign(_rewardedAds, nameof(_rewardedAds), this);
            _isValid &= RequiredRefs.Assign(_settingsPanel, nameof(_settingsPanel), this);
            _isValid &= RequiredRefs.Assign(_leaderboardPanel, nameof(_leaderboardPanel), this);
            _isValid &= RequiredRefs.Assign(_victoryPanel, nameof(_victoryPanel), this);
            _isValid &= RequiredRefs.Assign(_confetti, nameof(_confetti), this);
            _isValid &= RequiredRefs.Assign(_haystack, nameof(_haystack), this);
            _isValid &= RequiredRefs.Assign(_character, nameof(_character), this);
            _isValid &= RequiredRefs.Assign(_grabInput, nameof(_grabInput), this);
            _isValid &= RequiredRefs.Assign(_moveInput, nameof(_moveInput), this);
            _isValid &= RequiredRefs.Assign(_lookInput, nameof(_lookInput), this);
            _isValid &= RequiredRefs.Assign(_joystickMove, nameof(_joystickMove), this);
            _isValid &= RequiredRefs.Assign(_touchLook, nameof(_touchLook), this);
            _isValid &= RequiredRefs.Assign(_mobileControls, nameof(_mobileControls), this);
        }

        void ConstructPanels()
        {
            if (_settingsPanel != null)
                _settingsPanel.Construct(_audioSettings, _localeSettings);
        }

        void DisableUnimplementedModes()
        {
            if (_duelButton != null)
                _duelButton.SetActive(false);
        }

        void BindListeners()
        {
            if (_listenersBound)
                return;

            BindClick(_settingsButton, OpenSettings);
            BindClick(_leaderboardButton, OpenLeaderboard);
            BindClick(_easyButton, StartEasy);
            BindClick(_mediumButton, StartMedium);
            BindClick(_hardButton, StartHard);
            BindClick(_expertButton, StartExpert);
            BindClick(_timeButton, StartTimed);
            BindClick(_homeButton, OpenPauseSettings);
            if (_settingsPanel != null)
            {
                _settingsPanel.Closed += OnSettingsClosed;
                _settingsPanel.ExitRequested += OnSettingsExitRequested;
            }

            if (_victoryPanel != null)
                _victoryPanel.ExitRequested += OnVictoryExitRequested;

            if (_interstitialAds != null)
            {
                _interstitialAds.PauseRequested += OnAdPauseRequested;
                _interstitialAds.ResumeRequested += OnAdResumeRequested;
            }

            if (_rewardedAds != null)
            {
                _rewardedAds.PauseRequested += OnAdPauseRequested;
                _rewardedAds.ResumeRequested += OnAdResumeRequested;
            }

            BindPlatformAudio();
            BindPlatformPause();
            _listenersBound = true;
        }

        void BindClick(Button button, UnityEngine.Events.UnityAction handler)
        {
            if (button == null)
                return;

            button.onClick.AddListener(handler);
        }

        void UnbindListeners()
        {
            if (!_listenersBound)
                return;

            if (_settingsButton != null)
                _settingsButton.onClick.RemoveListener(OpenSettings);
            if (_leaderboardButton != null)
                _leaderboardButton.onClick.RemoveListener(OpenLeaderboard);
            if (_easyButton != null)
                _easyButton.onClick.RemoveListener(StartEasy);
            if (_mediumButton != null)
                _mediumButton.onClick.RemoveListener(StartMedium);
            if (_hardButton != null)
                _hardButton.onClick.RemoveListener(StartHard);
            if (_expertButton != null)
                _expertButton.onClick.RemoveListener(StartExpert);
            if (_timeButton != null)
                _timeButton.onClick.RemoveListener(StartTimed);
            if (_homeButton != null)
                _homeButton.onClick.RemoveListener(OpenPauseSettings);
            if (_settingsPanel != null)
            {
                _settingsPanel.Closed -= OnSettingsClosed;
                _settingsPanel.ExitRequested -= OnSettingsExitRequested;
            }

            if (_victoryPanel != null)
                _victoryPanel.ExitRequested -= OnVictoryExitRequested;

            if (_interstitialAds != null)
            {
                _interstitialAds.PauseRequested -= OnAdPauseRequested;
                _interstitialAds.ResumeRequested -= OnAdResumeRequested;
            }

            if (_rewardedAds != null)
            {
                _rewardedAds.PauseRequested -= OnAdPauseRequested;
                _rewardedAds.ResumeRequested -= OnAdResumeRequested;
            }

            UnbindPlatformAudio();
            UnbindPlatformPause();
            _listenersBound = false;
        }

        void StartEasy()
        {
            StartGame(0);
        }

        void StartMedium()
        {
            StartGame(1);
        }

        void StartHard()
        {
            StartGame(2);
        }

        void StartExpert()
        {
            StartGame(3);
        }

        void StartTimed()
        {
            StartGame(TimedPresetIndex, true);
        }

        void OpenSettings()
        {
            if (!RequiredRefs.Assign(_settingsPanel, nameof(_settingsPanel), this))
                return;

            _settingsPanel.Open(false);
        }

        void OpenPauseSettings()
        {
            if (!_isGameplaySession || IsGameplayFrozen)
                return;

            if (!RequiredRefs.Assign(_settingsPanel, nameof(_settingsPanel), this))
                return;

            _isPaused = true;
            FreezeGameplayForInterrupt();
            _platformLifecycle.NotifyLevelPaused();
            _settingsPanel.Open(true);
        }

        void OpenLeaderboard()
        {
            if (!RequiredRefs.Assign(_leaderboardPanel, nameof(_leaderboardPanel), this))
                return;

            _leaderboardPanel.Open();
            _leaderboardPanel.Initialize(BuildLeaderboardEntries(), BuildPlayerLeaderboardEntry());
        }

        void StartGame(int presetIndex, bool timed = false)
        {
            if (!RequiredRefs.CanAct(_isValid, "StartGame", this))
                return;

            StopVictoryRoutine();
            _isVictorySequence = false;
            if (_confetti != null)
                _confetti.Stop();
            _haystack.SelectPreset(presetIndex);
            _haystack.Rebuild();
            CloseOverlays();
            _isGameplaySession = true;
            _isPaused = false;
            _isAdPaused = false;
            _isTimedSession = timed;
            _menuRoot.SetActive(false);
            SetGameplayEnabled(true);
            SetHomeButtonVisible(true);
            BeginTimedClock(timed);
            if (_interstitialAds != null)
                _interstitialAds.StartSession();
            if (_rewardedAds != null)
                _rewardedAds.StartSession();
            _platformLifecycle.NotifyLevelStarted();
        }

        void BeginTimedClock(bool timed)
        {
            if (!timed)
            {
                _clock.Reset();
                SetTimerHudVisible(false);
                return;
            }

            _clock.Reset();
            _clock.Start();
            SetTimerHudVisible(true);
            RefreshTimerHud();
        }

        void OnNeedleFound(int _)
        {
            if (!_isGameplaySession || _isVictorySequence)
                return;

            if (_isTimedSession)
            {
                _clock.Stop();
                RefreshTimerHud();
                _records.TrySubmit(_clock.ElapsedSeconds);
                RefreshLeaderboardPlayer();
            }

            StopVictoryRoutine();
            if (_interstitialAds != null)
                _interstitialAds.StopSession();
            if (_rewardedAds != null)
                _rewardedAds.StopSession();
            _platformLifecycle.NotifyLevelCompleted();
            _victoryRoutine = StartCoroutine(PlayVictorySequence());
        }

        IEnumerator PlayVictorySequence()
        {
            _isVictorySequence = true;
            _isPaused = true;
            CloseOverlays();
            SetGameplayEnabled(false);
            SetHomeButtonVisible(false);

            if (_confetti != null)
                _confetti.Play();

            yield return new WaitForSecondsRealtime(_victoryDelay);

            if (!_isVictorySequence)
                yield break;

            if (!RequiredRefs.Assign(_victoryPanel, nameof(_victoryPanel), this))
                yield break;

            _victoryPanel.Open(_isTimedSession, _isTimedSession ? _clock.Format() : null);
        }

        void OnVictoryExitRequested()
        {
            ReturnToMenu();
        }

        void OnSettingsClosed()
        {
            if (!_isGameplaySession || _isVictorySequence)
                return;

            _isPaused = false;
            TryResumeGameplayAfterInterrupt();
        }

        void OnAdPauseRequested()
        {
            if (!_isGameplaySession || _isVictorySequence)
                return;

            _isAdPaused = true;
            _audioSettings.SetAdAudioSuppressed(true);
            FreezeGameplayForInterrupt();
            _platformLifecycle.NotifyLevelPaused();
        }

        void OnAdResumeRequested()
        {
            if (!_isGameplaySession || _isVictorySequence)
                return;

            _isAdPaused = false;
            _audioSettings.SetAdAudioSuppressed(false);
            TryResumeGameplayAfterInterrupt();
        }

        void OnSettingsExitRequested()
        {
            ReturnToMenu();
        }

        void ReturnToMenu()
        {
            StopVictoryRoutine();
            _platformLifecycle.NotifyLevelCompleted();
            _isGameplaySession = false;
            _isPaused = false;
            _isAdPaused = false;
            _isPlatformPaused = false;
            _audioSettings.SetAdAudioSuppressed(false);
            _isVictorySequence = false;
            _isTimedSession = false;
            _clock.Reset();
            CloseOverlays();
            if (_interstitialAds != null)
                _interstitialAds.StopSession();
            if (_rewardedAds != null)
                _rewardedAds.StopSession();
            if (_confetti != null)
                _confetti.Stop();
            if (_haystack != null)
                _haystack.ClearHeld();
            if (_menuRoot != null)
                _menuRoot.SetActive(true);
            SetGameplayEnabled(false);
            SetHomeButtonVisible(false);
            SetTimerHudVisible(false);
        }

        void CloseOverlays()
        {
            if (_settingsPanel != null)
                _settingsPanel.Close();
            if (_leaderboardPanel != null)
                _leaderboardPanel.Close();
            if (_victoryPanel != null)
                _victoryPanel.Close();
        }

        void StopVictoryRoutine()
        {
            if (_victoryRoutine == null)
                return;

            StopCoroutine(_victoryRoutine);
            _victoryRoutine = null;
        }

        void SetHomeButtonVisible(bool visible)
        {
            if (_homeButton != null)
                _homeButton.gameObject.SetActive(visible);
            if (_rewardedAds != null)
                _rewardedAds.SetVisible(visible);
        }

        void SetTimerHudVisible(bool visible)
        {
            if (_timerHud != null)
                _timerHud.SetActive(visible);
        }

        void RefreshTimerHud()
        {
            if (_timerHudText != null)
                _timerHudText.text = _clock.Format();
        }

        void RefreshLeaderboardPlayer()
        {
            if (_leaderboardPanel == null)
                return;

            _leaderboardPanel.SetPlayerEntry(BuildPlayerLeaderboardEntry());
        }

        LeaderboardEntry[] BuildLeaderboardEntries()
        {
            return _emptyLeaderboard;
        }

        LeaderboardEntry BuildPlayerLeaderboardEntry()
        {
            if (!_records.TryGetBest(out float best))
                return null;

            return new LeaderboardEntry(1, GetTimeModeDisplayName(), TimeAttackClock.Format(best), null);
        }

        string GetTimeModeDisplayName()
        {
            if (_timeModeName == null || string.IsNullOrEmpty(_timeModeName.Value))
                return TimeModeLocalizationKey;

            return _timeModeName.Value;
        }

        void FreezeGameplayForInterrupt()
        {
            if (_interstitialAds != null)
                _interstitialAds.SetFrozen(true);
            SetGameplayEnabled(false);
            SetHomeButtonVisible(false);
        }

        void TryResumeGameplayAfterInterrupt()
        {
            if (!_isGameplaySession || _isVictorySequence)
                return;

            if (_isPaused || _isAdPaused || _isPlatformPaused)
                return;

            if (_interstitialAds != null)
                _interstitialAds.SetFrozen(false);

            SetGameplayEnabled(true);
            SetHomeButtonVisible(true);
            _platformLifecycle.NotifyLevelResumed();
        }

        void OnLocaleReady()
        {
            if (_gameReadyRoutine != null)
                StopCoroutine(_gameReadyRoutine);

            _gameReadyRoutine = StartCoroutine(NotifyGameReadyNextFrame());
        }

        IEnumerator NotifyGameReadyNextFrame()
        {
            yield return null;
            _platformLifecycle.NotifyGameReady();
            _gameReadyRoutine = null;
        }

        void SetGameplayEnabled(bool enabled)
        {
            if (_character != null)
                _character.Pause(!enabled);
            if (_grabInput != null)
            {
                _grabInput.enabled = enabled;
                _grabInput.UseMouseGrab = !UsesTouchControls;
            }
            if (_moveInput != null)
                _moveInput.enabled = enabled && !UsesTouchControls;
            if (_joystickMove != null)
                _joystickMove.enabled = enabled && UsesTouchControls;
            if (_lookInput != null)
                _lookInput.enabled = false;
            if (_touchLook != null)
                _touchLook.enabled = enabled && UsesTouchControls;
            if (_mobileControls != null)
                _mobileControls.SetGameplayActive(enabled && UsesTouchControls);

            if (!enabled)
                ApplyCursorLock(false);
        }

        void UpdateGameplayCursorAndLook()
        {
            bool pointerOverUi = IsPointerOverUi();
            bool clicked = Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1);
            bool locked = GameplayCursorPolicy.ShouldLock(
                _isGameplaySession,
                IsGameplayFrozen,
                _isVictorySequence,
                Cursor.lockState == CursorLockMode.Locked,
                pointerOverUi,
                clicked,
                UsesTouchControls);

            ApplyCursorLock(locked);

            if (!_isGameplaySession || IsGameplayFrozen)
            {
                if (_lookInput != null)
                    _lookInput.enabled = false;
                if (_touchLook != null)
                    _touchLook.enabled = false;
                return;
            }

            if (UsesTouchControls)
            {
                if (_lookInput != null)
                    _lookInput.enabled = false;
                if (_touchLook != null)
                    _touchLook.enabled = true;
                return;
            }

            if (_touchLook != null)
                _touchLook.enabled = false;
            if (_lookInput != null)
                _lookInput.enabled = locked;
        }

        bool ResolveTouchControls()
        {
#if UNITY_EDITOR
            if (_forceTouchControls || Application.isMobilePlatform)
                return true;
#endif
            return _device.UsesTouchControls;
        }

        bool IsPointerOverUi()
        {
            if (IsPointerOverHome())
                return true;

            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
                return false;

            if (_pointerEventData == null)
                _pointerEventData = new PointerEventData(eventSystem);
            else
                _pointerEventData.Reset();

            _pointerEventData.position = Input.mousePosition;
            _uiRaycastHits.Clear();
            eventSystem.RaycastAll(_pointerEventData, _uiRaycastHits);
            return _uiRaycastHits.Count > 0;
        }

        bool IsPointerOverHome()
        {
            if (_homeButton == null || !_homeButton.isActiveAndEnabled)
                return false;

            RectTransform rect = _homeButton.transform as RectTransform;
            return rect != null &&
                   RectTransformUtility.RectangleContainsScreenPoint(rect, Input.mousePosition, null);
        }

        static void ApplyCursorLock(bool locked)
        {
            CursorLockMode mode = locked ? CursorLockMode.Locked : CursorLockMode.None;
            if (Cursor.lockState != mode)
                Cursor.lockState = mode;

            bool visible = !locked;
            if (Cursor.visible != visible)
                Cursor.visible = visible;
        }

        void BindPlatformAudio()
        {
#if UNITY_WEBGL
            if (Playgama.Bridge.platform == null)
                return;

            _audioSettings.SetPlatformAudioEnabled(Playgama.Bridge.platform.isAudioEnabled);
            Playgama.Bridge.platform.audioStateChanged += OnPlatformAudioChanged;
#endif
        }

        void UnbindPlatformAudio()
        {
#if UNITY_WEBGL
            if (Playgama.Bridge.platform == null)
                return;

            Playgama.Bridge.platform.audioStateChanged -= OnPlatformAudioChanged;
#endif
        }

        void BindPlatformPause()
        {
#if UNITY_WEBGL
            if (Playgama.Bridge.platform == null)
                return;

            Playgama.Bridge.platform.pauseStateChanged += OnPlatformPauseChanged;
#endif
        }

        void UnbindPlatformPause()
        {
#if UNITY_WEBGL
            if (Playgama.Bridge.platform == null)
                return;

            Playgama.Bridge.platform.pauseStateChanged -= OnPlatformPauseChanged;
#endif
        }

#if UNITY_WEBGL
        void OnPlatformAudioChanged(bool enabled)
        {
            _audioSettings.SetPlatformAudioEnabled(enabled);
        }

        void OnPlatformPauseChanged(bool paused)
        {
            if (paused)
                OnPlatformPauseRequested();
            else
                OnPlatformResumeRequested();
        }
#endif

        void OnApplicationFocus(bool hasFocus)
        {
            _audioSettings.SetHasFocus(hasFocus);
        }

        void OnApplicationPause(bool paused)
        {
            if (paused)
                _audioSettings.SetHasFocus(false);
        }

        void OnPlatformPauseRequested()
        {
            _isPlatformPaused = true;
            _audioSettings.SetPlatformPaused(true);
            if (!_isGameplaySession || _isVictorySequence)
                return;

            FreezeGameplayForInterrupt();
            _platformLifecycle.NotifyLevelPaused();
        }

        void OnPlatformResumeRequested()
        {
            _isPlatformPaused = false;
            _audioSettings.SetPlatformPaused(false);
            TryResumeGameplayAfterInterrupt();
        }
    }
}
