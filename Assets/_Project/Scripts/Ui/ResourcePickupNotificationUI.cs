using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using CultivationGame.Data;

namespace CultivationGame.UI
{
    /// <summary>
    /// Displays a short "+N Item Name" notification whenever a resource is extracted,
    /// giving the player immediate feedback for both manual and automated collection.
    /// </summary>
    public class ResourcePickupNotificationUI : MonoBehaviour
    {
        private VisualElement _container;

        public void InitializeUI(VisualElement root)
        {
            _container = root.Q<VisualElement>("ResourcePickupContainer");
            GameDataEvents.OnResourceExtracted += HandleResourceExtracted;
        }

        private void OnDisable()
        {
            GameDataEvents.OnResourceExtracted -= HandleResourceExtracted;
            StopAllCoroutines();
            _container?.Clear();
        }

        private void HandleResourceExtracted(ItemData resource, int amount)
        {
            if (_container == null || resource == null || amount <= 0) return;

            string itemName = GetItemDisplayName(resource);
            StartCoroutine(ShowNotification($"+{amount} {itemName}"));
        }

        private IEnumerator ShowNotification(string text)
        {
            var label = new Label(text);
            label.AddToClassList("resource-pickup");
            _container.Add(label);

            // Yield one frame so the initial style is applied before adding the visible class
            yield return null;

            label.AddToClassList("resource-pickup--visible");

            yield return new WaitForSeconds(1.5f);

            label.RemoveFromClassList("resource-pickup--visible");

            yield return new WaitForSeconds(0.5f);

            label.RemoveFromHierarchy();
        }

        private static string GetItemDisplayName(ItemData item)
        {
            if (item is RawMaterialData raw && !string.IsNullOrEmpty(raw.materialName))
                return raw.materialName;
            return item.name;
        }
    }
}
