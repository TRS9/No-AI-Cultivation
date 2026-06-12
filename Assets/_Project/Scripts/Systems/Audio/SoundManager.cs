using System.Collections.Generic;
using UnityEngine;
using CultivationGame.Data;

namespace CultivationGame.Systems
{
    public class SoundManager : MonoBehaviour
    {
        public static SoundManager Instance { get; private set; }

        [SerializeField] [Tooltip("AudioSource used for background music; auto-created if not assigned.")] private AudioSource musicSource;

        [Range(0f, 1f)] [SerializeField] [Tooltip("Global volume multiplier applied to all sounds.")] private float masterVolume = 1f;
        [Range(0f, 1f)] [SerializeField] [Tooltip("Volume multiplier applied to all sound effects.")] private float sfxVolume = 1f;
        [Range(0f, 1f)] [SerializeField] [Tooltip("Volume multiplier applied to background music.")] private float musicVolume = 1f;

        private const int MaxPooledSources = 16;

        // Pooled one-shot sources — reused instead of creating/destroying a
        // GameObject per sound effect.
        private readonly List<AudioSource> _sfxPool = new();

        public float MasterVolume => masterVolume;
        public float SFXVolume => sfxVolume;
        public float MusicVolume => musicVolume;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
                musicSource.loop = true;
                musicSource.playOnAwake = false;
            }
        }

        /// <summary>
        /// Plays a sound effect. Pass a position for 3D clips; omit it for UI/2D sounds.
        /// </summary>
        public void PlaySFX(SoundClip clip, Vector3? position = null)
        {
            if (clip == null || clip.clip == null) return;

            float vol = clip.volume * sfxVolume * masterVolume;
            float pitch = 1f + Random.Range(-clip.pitchVariance, clip.pitchVariance);

            var source = GetPooledSource();
            if (source == null) return; // pool exhausted — drop the sound

            bool spatial = clip.is3D && position.HasValue;
            source.transform.position = spatial ? position.Value : transform.position;
            source.spatialBlend = spatial ? 1f : 0f;
            source.clip = clip.clip;
            source.volume = vol;
            source.pitch = pitch;
            source.Play();
        }

        public void PlayMusic(AudioClip music, bool loop = true)
        {
            if (musicSource == null) return;
            musicSource.clip = music;
            musicSource.loop = loop;
            musicSource.volume = musicVolume * masterVolume;
            musicSource.Play();
        }

        public void SetMasterVolume(float vol)
        {
            masterVolume = Mathf.Clamp01(vol);
            ApplyMusicVolume();
        }

        public void SetSFXVolume(float vol)
        {
            sfxVolume = Mathf.Clamp01(vol);
        }

        public void SetMusicVolume(float vol)
        {
            musicVolume = Mathf.Clamp01(vol);
            ApplyMusicVolume();
        }

        private void ApplyMusicVolume()
        {
            if (musicSource != null)
                musicSource.volume = musicVolume * masterVolume;
        }

        private AudioSource GetPooledSource()
        {
            foreach (var source in _sfxPool)
                if (source != null && !source.isPlaying)
                    return source;

            if (_sfxPool.Count >= MaxPooledSources)
                return null;

            var go = new GameObject($"SFX_{_sfxPool.Count}");
            go.transform.SetParent(transform, false);
            var newSource = go.AddComponent<AudioSource>();
            newSource.playOnAwake = false;
            _sfxPool.Add(newSource);
            return newSource;
        }
    }
}
