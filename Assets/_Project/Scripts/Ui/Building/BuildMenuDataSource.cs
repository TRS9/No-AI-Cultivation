using System;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using Unity.Properties;
using CultivationGame.Data;

namespace CultivationGame.UI
{
    [Serializable]
    public struct MachineSlotData
    {
        public MachineData Machine;
        public string Name;
        public Sprite Icon;
    }

    [CreateAssetMenu(menuName = "Cultivation/UI/Build Menu Data Source")]
    public class BuildMenuDataSource : ScriptableObject, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        [CreateProperty]
        public List<MachineSlotData> Machines { get; private set; } = new();

        [SerializeField] private MachineData[] availableMachines;

        private void Notify(string name)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        /// <summary>
        /// Populates the Machines list from the serialized availableMachines array.
        /// Called when the build menu opens (spirit sense mode entered).
        /// </summary>
        public void BuildMachineList()
        {
            Machines.Clear();
            if (availableMachines == null) return;

            foreach (var machine in availableMachines)
            {
                if (machine == null) continue;
                Machines.Add(new MachineSlotData
                {
                    Machine = machine,
                    Name = machine.machineName,
                    Icon = machine.icon
                });
            }
            Notify(nameof(Machines));
        }

        public void ResetState()
        {
            Machines.Clear();
            Notify(nameof(Machines));
        }
    }
}
