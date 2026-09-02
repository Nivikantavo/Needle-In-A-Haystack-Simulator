using UnityEngine;

namespace Needle.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class BackgroundMusicPlayer : MonoBehaviour
    {
        [SerializeField] AudioClip _clip;

        AudioSource _source;

        void Awake()
        {
            CacheSource();
            Configure();
            EnsurePlaying();
        }

        void OnEnable()
        {
            EnsurePlaying();
        }

        void OnValidate()
        {
            CacheSource();
            Configure();
        }

        void CacheSource()
        {
            if (_source == null)
                _source = GetComponent<AudioSource>();
        }

        void Configure()
        {
            if (_source == null)
                return;

            _source.playOnAwake = true;
            _source.loop = true;
            _source.spatialBlend = 0f;
            _source.volume = 1f;
            if (_clip != null)
                _source.clip = _clip;
        }

        void EnsurePlaying()
        {
            if (_source == null || _clip == null)
                return;

            if (_source.clip != _clip)
                _source.clip = _clip;

            if (!_source.isPlaying)
                _source.Play();
        }
    }
}
