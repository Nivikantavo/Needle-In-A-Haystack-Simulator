using System;

namespace Needle.Haystack
{
    [Flags]
    public enum StrawFlags : byte
    {
        None = 0,
        Buried = 1 << 0,
        Grabbed = 1 << 1,
        Discarded = 1 << 2,
        IsNeedle = 1 << 3
    }
}
