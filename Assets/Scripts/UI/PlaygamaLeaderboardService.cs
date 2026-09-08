using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_WEBGL
using Playgama.Modules.Leaderboards;
#endif

namespace Needle.UI
{
    public sealed class PlaygamaLeaderboardService
    {
        public const float MinSubmitIntervalSeconds = 1f;

        float _lastSubmitUnscaledTime = float.NegativeInfinity;
        int _lastSubmittedMilliseconds = -1;
        int _pendingMilliseconds = -1;
        string _pendingId;
        bool _submitInFlight;

        public bool CanSetScore
        {
            get
            {
#if UNITY_WEBGL
                if (Playgama.Bridge.leaderboards == null)
                    return false;

                LeaderboardType type = Playgama.Bridge.leaderboards.type;
                return type == LeaderboardType.InGame ||
                       type == LeaderboardType.Native ||
                       type == LeaderboardType.NativePopup;
#else
                return false;
#endif
            }
        }

        public bool IsInGame
        {
            get
            {
#if UNITY_WEBGL
                return Playgama.Bridge.leaderboards != null &&
                       Playgama.Bridge.leaderboards.type == LeaderboardType.InGame;
#else
                return false;
#endif
            }
        }

        public void Tick(float unscaledTime)
        {
            TryFlush(unscaledTime);
        }

        public void SetScore(string id, int milliseconds)
        {
            if (string.IsNullOrEmpty(id) || milliseconds < 0 || !CanSetScore)
                return;

            if (_lastSubmittedMilliseconds >= 0 && milliseconds >= _lastSubmittedMilliseconds)
                return;

            if (_pendingMilliseconds >= 0 && milliseconds >= _pendingMilliseconds)
                return;

            _pendingId = id;
            _pendingMilliseconds = milliseconds;
            TryFlush(Time.unscaledTime);
        }

        public void GetEntries(string id, Action<bool, List<Dictionary<string, string>>> onComplete)
        {
            if (!IsInGame || string.IsNullOrEmpty(id))
            {
                onComplete?.Invoke(false, null);
                return;
            }

#if UNITY_WEBGL
            Playgama.Bridge.leaderboards.GetEntries(id, onComplete);
#else
            onComplete?.Invoke(false, null);
#endif
        }

        void TryFlush(float unscaledTime)
        {
            if (_submitInFlight || _pendingMilliseconds < 0 || string.IsNullOrEmpty(_pendingId))
                return;

            if (_lastSubmittedMilliseconds >= 0 && _pendingMilliseconds >= _lastSubmittedMilliseconds)
            {
                _pendingMilliseconds = -1;
                return;
            }

            if (unscaledTime - _lastSubmitUnscaledTime < MinSubmitIntervalSeconds)
                return;

            string id = _pendingId;
            int milliseconds = _pendingMilliseconds;
            _pendingMilliseconds = -1;
            _submitInFlight = true;
            _lastSubmitUnscaledTime = unscaledTime;

#if UNITY_WEBGL
            Playgama.Bridge.leaderboards.SetScore(id, milliseconds, success =>
            {
                _submitInFlight = false;
                if (success)
                    _lastSubmittedMilliseconds = milliseconds;

                TryFlush(Time.unscaledTime);
            });
#else
            _submitInFlight = false;
#endif
        }
    }
}
