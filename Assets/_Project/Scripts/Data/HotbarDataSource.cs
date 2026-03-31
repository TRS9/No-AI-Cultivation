using System;
using UnityEngine;

namespace CultivationGame.Data
{
    [CreateAssetMenu(fileName = "HotbarDataSource", menuName = "Cultivation/UI/Hotbar Data Source")]
    public class HotbarDataSource : ScriptableObject
    {
        [SerializeField] private MachineData[] slots = new MachineData[10];

        public event Action<int> OnSlotChanged;

        public void SetSlot(int index, MachineData data)
        {
            if (index < 0 || index >= slots.Length) return;
            slots[index] = data;
            OnSlotChanged?.Invoke(index);
        }

        public void ClearSlot(int index)
        {
            if (index < 0 || index >= slots.Length) return;
            slots[index] = null;
            OnSlotChanged?.Invoke(index);
        }

        public MachineData GetSlot(int index)
        {
            if (index < 0 || index >= slots.Length) return null;
            return slots[index];
        }
    }
}
