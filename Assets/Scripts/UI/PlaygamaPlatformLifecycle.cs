#if UNITY_WEBGL
using Playgama.Modules.Platform;
#endif

namespace Needle.UI
{
    public sealed class PlaygamaPlatformLifecycle
    {
        bool _gameReadySent;
        bool _levelActive;
        bool _gameplayRunning;

        public void NotifyGameReady()
        {
            if (_gameReadySent)
                return;

            _gameReadySent = true;
#if UNITY_WEBGL
            Send(PlatformMessage.GameReady);
#endif
        }

        public void NotifyLevelStarted()
        {
            _levelActive = true;
            _gameplayRunning = true;
#if UNITY_WEBGL
            Send(PlatformMessage.LevelStarted);
#endif
        }

        public void NotifyLevelPaused()
        {
            if (!_levelActive || !_gameplayRunning)
                return;

            _gameplayRunning = false;
#if UNITY_WEBGL
            Send(PlatformMessage.LevelPaused);
#endif
        }

        public void NotifyLevelResumed()
        {
            if (!_levelActive || _gameplayRunning)
                return;

            _gameplayRunning = true;
#if UNITY_WEBGL
            Send(PlatformMessage.LevelResumed);
#endif
        }

        public void NotifyLevelCompleted()
        {
            if (!_levelActive)
                return;

            _levelActive = false;
            _gameplayRunning = false;
#if UNITY_WEBGL
            Send(PlatformMessage.LevelCompleted);
#endif
        }

#if UNITY_WEBGL
        static void Send(PlatformMessage message)
        {
            if (Playgama.Bridge.platform == null)
                return;

            Playgama.Bridge.platform.SendMessage(message);
        }
#endif
    }
}
