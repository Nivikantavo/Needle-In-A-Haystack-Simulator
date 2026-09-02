using System;

namespace Needle.UI
{
    public static class LocaleCodeResolver
    {
        public const string FallbackCode = "en";
        public const string CisFallbackCode = "ru";

        static readonly string[] CisFallbackLanguages = { "be", "kk", "uk", "uz" };

        public static string ResolveInitial(string savedCode, string platformCode, string[] availableCodes)
        {
            if (TryMatch(savedCode, availableCodes, out string savedMatch))
                return savedMatch;

            return Resolve(platformCode, availableCodes);
        }

        public static string Resolve(string requestedCode, string[] availableCodes)
        {
            if (TryMatch(requestedCode, availableCodes, out string match))
                return match;

            if (TryCisRussianFallback(requestedCode, availableCodes, out string russian))
                return russian;

            if (TryMatch(FallbackCode, availableCodes, out string fallback))
                return fallback;

            if (availableCodes != null)
            {
                for (int i = 0; i < availableCodes.Length; i++)
                {
                    if (!string.IsNullOrWhiteSpace(availableCodes[i]))
                        return availableCodes[i];
                }
            }

            return FallbackCode;
        }

        public static bool TryMatch(string requestedCode, string[] availableCodes, out string matched)
        {
            matched = null;
            if (string.IsNullOrWhiteSpace(requestedCode) || availableCodes == null)
                return false;

            string requested = requestedCode.Trim();
            if (TryExact(requested, availableCodes, out matched))
                return true;

            string language = LanguagePart(requested);
            if (language.Length == requested.Length)
                return false;

            return TryExact(language, availableCodes, out matched);
        }

        static bool TryCisRussianFallback(string requestedCode, string[] availableCodes, out string matched)
        {
            matched = null;
            if (string.IsNullOrWhiteSpace(requestedCode))
                return false;

            string language = LanguagePart(requestedCode.Trim());
            if (!IsCisFallbackLanguage(language))
                return false;

            return TryMatch(CisFallbackCode, availableCodes, out matched);
        }

        static bool IsCisFallbackLanguage(string language)
        {
            for (int i = 0; i < CisFallbackLanguages.Length; i++)
            {
                if (string.Equals(language, CisFallbackLanguages[i], StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        static bool TryExact(string requested, string[] availableCodes, out string matched)
        {
            for (int i = 0; i < availableCodes.Length; i++)
            {
                string available = availableCodes[i];
                if (string.IsNullOrWhiteSpace(available))
                    continue;

                if (!string.Equals(available.Trim(), requested, StringComparison.OrdinalIgnoreCase))
                    continue;

                matched = available;
                return true;
            }

            matched = null;
            return false;
        }

        static string LanguagePart(string code)
        {
            for (int i = 0; i < code.Length; i++)
            {
                char c = code[i];
                if (c == '-' || c == '_')
                    return code.Substring(0, i);
            }

            return code;
        }
    }
}
