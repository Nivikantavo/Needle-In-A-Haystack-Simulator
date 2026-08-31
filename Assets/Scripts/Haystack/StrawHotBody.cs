using UnityEngine;

namespace Needle.Haystack
{
    [DisallowMultipleComponent]
    public sealed class StrawHotBody : MonoBehaviour
    {
        public HaystackSystem Owner;
        public int StrawIndex = -1;
        public StrawHotState State = StrawHotState.Free;
    }

    public enum StrawHotState : byte
    {
        Free = 0,
        Held = 1,
        Settling = 2
    }
}
