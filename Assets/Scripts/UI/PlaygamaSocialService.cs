using UnityEngine;

namespace Needle.UI
{
    public sealed class PlaygamaSocialService
    {
        public bool IsRateSupported
        {
            get
            {
#if UNITY_WEBGL
                return Playgama.Bridge.social != null && Playgama.Bridge.social.isRateSupported;
#else
                return false;
#endif
            }
        }

        public void Rate()
        {
            if (!IsRateSupported)
            {
                Debug.LogError("PlaygamaSocialService: Rate is not supported on this platform.");
                return;
            }

#if UNITY_WEBGL
            Playgama.Bridge.social.Rate();
#endif
        }
    }
}
