using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;
using CultivationGame.Core;

namespace CultivationGame.Player
{
    public class CameraSystem : MonoBehaviour
    {
        [Header("Cinemachine (Action Mode)")]
        [SerializeField] [Tooltip("Cinemachine brain component attached to the main camera.")] private CinemachineBrain cinemachineBrain;
        [SerializeField] [Tooltip("The active Cinemachine virtual camera used in action mode.")] private CinemachineCamera cinemachineVCam;

        [Header("Spirit Sense (Free Camera)")]
        [SerializeField] [Tooltip("The Spirit Sense free camera used during meditation.")] private SpiritSenseCamera spiritSenseCamera;

        [Header("Player")]
        [SerializeField] [Tooltip("The player's transform, used to compute camera positions during transitions.")] private Transform playerTransform;
        [SerializeField] [Tooltip("Player stats, used to read the current realm's Spirit Sense range.")] private PlayerStats playerStats;

        [Header("Transition")]
        [SerializeField] [Tooltip("Duration in seconds for the camera blend between action and Spirit Sense modes.")] private float transitionDuration = 1.0f;
        [SerializeField] [Tooltip("Animation curve defining the easing of camera transitions.")] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Input")]
        [SerializeField] [Tooltip("The Input Action Asset containing Player and BuildMode action maps.")] private InputActionAsset inputActions;
        [SerializeField] [Tooltip("Name of the Player action map inside the Input Action Asset.")] private string playerMapName = "Player";
        [SerializeField] [Tooltip("Name of the BuildMode action map inside the Input Action Asset.")] private string buildModeMapName = "BuildMode";

        private Camera _mainCamera;
        private bool _isTransitioning;
        private bool _inSpiritSense;
        private bool _isBuildMode;
        private InputActionMap _playerMap;
        private InputActionMap _buildModeMap;
        private InputAction _meditateAction;
        private InputAction _buildToggleAction;
        private CinemachineInputAxisController _inputAxisController;

        // Saved Cinemachine state for smooth return transition
        private Vector3 _savedActionPos;
        private Quaternion _savedActionRot;
        private bool _hasSavedActionState;

        private void Awake()
        {
            if (cinemachineBrain != null)
                _mainCamera = cinemachineBrain.GetComponent<Camera>();

            if (cinemachineVCam != null)
                _inputAxisController = cinemachineVCam.GetComponent<CinemachineInputAxisController>();

            if (inputActions != null)
            {
                _playerMap = inputActions.FindActionMap(playerMapName);
                _buildModeMap = inputActions.FindActionMap(buildModeMapName);
                _meditateAction = _playerMap?.FindAction("Meditate");
                _buildToggleAction = _playerMap?.FindAction("BuildToggle");
            }
        }

        private void Start()
        {
            spiritSenseCamera.SetEnabled(false);
            _buildModeMap?.Disable();
        }

        private void OnEnable()
        {
            GameEvents.OnMeditationToggled += HandleMeditationToggled;
            GameEvents.OnPanelStateChanged += HandlePanelOpened;

            if (_buildToggleAction != null)
                _buildToggleAction.performed += OnBuildTogglePerformed;
        }

        private void OnDisable()
        {
            GameEvents.OnMeditationToggled -= HandleMeditationToggled;
            GameEvents.OnPanelStateChanged -= HandlePanelOpened;

            if (_buildToggleAction != null)
                _buildToggleAction.performed -= OnBuildTogglePerformed;
        }

        // ------------------------------------------------------------------ //
        //  Build Mode Toggle (B key — works in any camera perspective)
        // ------------------------------------------------------------------ //

        private void OnBuildTogglePerformed(InputAction.CallbackContext ctx)
        {
            if (_isTransitioning) return;
            SetBuildMode(!_isBuildMode);
        }

        /// <summary>
        /// Force-exit build mode when a UI panel opens (Inventory, MachineInspect, etc.).
        /// The Machine Catalogue is special: it stays inside build mode but disables
        /// camera look so the free cursor can be used for drag-and-drop.
        /// </summary>
        private void HandlePanelOpened(string panelId, bool isOpen)
        {
            if (panelId == "MachineCatalogue")
            {
                if (_inputAxisController != null)
                    _inputAxisController.enabled = !isOpen;
                return;
            }

            if (isOpen && _isBuildMode)
                SetBuildMode(false);
        }

        private void SetBuildMode(bool enabled)
        {
            if (_isBuildMode == enabled) return;
            _isBuildMode = enabled;

            if (_isBuildMode)
            {
                // Enable BuildMode map (may already be enabled in Spirit Sense for camera controls)
                _buildModeMap?.Enable();

                // Keep Cinemachine mouse look enabled — placement uses screen-centre
                // raycasts, not the mouse cursor, so free-look still works.
            }
            else
            {
                if (!_inSpiritSense)
                {
                    // Back to pure 3rd person — disable BuildMode map
                    _buildModeMap?.Disable();
                }
                // else: in Spirit Sense, keep BuildMode map enabled for camera controls
            }

            // Cursor stays locked and hidden in build mode (same as action mode).
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;

            GameEvents.RaiseBuildModeToggled(_isBuildMode);
        }

        // ------------------------------------------------------------------ //
        //  Meditation → Spirit Sense camera transition
        // ------------------------------------------------------------------ //

        private void HandleMeditationToggled(bool isMeditating)
        {
            if (_isTransitioning) return;
            StartCoroutine(TransitionCoroutine(isMeditating));
        }

        private IEnumerator TransitionCoroutine(bool toSpiritSense)
        {
            _isTransitioning = true;

            if (toSpiritSense)
                yield return TransitionToSpiritSense();
            else
                yield return TransitionToAction();

            _inSpiritSense = toSpiritSense;
            _isTransitioning = false;
        }

        private IEnumerator TransitionToSpiritSense()
        {
            Vector3 startPos = _mainCamera.transform.position;
            Quaternion startRot = _mainCamera.transform.rotation;
            float currentYaw = _mainCamera.transform.eulerAngles.y;

            // Save Cinemachine state BEFORE disabling — used for smooth return
            _savedActionPos = _mainCamera.transform.position;
            _savedActionRot = _mainCamera.transform.rotation;
            _hasSavedActionState = true;

            // Disable Cinemachine so we can manually drive the main camera
            cinemachineBrain.enabled = false;

            // Set realm-based zoom range
            if (playerStats != null && playerStats.currentRealm != null)
                spiritSenseCamera.SetMaxZoom(playerStats.currentRealm.spiritSenseRange);

            // Initialize spirit sense — it will drive _mainCamera.transform from now on
            spiritSenseCamera.Initialize(_mainCamera.transform, playerTransform.position, currentYaw);
            var (endPos, endRot) = spiritSenseCamera.GetCurrentState();

            // Animate the main camera from action pose to spirit sense pose
            yield return AnimateCamera(_mainCamera.transform, startPos, startRot, endPos, endRot);

            // Spirit sense takes over driving the main camera
            spiritSenseCamera.SetEnabled(true);

            // Input: disable Player map, re-enable Meditate and BuildToggle,
            // enable BuildMode map for camera controls (Pan/Orbit/Zoom)
            _playerMap?.Disable();
            _meditateAction?.Enable();
            _buildToggleAction?.Enable();
            _buildModeMap?.Enable();

            // Spirit sense does NOT enable the mouse — cursor stays locked.
            // The crosshair at screen centre shows where placement / interaction occurs.
        }

        private IEnumerator TransitionToAction()
        {
            var (startPos, startRot) = spiritSenseCamera.GetCurrentState();

            // Spirit sense stops driving the camera
            spiritSenseCamera.SetEnabled(false);

            // Use saved Cinemachine position for smooth return (no jump)
            Vector3 endPos;
            Quaternion endRot;
            if (_hasSavedActionState)
            {
                endPos = _savedActionPos;
                endRot = _savedActionRot;
            }
            else
            {
                endPos = ComputeActionCameraPosition();
                endRot = Quaternion.LookRotation(
                    playerTransform.position + Vector3.up * 1.5f - endPos);
            }

            // Animate back toward action position
            yield return AnimateCamera(_mainCamera.transform, startPos, startRot, endPos, endRot);

            // Set camera transform to end position before re-enabling Cinemachine
            // so Cinemachine blends from the correct position
            _mainCamera.transform.position = endPos;
            _mainCamera.transform.rotation = endRot;

            // Re-enable Cinemachine — it picks up from the current transform
            cinemachineBrain.enabled = true;

            // Re-enable full Player map
            _playerMap?.Enable();

            // Restore locked cursor and normal camera controls
            if (_inputAxisController != null)
                _inputAxisController.enabled = true;
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;

            if (!_isBuildMode)
                _buildModeMap?.Disable();
        }

        private IEnumerator AnimateCamera(Transform cam, Vector3 startPos, Quaternion startRot,
            Vector3 endPos, Quaternion endRot)
        {
            float elapsed = 0f;
            while (elapsed < transitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = transitionCurve.Evaluate(Mathf.Clamp01(elapsed / transitionDuration));
                cam.position = Vector3.Lerp(startPos, endPos, t);
                cam.rotation = Quaternion.Slerp(startRot, endRot, t);
                yield return null;
            }
            cam.position = endPos;
            cam.rotation = endRot;
        }

        private Vector3 ComputeActionCameraPosition()
        {
            Vector3 playerPos = playerTransform.position;
            Vector3 back = -playerTransform.forward;
            return playerPos + back * 5f + Vector3.up * 2f;
        }
    }
}
