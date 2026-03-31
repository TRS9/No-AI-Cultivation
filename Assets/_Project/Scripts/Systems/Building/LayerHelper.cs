using UnityEngine;

namespace CultivationGame.Systems
{
    /// <summary>
    /// Small utility for layer operations shared by PlacementController and SaveManager.
    /// </summary>
    public static class LayerHelper
    {
        /// <summary>
        /// Recursively sets the layer on a GameObject and all of its children.
        /// </summary>
        public static void SetLayerRecursive(GameObject obj, int layer)
        {
            obj.layer = layer;
            foreach (Transform child in obj.transform)
                SetLayerRecursive(child.gameObject, layer);
        }

        /// <summary>
        /// Returns the first layer index from a LayerMask bitmask.
        /// Returns 0 (Default) if no bits are set.
        /// </summary>
        public static int GetLayerFromMask(LayerMask mask)
        {
            int value = mask.value;
            for (int i = 0; i < 32; i++)
                if ((value & (1 << i)) != 0) return i;
            return 0;
        }
    }
}
