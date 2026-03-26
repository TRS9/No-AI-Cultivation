using System.Collections.Generic;
using UnityEngine;
using CultivationGame.Core;
using CultivationGame.Data;

namespace CultivationGame.Systems
{
    /// <summary>
    /// Manages the Qi power grid. Tracks all QiConduits and machines,
    /// determines which machines are powered (within range of a connected conduit),
    /// and deducts Qi from the player pool each frame.
    ///
    /// Like power lines in Satisfactory: conduits form a chain from the player's
    /// base outward. Machines near a connected conduit receive power.
    /// </summary>
    public class QiNetwork : MonoBehaviour
    {
        public static QiNetwork Instance { get; private set; }

        [Header("Qi Source")]
        [Tooltip("Position of the Qi source (player base / meditation spot). " +
                 "Conduits within range of this point are automatically connected.")]
        [SerializeField] private Transform qiSourcePoint;
        [SerializeField] private float qiSourceRadius = 10f;

        private readonly List<QiConduit> _conduits = new();
        private readonly List<BaseMachine> _machines = new();
        private readonly HashSet<QiConduit> _connectedConduits = new();

        private bool _networkDirty = true;
        private float _totalDemand;

        // --- Public API ---
        public float TotalDemand => _totalDemand;
        public int ConnectedConduitCount => _connectedConduits.Count;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void LateUpdate()
        {
            if (_networkDirty)
            {
                RebuildConnectivity();
                _networkDirty = false;
            }

            UpdatePowerState();
            ConsumeQi();
        }

        // ------------------------------------------------------------------ //
        //  Registration
        // ------------------------------------------------------------------ //

        public void RegisterConduit(QiConduit conduit)
        {
            if (!_conduits.Contains(conduit))
            {
                _conduits.Add(conduit);
                _networkDirty = true;
            }
        }

        public void UnregisterConduit(QiConduit conduit)
        {
            if (_conduits.Remove(conduit))
            {
                _connectedConduits.Remove(conduit);
                _networkDirty = true;
            }
        }

        public void RegisterMachine(BaseMachine machine)
        {
            if (!_machines.Contains(machine))
                _machines.Add(machine);
        }

        public void UnregisterMachine(BaseMachine machine)
        {
            _machines.Remove(machine);
        }

        /// <summary>Call when a conduit is placed or removed to trigger connectivity rebuild.</summary>
        public void SetDirty()
        {
            _networkDirty = true;
        }

        // ------------------------------------------------------------------ //
        //  Connectivity (BFS from Qi source)
        // ------------------------------------------------------------------ //

        /// <summary>
        /// BFS/flood-fill from the Qi source position through conduits.
        /// A conduit is "connected" if it's within range of the source OR
        /// within connectionRadius of another connected conduit.
        /// </summary>
        private void RebuildConnectivity()
        {
            _connectedConduits.Clear();

            Vector3 sourcePos = qiSourcePoint != null
                ? qiSourcePoint.position
                : transform.position;

            // Seed: conduits within range of the Qi source
            var queue = new Queue<QiConduit>();
            foreach (var conduit in _conduits)
            {
                float dist = Vector3.Distance(conduit.transform.position, sourcePos);
                if (dist <= qiSourceRadius)
                {
                    _connectedConduits.Add(conduit);
                    queue.Enqueue(conduit);
                }
            }

            // BFS: spread connectivity through conduit chain
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var other in _conduits)
                {
                    if (_connectedConduits.Contains(other)) continue;

                    float dist = Vector3.Distance(
                        current.transform.position,
                        other.transform.position);

                    if (dist <= current.ConnectionRadius)
                    {
                        _connectedConduits.Add(other);
                        queue.Enqueue(other);
                    }
                }
            }

            // Update conduit connected state
            foreach (var conduit in _conduits)
                conduit.IsConnected = _connectedConduits.Contains(conduit);
        }

        // ------------------------------------------------------------------ //
        //  Power state
        // ------------------------------------------------------------------ //

        /// <summary>
        /// A machine is powered if it's within machineRadius of any connected conduit.
        /// </summary>
        private void UpdatePowerState()
        {
            for (int i = _machines.Count - 1; i >= 0; i--)
            {
                if (_machines[i] == null)
                {
                    _machines.RemoveAt(i);
                    continue;
                }

                _machines[i].IsPowered = IsMachinePowered(_machines[i]);
            }
        }

        private bool IsMachinePowered(BaseMachine machine)
        {
            // Machines with 0 qiConsumptionRate are always powered (passive machines)
            if (machine.MachineData != null && machine.MachineData.qiConsumptionRate <= 0f)
                return true;

            Vector3 machinePos = machine.transform.position;
            foreach (var conduit in _connectedConduits)
            {
                float dist = Vector3.Distance(machinePos, conduit.transform.position);
                if (dist <= conduit.MachineRadius)
                    return true;
            }
            return false;
        }

        // ------------------------------------------------------------------ //
        //  Qi consumption
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Sum up Qi demand from all active, powered machines and deduct from player.
        /// If player doesn't have enough Qi, cut power to all machines.
        /// </summary>
        private void ConsumeQi()
        {
            _totalDemand = 0f;

            foreach (var machine in _machines)
            {
                if (machine == null || !machine.IsPowered) continue;
                if (!machine.IsProcessing) continue;
                if (machine.MachineData == null) continue;

                _totalDemand += machine.MachineData.qiConsumptionRate;
            }

            if (_totalDemand <= 0f) return;

            float qiToDeduct = _totalDemand * Time.deltaTime;

            // Deduct Qi from the player via the existing AddQi event (negative value)
            GameEvents.RaiseAddQi(-qiToDeduct);

            // Raise network status event for UI
            GameDataEvents.RaiseQiNetworkChanged(_totalDemand, 0f);
        }
    }
}
