using UnityEngine;
using UnityEngine.UIElements;

namespace CultivationGame.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        public VisualElement Root { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            Root = GetComponent<UIDocument>().rootVisualElement;
            BroadcastMessage("InitializeUI", Root, SendMessageOptions.DontRequireReceiver);
        }
    }
}
