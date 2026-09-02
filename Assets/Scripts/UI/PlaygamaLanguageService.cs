namespace Needle.UI
{
    public sealed class PlaygamaLanguageService
    {
        public string Language
        {
            get
            {
#if UNITY_WEBGL
                if (Playgama.Bridge.platform == null)
                    return null;

                return Playgama.Bridge.platform.language;
#else
                return null;
#endif
            }
        }
    }
}
