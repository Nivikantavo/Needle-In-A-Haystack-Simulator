using System;
using UnityEngine.Localization;

namespace Needle.UI
{
    public sealed class LocalizedStringWatcher : IDisposable
    {
        readonly LocalizedString _localized;
        readonly string _fallback;
        Action _changed;
        bool _disposed;

        public LocalizedStringWatcher(string table, string key, string fallback, Action changed = null)
        {
            _fallback = fallback ?? string.Empty;
            Value = _fallback;
            _changed = changed;
            _localized = new LocalizedString(table, key);
            _localized.StringChanged += OnChanged;
        }

        public string Value { get; private set; }

        void OnChanged(string value)
        {
            Value = string.IsNullOrEmpty(value) ? _fallback : value;
            _changed?.Invoke();
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _localized.StringChanged -= OnChanged;
            _changed = null;
        }
    }
}
