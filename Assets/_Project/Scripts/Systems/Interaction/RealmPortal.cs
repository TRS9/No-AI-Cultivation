using UnityEngine;
using UnityEngine.SceneManagement;
using CultivationGame.Core;

namespace CultivationGame.Systems
{
    /// <summary>
    /// Place in the main world. On interact, picks a random biome + seed and loads
    /// the shared MinorRealm scene, which gets freshly generated each time.
    /// </summary>
    public class RealmPortal : MonoBehaviour, IInteractable
    {
        [Tooltip("Leave empty to draw from all BiomeType values.")]
        [SerializeField] private BiomeType[] availableBiomes;

        [SerializeField] private string minorRealmScene = "MinorRealm";

        public void Interact(GameObject user)
        {
            BiomeType biome = PickBiome();
            int seed = Random.Range(0, int.MaxValue);

            SceneTransitionData.SetRealm(biome, seed);
            SceneTransitionData.SetReturn(
                SceneManager.GetActiveScene().name,
                user.transform.position,
                user.transform.eulerAngles.y);

            SceneManager.LoadScene(minorRealmScene);
        }

        private BiomeType PickBiome()
        {
            if (availableBiomes != null && availableBiomes.Length > 0)
                return availableBiomes[Random.Range(0, availableBiomes.Length)];

            var values = (BiomeType[])System.Enum.GetValues(typeof(BiomeType));
            return values[Random.Range(0, values.Length)];
        }
    }
}
