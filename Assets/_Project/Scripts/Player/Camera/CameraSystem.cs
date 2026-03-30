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
        [SerializeField] private CinemachineBrain cinemachineBrain;
        [SerializeField] private CinemachineCamera cinemachineVCam;

        [Header("Spirit Sense (Build Mode)")]
        [SerializeField] private SpiritSenseCamera spiritSenseCamera;

        [Header("Player")]
        [SerializeField] private Transform playerTransform;
        [SerializeField] private PlayerStats playerStats;

        [Header("Transition")]
        [SerializeField] private float transitionDuration = 1.0f;
        [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Input")]
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private string playerMapName = "Player";
        [SerializeField] private string buildModeMapName = "BuildMode";

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
        /// </summary>
        private void HandlePanelOpened(string panelId, bool isOpen)
        {
            if (isOpen && _isBuildMode)
                SetBuildMode(false);
        }

        private void SetBuildMode(bool enabled)
        {
            if (_isBuildMode == enabled) return;
            _isBuildMode = enabled;

            if (_isBuildMode && !_inSpiritSense)
            {
                // In 3rd person: enable BuildMode map additively so Place/Cancel/Rotate work
                _buildModeMap?.Enable();
                // Disable Cinemachine look input so mouse controls the cursor, not the camera
                if (_inputAxisController != null)
                    _inputAxisController.enabled = false;
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
            else if (!_isBuildMode && !_inSpiritSense)
            {
                // Back to pure 3rd person
                _buildModeMap?.Disable();
                // Re-enable Cinemachine look input for normal camera control
                if (_inputAxisController != null)
                    _inputAxisController.enabled = true;
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }

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

            // Auto-enable build mode when entering Spirit Sense
            if (!_isBuildMode)
                SetBuildMode(true);

            // Input: disable Player map, re-enable Meditate, enable BuildMode
            _playerMap?.Disable();
            _meditateAction?.Enable();
            // Keep BuildToggle available in Spirit Sense
            _buildToggleAction?.Enable();
            _buildModeMap?.Enable();

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
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

            // Always re-enable Cinemachine look input when returning to action mode.
            // SetBuildMode checks _inSpiritSense (still true here) and skips the
            // branch that would normally restore the input axis controller, so we
            // must do it explicitly before the build-mode cleanup.
            if (_inputAxisController != null)
                _inputAxisController.enabled = true;

            // Disable build mode when leaving Spirit Sense
            if (_isBuildMode)
                SetBuildMode(false);

            // Input: disable BuildMode, enable full Player map
            _buildModeMap?.Disable();
            _playerMap?.Enable();

            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
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
