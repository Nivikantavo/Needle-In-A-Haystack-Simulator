using System;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Needle.UI
{
    public sealed class LocaleSettingsService
    {
        const string PrefsKey = "locale.code";

        readonly PlaygamaLanguageService _platformLanguage;

        public LocaleSettingsService(PlaygamaLanguageService platformLanguage = null)
        {
            _platformLanguage = platformLanguage ?? new PlaygamaLanguageService();
        }

        public string CurrentCode { get; private set; }

        public event Action<string> Changed;

        public void LoadAndApply()
        {
            AsyncOperationHandle<LocalizationSettings> handle = LocalizationSettings.InitializationOperation;
            if (handle.IsDone)
            {
                ApplyDetectedOrSaved();
                return;
            }

            handle.Completed += _ => ApplyDetectedOrSaved();
        }

        public void Apply(string localeCode)
        {
            if (string.IsNullOrWhiteSpace(localeCode))
            {
                Debug.LogError("LocaleSettingsService: locale code is empty.");
                return;
            }

            Locale locale = LocalizationSettings.AvailableLocales.GetLocale(new LocaleIdentifier(localeCode));
            if (locale == null)
            {
                Debug.LogError($"LocaleSettingsService: locale '{localeCode}' is not in Available Locales.");
                return;
            }

            LocalizationSettings.SelectedLocale = locale;
            CurrentCode = locale.Identifier.Code;
            PlayerPrefs.SetString(PrefsKey, CurrentCode);
            PlayerPrefs.Save();
            Changed?.Invoke(CurrentCode);
        }

        void ApplyDetectedOrSaved()
        {
            string saved = PlayerPrefs.HasKey(PrefsKey) ? PlayerPrefs.GetString(PrefsKey) : null;
            string platform = _platformLanguage.Language;
            Apply(LocaleCodeResolver.ResolveInitial(saved, platform, GetAvailableCodes()));
        }

        static string[] GetAvailableCodes()
        {
            var locales = LocalizationSettings.AvailableLocales.Locales;
            if (locales == null || locales.Count == 0)
                return Array.Empty<string>();

            var codes = new string[locales.Count];
            for (int i = 0; i < locales.Count; i++)
            {
                Locale locale = locales[i];
                codes[i] = locale != null ? locale.Identifier.Code : string.Empty;
            }

            return codes;
        }
    }
}
