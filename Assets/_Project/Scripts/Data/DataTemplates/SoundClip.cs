using UnityEngine;

namespace CultivationGame.Data
{
    [CreateAssetMenu(fileName = "NewSoundClip", menuName = "Cultivation/Sound Clip")]
    public class SoundClip : ScriptableObject
    {
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
        [Range(0f, 0.5f)] public float pitchVariance = 0.1f;
        public bool is3D = true;
    }
}
