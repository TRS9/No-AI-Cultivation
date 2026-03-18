using CultivationGame.Core;
using UnityEngine;

namespace CultivationGame.Systems
{
    public class CraftingStation : MonoBehaviour, IInteractable
    {
        public void Interact(GameObject user)
        {
            GameEvents.RaiseCraftingStationInteracted();
        }
    }
}