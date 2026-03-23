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
        private InputActionMap _playerMap;
        private InputActionMap _buildModeMap;
        private InputAction _meditateAction;

        private void Awake()
        {
            if (cinemachineBrain != null)
                _mainCamera = cinemachineBrain.GetComponent<Camera>();

            if (inputActions != null)
            {
                _playerMap = inputActions.FindActionMap(playerMapName);
                _buildModeMap = inputActions.FindActionMap(buildModeMapName);
                _meditateAction = _playerMap?.FindAction("Meditate");
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
        }

        private void OnDisable()
        {
            GameEvents.OnMeditationToggled -= HandleMeditationToggled;
        }

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
            // Capture current action camera state
            Vector3 startPos = _mainCamera.transform.position;
            Quaternion startRot = _mainCamera.transform.rotation;
            float currentYaw = _mainCamera.transform.eulerAngles.y;

            // Disable Cinemachine so we can manually drive the main camera
            cinemachineBrain.enabled = false;

            // Initialize spirit sense target
            spiritSenseCamera.Initialize(playerTransform.position, currentYaw);
            var (endPos, endRot) = spiritSenseCamera.GetCurrentState();

            // Animate the main camera from action pose to spirit sense pose
            yield return AnimateCamera(_mainCamera.transform, startPos, startRot, endPos, endRot);

            // Switch: disable main camera, enable spirit sense camera
            _mainCamera.enabled = false;
            spiritSenseCamera.SetEnabled(true);
            SetCameraTag(_mainCamera.gameObject, false);
            SetCameraTag(spiritSenseCamera.gameObject, true);
            GameEvents.RaiseActiveCameraChanged(spiritSenseCamera.GetCamera());

            // Input: disable Player map, re-enable Meditate, enable BuildMode
            _playerMap?.Disable();
            _meditateAction?.Enable();
            _buildModeMap?.Enable();

            // Free cursor for build mode
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        private IEnumerator TransitionToAction()
        {
            // Capture spirit sense camera state
            var (startPos, startRot) = spiritSenseCamera.GetCurrentState();

            // Disable spirit sense camera
            spiritSenseCamera.SetEnabled(false);
            SetCameraTag(spiritSenseCamera.gameObject, false);

            // Enable main camera for rendering during transition
            _mainCamera.enabled = true;
            SetCameraTag(_mainCamera.gameObject, true);

            // Compute a reasonable end position behind the player
            // (Cinemachine will smooth from here when re-enabled)
            Vector3 endPos = ComputeActionCameraPosition();
            Quaternion endRot = Quaternion.LookRotation(
                playerTransform.position + Vector3.up * 1.5f - endPos);

            // Animate from spirit sense position back toward action position
            yield return AnimateCamera(_mainCamera.transform, startPos, startRot, endPos, endRot);

            // Re-enable Cinemachine — it blends smoothly from current transform
            cinemachineBrain.enabled = true;
            GameEvents.RaiseActiveCameraChanged(_mainCamera);

            // Input: disable BuildMode, enable full Player map
            _buildModeMap?.Disable();
            _playerMap?.Enable();

            // Lock cursor for action mode
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
            // Place camera behind and above the player at a reasonable default distance
            Vector3 playerPos = playerTransform.position;
            Vector3 back = -playerTransform.forward;
            return playerPos + back * 5f + Vector3.up * 2f;
        }

        private void SetCameraTag(GameObject go, bool isMain)
        {
            if (go == null) return;
            go.tag = isMain ? "MainCamera" : "Untagged";
        }
    }
}
