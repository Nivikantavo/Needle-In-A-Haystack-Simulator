using System;

namespace Needle.UI
{
    public static class DeviceInputPolicy
    {
        public static bool UsesTouchControls(string deviceType)
        {
            if (string.IsNullOrWhiteSpace(deviceType))
                return false;

            return string.Equals(deviceType, "mobile", StringComparison.OrdinalIgnoreCase)
                || string.Equals(deviceType, "tablet", StringComparison.OrdinalIgnoreCase);
        }
    }
}
