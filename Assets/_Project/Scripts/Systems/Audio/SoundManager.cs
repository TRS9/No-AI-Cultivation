using UnityEngine;
using CultivationGame.Data;

namespace CultivationGame.Systems
{
    public class SoundManager : MonoBehaviour
    {
        public static SoundManager Instance { get; private set; }

        [SerializeField] private AudioSource musicSource;

        [Range(0f, 1f)] [SerializeField] private float masterVolume = 1f;
        [Range(0f, 1f)] [SerializeField] private float sfxVolume = 1f;
        [Range(0f, 1f)] [SerializeField] private float musicVolume = 1f;

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

        public void PlaySFX(SoundClip clip, Vector3 position = default)
        {
            if (clip == null || clip.clip == null) return;

            float vol = clip.volume * sfxVolume * masterVolume;
            float pitch = 1f + Random.Range(-clip.pitchVariance, clip.pitchVariance);

            if (clip.is3D && position != default)
            {
                PlaySFX3D(clip.clip, position, vol, pitch);
            }
            else
            {
                PlaySFX2D(clip.clip, vol, pitch);
            }
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

        private void PlaySFX3D(AudioClip clip, Vector3 position, float volume, float pitch)
        {
            GameObject temp = new GameObject("SFX_Temp");
            temp.transform.position = position;
            AudioSource source = temp.AddComponent<AudioSource>();
            source.clip = clip;
            source.volume = volume;
            source.pitch = pitch;
            source.spatialBlend = 1f;
            source.Play();
            Destroy(temp, clip.length / Mathf.Max(pitch, 0.01f));
        }

        private void PlaySFX2D(AudioClip clip, float volume, float pitch)
        {
            GameObject temp = new GameObject("SFX_Temp");
            temp.transform.position = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
            AudioSource source = temp.AddComponent<AudioSource>();
            source.clip = clip;
            source.volume = volume;
            source.pitch = pitch;
            source.spatialBlend = 0f;
            source.Play();
            Destroy(temp, clip.length / Mathf.Max(pitch, 0.01f));
        }
    }
}
