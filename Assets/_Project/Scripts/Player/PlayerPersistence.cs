using UnityEngine;

namespace CultivationGame.Player
{
    /// <summary>
    /// Attach to the Player root GameObject.
    /// Keeps the player (and its camera child) alive across scene loads so portals
    /// work without duplicating the player setup in every scene.
    /// Destroys duplicates automatically when the scene is reloaded (e.g. New Game).
    /// </summary>
    public class PlayerPersistence : MonoBehaviour
    {
        private static PlayerPersistence _instance;

        private void Awake()
        {
            if (_instance != null)
            {
                // A persistent player already exists — this one is a duplicate from a scene reload.
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }
    }
}
