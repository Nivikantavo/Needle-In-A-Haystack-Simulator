using UnityEngine;

namespace Needle.UI
{
    public sealed class PlaygamaDeviceService
    {
        public string Type
        {
            get
            {
#if UNITY_WEBGL
                if (Playgama.Bridge.device == null)
                    return null;

                return Playgama.Bridge.device.type.ToString();
#else
                return Application.isMobilePlatform ? "mobile" : "desktop";
#endif
            }
        }

        public bool UsesTouchControls => DeviceInputPolicy.UsesTouchControls(Type);
    }
}
