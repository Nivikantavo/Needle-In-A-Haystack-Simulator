using UnityEngine;

namespace Needle.UI
{
    public sealed class AudioSettingsService
    {
        const string MutedKey = "audio.muted";
        const string VolumeKey = "audio.volume";
        const float DefaultVolume = 1f;

        public bool IsMuted { get; private set; }
        public float Volume { get; private set; }
        public bool IsPlatformAudioEnabled { get; private set; } = true;

        public void LoadAndApply()
        {
            IsMuted = PlayerPrefs.GetInt(MutedKey, 0) != 0;
            Volume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, DefaultVolume));
            ApplyListener();
        }

        public void SetMuted(bool muted)
        {
            IsMuted = muted;
            PlayerPrefs.SetInt(MutedKey, muted ? 1 : 0);
            PlayerPrefs.Save();
            ApplyListener();
        }

        public void ToggleMuted()
        {
            SetMuted(!IsMuted);
        }

        public void SetVolume(float volume)
        {
            Volume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat(VolumeKey, Volume);
            PlayerPrefs.Save();
            ApplyListener();
        }

        public void SetPlatformAudioEnabled(bool enabled)
        {
            IsPlatformAudioEnabled = enabled;
            ApplyListener();
        }

        void ApplyListener()
        {
            AudioListener.volume = !IsPlatformAudioEnabled || IsMuted ? 0f : Volume;
        }
    }
}
