namespace Needle.UI
{
    public static class GameplayCursorPolicy
    {
        public static bool ShouldLock(
            bool isGameplaySession,
            bool isPaused,
            bool isVictorySequence,
            bool currentlyLocked,
            bool pointerOverUi,
            bool clicked,
            bool useTouchControls = false)
        {
            if (useTouchControls)
                return false;

            if (!isGameplaySession || isPaused || isVictorySequence)
                return false;

            if (pointerOverUi)
                return false;

            if (currentlyLocked)
                return true;

            return clicked;
        }
    }
}
