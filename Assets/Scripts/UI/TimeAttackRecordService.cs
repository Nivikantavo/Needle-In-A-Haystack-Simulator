using UnityEngine;

namespace Needle.UI
{
    public interface ITimeAttackStore
    {
        bool TryGetBest(out float seconds);
        void SetBest(float seconds);
    }

    public sealed class PlayerPrefsTimeAttackStore : ITimeAttackStore
    {
        public const string DefaultKey = "timeattack.bestSeconds";

        readonly string _key;

        public PlayerPrefsTimeAttackStore(string key = DefaultKey)
        {
            _key = string.IsNullOrEmpty(key) ? DefaultKey : key;
        }

        public bool TryGetBest(out float seconds)
        {
            if (!PlayerPrefs.HasKey(_key))
            {
                seconds = 0f;
                return false;
            }

            seconds = PlayerPrefs.GetFloat(_key);
            return true;
        }

        public void SetBest(float seconds)
        {
            PlayerPrefs.SetFloat(_key, seconds);
            PlayerPrefs.Save();
        }
    }

    public sealed class TimeAttackRecordService
    {
        readonly ITimeAttackStore _store;

        public TimeAttackRecordService(ITimeAttackStore store = null)
        {
            _store = store ?? new PlayerPrefsTimeAttackStore();
        }

        public bool TryGetBest(out float seconds)
        {
            return _store.TryGetBest(out seconds);
        }

        public bool TrySubmit(float seconds)
        {
            if (seconds < 0f)
                return false;

            if (_store.TryGetBest(out float best) && seconds >= best)
                return false;

            _store.SetBest(seconds);
            return true;
        }
    }
}
