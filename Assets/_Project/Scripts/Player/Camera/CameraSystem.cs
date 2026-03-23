using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using CultivationGame.Core;

namespace CultivationGame.Player
{
    public class CameraSystem : MonoBehaviour
    {
        [Header("Cameras")]
        [SerializeField] private ActionCamera actionCamera;
        [SerializeField] private SpiritSenseCamera spiritSenseCamera;

        [Header("Player")]
        [SerializeField] private Transform playerTransform;
        [SerializeField] private PlayerStats playerStats;

        [Header("Transition")]
        [SerializeField] private float transitionDuration = 1.0f;
        [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Input Maps")]
        [SerializeField] private string playerMapName = "Player";
        [SerializeField] private string buildModeMapName = "BuildMode";

        private bool _isTransitioning;
        private bool _inSpiritSense;
        private InputActionMap _playerMap;
        private InputActionMap _buildModeMap;
        private InputAction _meditateAction;

        private void Awake()
        {
            // Resolve input action maps from the action camera's look action (same asset)
            var inputAsset = actionCamera.lookAction?.action?.actionMap?.asset;
            if (inputAsset != null)
            {
                _playerMap = inputAsset.FindActionMap(playerMapName);
                _buildModeMap = inputAsset.FindActionMap(buildModeMapName);
                _meditateAction = _playerMap?.FindAction("Meditate");
            }
        }

        private void Start()
        {
            // Action camera starts active, spirit sense starts disabled
            actionCamera.SetEnabled(true);
            spiritSenseCamera.SetEnabled(false);

            SetCameraTag(actionCamera.GetCamera(), true);
            SetCameraTag(spiritSenseCamera.GetCamera(), false);

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

            // Disable input on both cameras during transition
            actionCamera.SetEnabled(false);
            spiritSenseCamera.SetEnabled(false);

            // Keep a camera rendering during transition — use the outgoing one's transform
            Camera transitionCam = toSpiritSense
                ? actionCamera.GetCamera()
                : spiritSenseCamera.GetCamera();
            transitionCam.enabled = true;

            // Compute start and end states
            var (startPos, startRot) = toSpiritSense
                ? actionCamera.GetCurrentState()
                : spiritSenseCamera.GetCurrentState();

            if (toSpiritSense)
            {
                spiritSenseCamera.Initialize(playerTransform.position, actionCamera.Yaw);
            }

            var (endPos, endRot) = toSpiritSense
                ? spiritSenseCamera.GetCurrentState()
                : actionCamera.GetCurrentState();

            // Animated interpolation
            float elapsed = 0f;
            while (elapsed < transitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = transitionCurve.Evaluate(Mathf.Clamp01(elapsed / transitionDuration));
                transitionCam.transform.position = Vector3.Lerp(startPos, endPos, t);
                transitionCam.transform.rotation = Quaternion.Slerp(startRot, endRot, t);
                yield return null;
            }

            // Snap to final
            transitionCam.transform.position = endPos;
            transitionCam.transform.rotation = endRot;

            // Switch cameras
            if (toSpiritSense)
            {
                actionCamera.SetEnabled(false);
                spiritSenseCamera.SetEnabled(true);
                SetCameraTag(actionCamera.GetCamera(), false);
                SetCameraTag(spiritSenseCamera.GetCamera(), true);
                GameEvents.RaiseActiveCameraChanged(spiritSenseCamera.GetCamera());

                // Input: disable Player map, re-enable Meditate, enable BuildMode
                _playerMap?.Disable();
                _meditateAction?.Enable();
                _buildModeMap?.Enable();

                // Free cursor for build mode
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
            else
            {
                spiritSenseCamera.SetEnabled(false);
                actionCamera.SetEnabled(true);
                SetCameraTag(spiritSenseCamera.GetCamera(), false);
                SetCameraTag(actionCamera.GetCamera(), true);
                GameEvents.RaiseActiveCameraChanged(actionCamera.GetCamera());

                // Input: disable BuildMode, enable full Player map
                _buildModeMap?.Disable();
                _playerMap?.Enable();

                // Lock cursor for action mode
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }

            _inSpiritSense = toSpiritSense;
            _isTransitioning = false;
        }

        private void SetCameraTag(Camera cam, bool isMain)
        {
            if (cam == null) return;
            cam.gameObject.tag = isMain ? "MainCamera" : "Untagged";
        }
    }
}
