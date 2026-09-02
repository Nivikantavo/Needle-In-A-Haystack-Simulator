namespace Needle.UI
{
    public sealed class TimeAttackClock
    {
        float _elapsed;
        bool _running;

        public float ElapsedSeconds => _elapsed;
        public bool IsRunning => _running;

        public void Reset()
        {
            _elapsed = 0f;
            _running = false;
        }

        public void Start()
        {
            _running = true;
        }

        public void Stop()
        {
            _running = false;
        }

        public void Tick(float unscaledDeltaTime)
        {
            if (!_running || unscaledDeltaTime <= 0f)
                return;

            _elapsed += unscaledDeltaTime;
        }

        public string Format()
        {
            return Format(_elapsed);
        }

        public static string Format(float seconds)
        {
            if (seconds < 0f)
                seconds = 0f;

            int totalCentiseconds = (int)(seconds * 100f);
            int minutes = totalCentiseconds / 6000;
            int remainder = totalCentiseconds % 6000;
            int secs = remainder / 100;
            int cents = remainder % 100;
            return string.Format("{0:00}:{1:00}.{2:00}", minutes, secs, cents);
        }
    }
}
