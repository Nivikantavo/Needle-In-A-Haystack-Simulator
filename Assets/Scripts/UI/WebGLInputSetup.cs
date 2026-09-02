using UnityEngine;

namespace Needle.UI
{
    static class WebGLInputSetup
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Apply()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            WebGLInput.stickyCursorLock = false;
#endif
        }
    }
}
