using System;
using System.Collections.Generic;

namespace Needle.UI
{
    public sealed class PlaygamaPlayerService
    {
        public bool IsAuthorizationSupported
        {
            get
            {
#if UNITY_WEBGL
                return Playgama.Bridge.player != null &&
                       Playgama.Bridge.player.isAuthorizationSupported;
#else
                return false;
#endif
            }
        }

        public bool IsAuthorized
        {
            get
            {
#if UNITY_WEBGL
                return Playgama.Bridge.player != null &&
                       Playgama.Bridge.player.isAuthorized;
#else
                return false;
#endif
            }
        }

        public string Id
        {
            get
            {
#if UNITY_WEBGL
                return Playgama.Bridge.player != null ? Playgama.Bridge.player.id : null;
#else
                return null;
#endif
            }
        }

        public string Name
        {
            get
            {
#if UNITY_WEBGL
                return Playgama.Bridge.player != null ? Playgama.Bridge.player.name : null;
#else
                return null;
#endif
            }
        }

        public string Photo
        {
            get
            {
#if UNITY_WEBGL
                if (Playgama.Bridge.player == null)
                    return null;

                List<string> photos = Playgama.Bridge.player.photos;
                if (photos == null || photos.Count == 0)
                    return null;

                return photos[photos.Count - 1];
#else
                return null;
#endif
            }
        }

        public void Authorize(Action<bool> onComplete)
        {
            if (!IsAuthorizationSupported)
            {
                onComplete?.Invoke(false);
                return;
            }

            if (IsAuthorized)
            {
                onComplete?.Invoke(true);
                return;
            }

#if UNITY_WEBGL
            Playgama.Bridge.player.Authorize(new Dictionary<string, object>(), onComplete);
#else
            onComplete?.Invoke(false);
#endif
        }
    }
}
