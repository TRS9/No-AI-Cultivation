using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    /// Like power lines in Satisfactory: conduits form a chain from the Qi source
    /// outward. Machines near a connected conduit receive power.
    ///
    /// If no QiNetwork exists in the scene, one is created on demand the first
    /// time a machine or conduit registers (GetOrCreate). The Qi source falls
    /// back to the first CraftingStation, then the player, then this transform.
    /// </summary>
    public class QiNetwork : MonoBehaviour
    {
        public static QiNetwork Instance { get; private set; }

        [Header("Qi Source")]
        [Tooltip("Position of the Qi source (player base / meditation spot). " +
                 "Conduits within range of this point are automatically connected. " +
                 "Falls back to the first CraftingStation, then the player.")]
        [SerializeField] private Transform qiSourcePoint;
        [SerializeField] private float qiSourceRadius = 10f;

        private const float PowerUpdateInterval = 0.25f;

        private readonly List<QiConduit> _conduits = new();
        private readonly List<BaseMachine> _machines = new();
        private readonly HashSet<QiConduit> _connectedConduits = new();

        private bool _networkDirty = true;
        private float _totalDemand;
        private float _powerUpdateTimer;
        private bool _sourceIsMobile;
        private bool _playerHasQi = true;

        // --- Public API ---
        public float TotalDemand => _totalDemand;
        public int ConnectedConduitCount => _connectedConduits.Count;
        public ReadOnlyCollection<BaseMachine> RegisteredMachines => _machines.AsReadOnly();

        /// <summary>
        /// Returns the active QiNetwork, creating one at runtime if no scene object
        /// provides it. Keeps machines functional in scenes without manual setup.
        /// </summary>
        public static QiNetwork GetOrCreate()
        {
            if (Instance != null) return Instance;

            var existing = FindFirstObjectByType<QiNetwork>();
            if (existing != null)
            {
                Instance = existing;
                return existing;
            }

            var go = new GameObject("QiNetwork (Runtime)");
            return go.AddComponent<QiNetwork>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            ResolveQiSource();
        }

        private void OnEnable()
        {
            GameEvents.OnQiChanged += HandleQiChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnQiChanged -= HandleQiChanged;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void HandleQiChanged(double current, double max)
        {
            bool hasQi = current > 0.0;
            if (hasQi != _playerHasQi)
            {
                _playerHasQi = hasQi;
                _networkDirty = true; // re-evaluate power immediately
            }
        }

        private void ResolveQiSource()
        {
            if (qiSourcePoint != null) return;

            var station = FindFirstObjectByType<CraftingStation>();
            if (station != null)
            {
                qiSourcePoint = station.transform;
                return;
            }

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                qiSourcePoint = player.transform;
                _sourceIsMobile = true;
            }
        }

        private void LateUpdate()
        {
            _powerUpdateTimer -= Time.deltaTime;
            bool powerTick = _powerUpdateTimer <= 0f;

            bool rebuilt = false;
            if (_networkDirty || (powerTick && _sourceIsMobile))
            {
                RebuildConnectivity();
                _networkDirty = false;
                rebuilt = true;
            }

            if (powerTick || rebuilt)
            {
                _powerUpdateTimer = PowerUpdateInterval;
                UpdatePowerState();
            }

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
            {
                _machines.Add(machine);
                _networkDirty = true;
            }
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
            float sourceRadiusSq = qiSourceRadius * qiSourceRadius;
            foreach (var conduit in _conduits)
            {
                if ((conduit.transform.position - sourcePos).sqrMagnitude <= sourceRadiusSq)
                {
                    _connectedConduits.Add(conduit);
                    queue.Enqueue(conduit);
                }
            }

            // BFS: spread connectivity through conduit chain
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                float connectRadiusSq = current.ConnectionRadius * current.ConnectionRadius;
                foreach (var other in _conduits)
                {
                    if (_connectedConduits.Contains(other)) continue;

                    float distSq = (current.transform.position - other.transform.position).sqrMagnitude;
                    if (distSq <= connectRadiusSq)
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

            // Power is cut for consuming machines when the player has no Qi left.
            if (!_playerHasQi)
                return false;

            Vector3 machinePos = machine.transform.position;
            foreach (var conduit in _connectedConduits)
            {
                float radiusSq = conduit.MachineRadius * conduit.MachineRadius;
                if ((machinePos - conduit.transform.position).sqrMagnitude <= radiusSq)
                    return true;
            }
            return false;
        }

        // ------------------------------------------------------------------ //
        //  Qi consumption
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Sum up Qi demand from all active, powered machines and deduct from player.
        /// When the player runs dry, HandleQiChanged flips _playerHasQi and the next
        /// power update stalls all consuming machines.
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

            // Deduct Qi from the player via the existing AddQi event (negative value).
            // PlayerStats clamps at 0 — it can never go negative.
            GameEvents.RaiseAddQi(-qiToDeduct);

            // Raise network status event for UI
            GameDataEvents.RaiseQiNetworkChanged(_totalDemand, 0f);
        }
    }
}
