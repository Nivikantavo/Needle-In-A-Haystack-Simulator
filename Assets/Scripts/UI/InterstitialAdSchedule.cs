using System;

namespace Needle.UI
{
    public enum InterstitialAdPhase
    {
        Waiting,
        Countdown,
        Showing
    }

    public sealed class InterstitialAdSchedule
    {
        readonly float _intervalSeconds;
        readonly int _countdownSeconds;
        float _elapsed;
        int _countdownRemaining;
        bool _frozen;

        public InterstitialAdSchedule(float intervalSeconds = 60f, int countdownSeconds = 5)
        {
            if (intervalSeconds <= 0f)
                throw new ArgumentOutOfRangeException(nameof(intervalSeconds));
            if (countdownSeconds < 1)
                throw new ArgumentOutOfRangeException(nameof(countdownSeconds));

            _intervalSeconds = intervalSeconds;
            _countdownSeconds = countdownSeconds;
        }

        public InterstitialAdPhase Phase { get; private set; } = InterstitialAdPhase.Waiting;

        public int CountdownRemaining => _countdownRemaining;

        public event Action<int> CountdownChanged;

        public event Action ShowRequested;

        public void SetFrozen(bool frozen)
        {
            _frozen = frozen;
        }

        public void Reset()
        {
            Phase = InterstitialAdPhase.Waiting;
            _elapsed = 0f;
            _countdownRemaining = 0;
        }

        public void NotifyAdFinished()
        {
            Phase = InterstitialAdPhase.Waiting;
            _elapsed = 0f;
            _countdownRemaining = 0;
        }

        public void Tick(float unscaledDeltaTime)
        {
            if (_frozen || unscaledDeltaTime <= 0f)
                return;

            if (Phase == InterstitialAdPhase.Waiting)
                TickWaiting(unscaledDeltaTime);
            else if (Phase == InterstitialAdPhase.Countdown)
                TickCountdown(unscaledDeltaTime);
        }

        void TickWaiting(float unscaledDeltaTime)
        {
            _elapsed += unscaledDeltaTime;
            if (_elapsed < _intervalSeconds)
                return;

            _elapsed = 0f;
            Phase = InterstitialAdPhase.Countdown;
            _countdownRemaining = _countdownSeconds;
            CountdownChanged?.Invoke(_countdownRemaining);
        }

        void TickCountdown(float unscaledDeltaTime)
        {
            _elapsed += unscaledDeltaTime;
            while (Phase == InterstitialAdPhase.Countdown && _elapsed >= 1f)
            {
                _elapsed -= 1f;
                _countdownRemaining--;
                if (_countdownRemaining <= 0)
                {
                    Phase = InterstitialAdPhase.Showing;
                    ShowRequested?.Invoke();
                    return;
                }

                CountdownChanged?.Invoke(_countdownRemaining);
            }
        }
    }
}
