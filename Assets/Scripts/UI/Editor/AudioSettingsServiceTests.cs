using NUnit.Framework;
using UnityEngine;

namespace Needle.UI.Editor.Tests
{
    public class AudioSettingsServiceTests
    {
        const string MutedKey = "audio.muted";
        const string VolumeKey = "audio.volume";
        const string LastVolumeKey = "audio.lastVolume";

        int _savedMuted;
        bool _hadMuted;
        float _savedVolume;
        bool _hadVolume;
        float _savedLastVolume;
        bool _hadLastVolume;
        float _savedListenerVolume;

        [SetUp]
        public void SetUp()
        {
            _hadMuted = PlayerPrefs.HasKey(MutedKey);
            _savedMuted = PlayerPrefs.GetInt(MutedKey, 0);
            _hadVolume = PlayerPrefs.HasKey(VolumeKey);
            _savedVolume = PlayerPrefs.GetFloat(VolumeKey, 1f);
            _hadLastVolume = PlayerPrefs.HasKey(LastVolumeKey);
            _savedLastVolume = PlayerPrefs.GetFloat(LastVolumeKey, 1f);
            _savedListenerVolume = AudioListener.volume;

            PlayerPrefs.DeleteKey(MutedKey);
            PlayerPrefs.DeleteKey(VolumeKey);
            PlayerPrefs.DeleteKey(LastVolumeKey);
        }

        [TearDown]
        public void TearDown()
        {
            Restore(MutedKey, _hadMuted, () => PlayerPrefs.SetInt(MutedKey, _savedMuted));
            Restore(VolumeKey, _hadVolume, () => PlayerPrefs.SetFloat(VolumeKey, _savedVolume));
            Restore(LastVolumeKey, _hadLastVolume, () => PlayerPrefs.SetFloat(LastVolumeKey, _savedLastVolume));
            PlayerPrefs.Save();
            AudioListener.volume = _savedListenerVolume;
        }

        [Test]
        public void SetVolume_AppliesToEffectiveVolume()
        {
            var audio = new AudioSettingsService();
            audio.SetVolume(0.4f);

            Assert.AreEqual(0.4f, audio.Volume);
            Assert.AreEqual(0.4f, audio.EffectiveVolume);
            Assert.IsFalse(audio.IsMuted);
        }

        [Test]
        public void SetMuted_True_ZerosVolume()
        {
            var audio = new AudioSettingsService();
            audio.SetVolume(0.8f);

            audio.SetMuted(true);

            Assert.AreEqual(0f, audio.Volume);
            Assert.AreEqual(0f, audio.EffectiveVolume);
            Assert.IsTrue(audio.IsMuted);
        }

        [Test]
        public void SetMuted_False_RestoresLastVolume()
        {
            var audio = new AudioSettingsService();
            audio.SetVolume(0.6f);
            audio.SetMuted(true);

            audio.SetMuted(false);

            Assert.AreEqual(0.6f, audio.Volume);
            Assert.AreEqual(0.6f, audio.EffectiveVolume);
            Assert.IsFalse(audio.IsMuted);
        }

        [Test]
        public void SetVolume_FromZero_Unmutes()
        {
            var audio = new AudioSettingsService();
            audio.SetVolume(0.5f);
            audio.SetMuted(true);

            audio.SetVolume(0.3f);

            Assert.IsFalse(audio.IsMuted);
            Assert.AreEqual(0.3f, audio.EffectiveVolume);
        }

        [Test]
        public void LoadAndApply_LegacyMutedKey_ZerosVolume()
        {
            PlayerPrefs.SetFloat(VolumeKey, 0.9f);
            PlayerPrefs.SetInt(MutedKey, 1);
            PlayerPrefs.Save();

            var audio = new AudioSettingsService();
            audio.LoadAndApply();

            Assert.AreEqual(0f, audio.Volume);
            Assert.IsTrue(audio.IsMuted);
            Assert.AreEqual(0f, audio.EffectiveVolume);
        }

        [Test]
        public void AdSuppress_ZerosEffectiveVolume_KeepsUserVolume()
        {
            var audio = new AudioSettingsService();
            audio.SetVolume(0.7f);

            audio.SetAdAudioSuppressed(true);

            Assert.AreEqual(0.7f, audio.Volume);
            Assert.AreEqual(0f, audio.EffectiveVolume);

            audio.SetAdAudioSuppressed(false);

            Assert.AreEqual(0.7f, audio.EffectiveVolume);
        }

        [Test]
        public void PlatformAudioDisabled_ZerosEffectiveVolume_KeepsUserVolume()
        {
            var audio = new AudioSettingsService();
            audio.SetVolume(1f);

            audio.SetPlatformAudioEnabled(false);

            Assert.AreEqual(1f, audio.Volume);
            Assert.AreEqual(0f, audio.EffectiveVolume);

            audio.SetPlatformAudioEnabled(true);

            Assert.AreEqual(1f, audio.EffectiveVolume);
        }

        [Test]
        public void PlatformPause_ZerosEffectiveVolume_KeepsUserVolume()
        {
            var audio = new AudioSettingsService();
            audio.SetVolume(0.5f);

            audio.SetPlatformPaused(true);

            Assert.AreEqual(0.5f, audio.Volume);
            Assert.AreEqual(0f, audio.EffectiveVolume);

            audio.SetPlatformPaused(false);

            Assert.AreEqual(0.5f, audio.EffectiveVolume);
        }

        [Test]
        public void LostFocus_ZerosEffectiveVolume_KeepsUserVolume()
        {
            var audio = new AudioSettingsService();
            audio.SetVolume(0.2f);

            audio.SetHasFocus(false);

            Assert.AreEqual(0.2f, audio.Volume);
            Assert.AreEqual(0f, audio.EffectiveVolume);

            audio.SetHasFocus(true);

            Assert.AreEqual(0.2f, audio.EffectiveVolume);
        }

        static void Restore(string key, bool hadValue, System.Action write)
        {
            if (hadValue)
                write();
            else
                PlayerPrefs.DeleteKey(key);
        }
    }
}
