using UnityEngine;

namespace Needle.UI
{
    public sealed class AudioSettingsService
    {
        const string MutedKey = "audio.muted";
        const string VolumeKey = "audio.volume";
        const string LastVolumeKey = "audio.lastVolume";
        const float DefaultVolume = 1f;

        float _lastVolume = DefaultVolume;

        public bool IsMuted { get; private set; }
        public float Volume { get; private set; }
        public bool IsPlatformAudioEnabled { get; private set; } = true;
        public bool IsAdAudioSuppressed { get; private set; }
        public bool IsPlatformPaused { get; private set; }
        public bool HasFocus { get; private set; } = true;

        public float EffectiveVolume =>
            !IsPlatformAudioEnabled || IsMuted || IsAdAudioSuppressed || IsPlatformPaused || !HasFocus
                ? 0f
                : Volume;

        public void LoadAndApply()
        {
            Volume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, DefaultVolume));
            _lastVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(LastVolumeKey, Volume > 0f ? Volume : DefaultVolume));
            if (PlayerPrefs.GetInt(MutedKey, 0) != 0)
                Volume = 0f;

            if (Volume > 0f)
                _lastVolume = Volume;

            IsMuted = Volume <= 0f;
            ApplyListener();
        }

        public void SetMuted(bool muted)
        {
            if (muted)
            {
                if (Volume > 0f)
                    _lastVolume = Volume;
                Volume = 0f;
            }
            else if (Volume <= 0f)
            {
                Volume = _lastVolume > 0f ? _lastVolume : DefaultVolume;
            }

            IsMuted = Volume <= 0f;
            Persist();
            ApplyListener();
        }

        public void ToggleMuted()
        {
            SetMuted(!IsMuted);
        }

        public void SetVolume(float volume)
        {
            Volume = Mathf.Clamp01(volume);
            if (Volume > 0f)
                _lastVolume = Volume;

            IsMuted = Volume <= 0f;
            Persist();
            ApplyListener();
        }

        public void SetPlatformAudioEnabled(bool enabled)
        {
            IsPlatformAudioEnabled = enabled;
            ApplyListener();
        }

        public void SetAdAudioSuppressed(bool suppressed)
        {
            IsAdAudioSuppressed = suppressed;
            ApplyListener();
        }

        public void SetPlatformPaused(bool paused)
        {
            IsPlatformPaused = paused;
            ApplyListener();
        }

        public void SetHasFocus(bool hasFocus)
        {
            HasFocus = hasFocus;
            ApplyListener();
        }

        void Persist()
        {
            PlayerPrefs.SetInt(MutedKey, IsMuted ? 1 : 0);
            PlayerPrefs.SetFloat(VolumeKey, Volume);
            PlayerPrefs.SetFloat(LastVolumeKey, _lastVolume);
            PlayerPrefs.Save();
        }

        void ApplyListener()
        {
            AudioListener.volume = EffectiveVolume;
        }
    }
}
