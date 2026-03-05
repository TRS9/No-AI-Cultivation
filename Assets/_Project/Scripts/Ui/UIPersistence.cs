using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace CultivationGame.UI
{
    /// <summary>
    /// Attach to the UI_Manager root GameObject.
    /// Keeps the entire HUD alive across scene loads (DontDestroyOnLoad).
    /// After every scene load, re-assigns the new Main Camera to any
    /// Screen Space - Camera canvases so they never lose their render camera.
    /// </summary>
    public class UIPersistence : MonoBehaviour
    {
        private static UIPersistence _instance;

        [SerializeField] private Canvas[] cameraSpaceCanvases;

        private void Awake()
        {
            if (_instance != null)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Ensure an EventSystem exists — without one, UI buttons can't receive clicks.
            // The original EventSystem lives in the starting scene and gets destroyed on
            // scene load, so we recreate it if missing.
            if (EventSystem.current == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }

            if (cameraSpaceCanvases == null || cameraSpaceCanvases.Length == 0)
                return;

            var cam = Camera.main;
            if (cam == null)
                return;

            foreach (var canvas in cameraSpaceCanvases)
            {
                if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceCamera)
                    canvas.worldCamera = cam;
            }
        }
    }
}
