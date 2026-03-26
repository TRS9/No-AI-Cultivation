using UnityEngine;
using CultivationGame.Core;
using CultivationGame.Data;

namespace CultivationGame.Systems
{
    /// <summary>
    /// A Qi power conduit (like a power pole in Satisfactory).
    /// Placed on the build grid. Forms a chain from the player's Qi source
    /// to power machines within its radius.
    ///
    /// connectionRadius: max distance to connect to another conduit.
    /// machineRadius: max distance to power nearby machines.
    /// </summary>
    public class QiConduit : MonoBehaviour, IInteractable, IMachineConnectable
    {
        [Header("Machine Configuration")]
        [SerializeField] private MachineData machineData;

        [Header("Conduit Settings")]
        [SerializeField] private float connectionRadius = 12f;
        [SerializeField] private float machineRadius = 8f;

        /// <summary>Set by QiNetwork. True if this conduit is reachable from the Qi source.</summary>
        public bool IsConnected { get; set; }

        public float ConnectionRadius => connectionRadius;
        public float MachineRadius => machineRadius;

        // --- IMachineConnectable (conduits don't transport items) ---
        public MachineData MachineData => machineData;
        public MachineInventory InputInventory => null;
        public MachineInventory OutputInventory => null;

        private void Start()
        {
            if (QiNetwork.Instance != null)
                QiNetwork.Instance.RegisterConduit(this);
        }

        private void OnDestroy()
        {
            if (QiNetwork.Instance != null)
                QiNetwork.Instance.UnregisterConduit(this);
        }

        public void SetMachineData(MachineData data)
        {
            machineData = data;
        }

        public void Interact(GameObject user)
        {
            GameDataEvents.RaiseMachineInteracted(this);
        }

        private void OnDrawGizmosSelected()
        {
            // Visualize connection radius (conduit-to-conduit)
            Gizmos.color = IsConnected ? new Color(0.2f, 0.6f, 1f, 0.3f) : new Color(1f, 0.3f, 0.3f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, connectionRadius);

            // Visualize machine radius (powers machines in range)
            Gizmos.color = IsConnected ? new Color(0.2f, 1f, 0.4f, 0.2f) : new Color(0.5f, 0.5f, 0.5f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, machineRadius);
        }
    }
}
