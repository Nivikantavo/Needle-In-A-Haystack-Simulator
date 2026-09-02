using System;
#if UNITY_WEBGL
using Playgama.Modules.Advertisement;
#endif

namespace Needle.UI
{
    public enum InterstitialAdState
    {
        Loading,
        Opened,
        Closed,
        Failed
    }

    public enum RewardedAdState
    {
        Loading,
        Opened,
        Closed,
        Rewarded,
        Failed
    }

    public sealed class PlaygamaAdvertisementService
    {
        public event Action<InterstitialAdState> InterstitialStateChanged;
        public event Action<RewardedAdState> RewardedStateChanged;

        bool _subscribed;

        public bool IsSupported
        {
            get
            {
#if UNITY_WEBGL
                return Playgama.Bridge.advertisement != null &&
                       Playgama.Bridge.advertisement.isInterstitialSupported;
#else
                return false;
#endif
            }
        }

        public bool IsRewardedSupported
        {
            get
            {
#if UNITY_WEBGL
                return Playgama.Bridge.advertisement != null &&
                       Playgama.Bridge.advertisement.isRewardedSupported;
#else
                return false;
#endif
            }
        }

        public void Subscribe()
        {
            if (_subscribed)
                return;

#if UNITY_WEBGL
            if (Playgama.Bridge.advertisement != null)
            {
                Playgama.Bridge.advertisement.interstitialStateChanged += OnBridgeInterstitialState;
                Playgama.Bridge.advertisement.rewardedStateChanged += OnBridgeRewardedState;
            }
#endif
            _subscribed = true;
        }

        public void Unsubscribe()
        {
            if (!_subscribed)
                return;

#if UNITY_WEBGL
            if (Playgama.Bridge.advertisement != null)
            {
                Playgama.Bridge.advertisement.interstitialStateChanged -= OnBridgeInterstitialState;
                Playgama.Bridge.advertisement.rewardedStateChanged -= OnBridgeRewardedState;
            }
#endif
            _subscribed = false;
        }

        public void ShowInterstitial(string placement)
        {
            if (!IsSupported)
            {
                InterstitialStateChanged?.Invoke(InterstitialAdState.Failed);
                return;
            }

#if UNITY_WEBGL
            Playgama.Bridge.advertisement.ShowInterstitial(placement);
#endif
        }

        public void ShowRewarded(string placement)
        {
            if (!IsRewardedSupported)
            {
                RewardedStateChanged?.Invoke(RewardedAdState.Failed);
                return;
            }

#if UNITY_WEBGL
            Playgama.Bridge.advertisement.ShowRewarded(placement);
#endif
        }

#if UNITY_WEBGL
        void OnBridgeInterstitialState(InterstitialState state)
        {
            switch (state)
            {
                case InterstitialState.Loading:
                    InterstitialStateChanged?.Invoke(InterstitialAdState.Loading);
                    break;
                case InterstitialState.Opened:
                    InterstitialStateChanged?.Invoke(InterstitialAdState.Opened);
                    break;
                case InterstitialState.Closed:
                    InterstitialStateChanged?.Invoke(InterstitialAdState.Closed);
                    break;
                default:
                    InterstitialStateChanged?.Invoke(InterstitialAdState.Failed);
                    break;
            }
        }

        void OnBridgeRewardedState(RewardedState state)
        {
            switch (state)
            {
                case RewardedState.Loading:
                    RewardedStateChanged?.Invoke(RewardedAdState.Loading);
                    break;
                case RewardedState.Opened:
                    RewardedStateChanged?.Invoke(RewardedAdState.Opened);
                    break;
                case RewardedState.Rewarded:
                    RewardedStateChanged?.Invoke(RewardedAdState.Rewarded);
                    break;
                case RewardedState.Closed:
                    RewardedStateChanged?.Invoke(RewardedAdState.Closed);
                    break;
                default:
                    RewardedStateChanged?.Invoke(RewardedAdState.Failed);
                    break;
            }
        }
#endif
    }
}
