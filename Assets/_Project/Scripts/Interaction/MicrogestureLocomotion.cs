using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace VRWatch.Interaction
{
    /// <summary>
    /// Implements Meta Horizon Home "Microgesture" locomotion and turning.
    /// Allows users to navigate comfortably with their hands resting in their lap:
    /// - Thumb Swipe Forward: Glides the player forward in gaze direction.
    /// - Thumb Swipe Backward: Glides the player backward.
    /// - Thumb Swipe Left: Snap-turns the player 45° to the left.
    /// - Thumb Swipe Right: Snap-turns the player 45° to the right.
    /// - Thumb Tap: Comfort hop forward / instant halt.
    /// </summary>
    [DisallowMultipleComponent]
    public class MicrogestureLocomotion : MonoBehaviour
    {
        [Header("Hand Sources")]
        [Tooltip("Right hand microgesture event source (primary navigation hand).")]
        [SerializeField] private OVRMicrogestureEventSource rightGestureSource;

        [Tooltip("Left hand microgesture event source (optional secondary hand).")]
        [SerializeField] private OVRMicrogestureEventSource leftGestureSource;

        [Header("Locomotion Dynamics")]
        [Tooltip("Impulse speed in m/s added per forward thumb swipe.")]
        [SerializeField] private float forwardSwipeImpulse = 1.6f;

        [Tooltip("Impulse speed in m/s added per backward thumb swipe.")]
        [SerializeField] private float backwardSwipeImpulse = 1.2f;

        [Tooltip("Maximum movement velocity.")]
        [SerializeField] private float maxVelocity = 2.4f;

        [Tooltip("Velocity damping rate (friction/deceleration).")]
        [SerializeField] private float decelerationDrag = 5.5f;

        [Header("Turning Dynamics")]
        [Tooltip("Angle in degrees for snap turns on left/right swipes.")]
        [SerializeField] private float snapTurnAngle = 45f;

        [Tooltip("Minimum time in seconds between snap turns.")]
        [SerializeField] private float turnCooldown = 0.28f;

        [Header("Room Boundaries")]
        [SerializeField] private Vector2 roomBoundsX = new Vector2(-4.2f, 4.2f);
        [SerializeField] private Vector2 roomBoundsZ = new Vector2(-4.2f, 4.2f);

        [Header("Audio Feedback")]
        [SerializeField] private AudioSource audioSource;

        // References
        private OVRCameraRig _cameraRig;
        private Transform _headTransform;
        private LoungeModeManager _modeManager;

        // Runtime State
        private Vector3 _currentVelocity = Vector3.zero;
        private float _lastTurnTime = -1f;
        private AudioClip _swipeClip;
        private AudioClip _turnClip;

        // Visual HUD Prompt
        private GameObject _hudObj;
        private TextMesh _hudText;
        private float _hudFadeTimer = 0f;

        private void Awake()
        {
            _cameraRig = GetComponent<OVRCameraRig>();
            if (_cameraRig == null) _cameraRig = UnityEngine.Object.FindObjectOfType<OVRCameraRig>();

            _modeManager = UnityEngine.Object.FindObjectOfType<LoungeModeManager>();

            SetupAudio();
            SetupHUD();
        }

        private void Start()
        {
            FindGestureSources();
            SubscribeEvents();
        }

        private void OnEnable()
        {
            SubscribeEvents();
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
        }

        private void FindGestureSources()
        {
            if (rightGestureSource == null)
            {
                var rightHandGo = GameObject.Find("[BuildingBlock] Hand Tracking right");
                if (rightHandGo != null)
                {
                    rightGestureSource = rightHandGo.GetComponent<OVRMicrogestureEventSource>();
                    if (rightGestureSource == null) rightGestureSource = rightHandGo.AddComponent<OVRMicrogestureEventSource>();
                    rightGestureSource.Hand = rightHandGo.GetComponent<OVRHand>();
                }
            }

            if (leftGestureSource == null)
            {
                var leftHandGo = GameObject.Find("[BuildingBlock] Hand Tracking left");
                if (leftHandGo != null)
                {
                    leftGestureSource = leftHandGo.GetComponent<OVRMicrogestureEventSource>();
                    if (leftGestureSource == null) leftGestureSource = leftHandGo.AddComponent<OVRMicrogestureEventSource>();
                    leftGestureSource.Hand = leftHandGo.GetComponent<OVRHand>();
                }
            }

            if (_cameraRig != null && _cameraRig.centerEyeAnchor != null)
            {
                _headTransform = _cameraRig.centerEyeAnchor;
            }
            else
            {
                var cam = Camera.main;
                if (cam != null) _headTransform = cam.transform;
            }
        }

        private void SubscribeEvents()
        {
            if (rightGestureSource != null)
            {
                rightGestureSource.WhenGestureRecognized -= OnMicrogestureRecognized;
                rightGestureSource.WhenGestureRecognized += OnMicrogestureRecognized;
            }

            if (leftGestureSource != null)
            {
                leftGestureSource.WhenGestureRecognized -= OnMicrogestureRecognized;
                leftGestureSource.WhenGestureRecognized += OnMicrogestureRecognized;
            }
        }

        private void UnsubscribeEvents()
        {
            if (rightGestureSource != null)
            {
                rightGestureSource.WhenGestureRecognized -= OnMicrogestureRecognized;
            }

            if (leftGestureSource != null)
            {
                leftGestureSource.WhenGestureRecognized -= OnMicrogestureRecognized;
            }
        }

        private void Update()
        {
            // Suspend in MR Passthrough (physical walking applies)
            if (_modeManager != null && _modeManager.IsPassthroughActive)
            {
                _currentVelocity = Vector3.zero;
                return;
            }

            // Keyboard simulation for Editor testing
            HandleKeyboardSimulation();

            // Apply movement physics
            if (_currentVelocity.sqrMagnitude > 0.0001f)
            {
                ApplyMovement(_currentVelocity * Time.deltaTime);
                _currentVelocity = Vector3.Lerp(_currentVelocity, Vector3.zero, Time.deltaTime * decelerationDrag);
            }

            UpdateHUD();
        }

        /// <summary>
        /// Native Meta XR Microgesture callback.
        /// </summary>
        public void OnMicrogestureRecognized(OVRHand.MicrogestureType gesture)
        {
            // Suspend in MR Passthrough
            if (_modeManager != null && _modeManager.IsPassthroughActive) return;

            if (_cameraRig == null)
            {
                _cameraRig = GetComponent<OVRCameraRig>();
                if (_cameraRig == null) _cameraRig = UnityEngine.Object.FindObjectOfType<OVRCameraRig>();
            }

            if (_headTransform == null && _cameraRig != null)
            {
                _headTransform = _cameraRig.centerEyeAnchor;
            }
            if (_headTransform == null)
            {
                var cam = Camera.main;
                if (cam != null) _headTransform = cam.transform;
            }
            if (_headTransform == null) return;

            Vector3 headFwd = _headTransform.forward;
            headFwd.y = 0f;
            headFwd.Normalize();

            switch (gesture)
            {
                case OVRHand.MicrogestureType.SwipeForward:
                    // Propel forward in gaze direction
                    _currentVelocity += headFwd * forwardSwipeImpulse;
                    if (_currentVelocity.magnitude > maxVelocity) _currentVelocity = _currentVelocity.normalized * maxVelocity;
                    PlaySound(_swipeClip, 1.0f);
                    ShowHUD("▲ Walk Forward");
                    Debug.Log("[MicrogestureLocomotion] Thumb Swipe Forward -> Gliding forward");
                    break;

                case OVRHand.MicrogestureType.SwipeBackward:
                    // Propel backward in gaze direction
                    _currentVelocity -= headFwd * backwardSwipeImpulse;
                    if (_currentVelocity.magnitude > maxVelocity) _currentVelocity = _currentVelocity.normalized * maxVelocity;
                    PlaySound(_swipeClip, 0.85f);
                    ShowHUD("▼ Back Up");
                    Debug.Log("[MicrogestureLocomotion] Thumb Swipe Backward -> Gliding backward");
                    break;

                case OVRHand.MicrogestureType.SwipeLeft:
                    // Snap-turn left
                    if (Time.unscaledTime - _lastTurnTime > turnCooldown)
                    {
                        SnapTurn(-snapTurnAngle);
                        _lastTurnTime = Time.unscaledTime;
                        PlaySound(_turnClip, 0.95f);
                        ShowHUD("◄ Turn 45°");
                        Debug.Log("[MicrogestureLocomotion] Thumb Swipe Left -> Snap Turn -45°");
                    }
                    break;

                case OVRHand.MicrogestureType.SwipeRight:
                    // Snap-turn right
                    if (Time.unscaledTime - _lastTurnTime > turnCooldown)
                    {
                        SnapTurn(snapTurnAngle);
                        _lastTurnTime = Time.unscaledTime;
                        PlaySound(_turnClip, 1.05f);
                        ShowHUD("Turn 45° ►");
                        Debug.Log("[MicrogestureLocomotion] Thumb Swipe Right -> Snap Turn +45°");
                    }
                    break;

                case OVRHand.MicrogestureType.ThumbTap:
                    // Comfort hop forward / instant halt
                    if (_currentVelocity.sqrMagnitude > 0.05f)
                    {
                        _currentVelocity = Vector3.zero; // Brake immediately
                        ShowHUD("■ Stop");
                    }
                    else
                    {
                        // Quick step forward
                        ApplyMovement(headFwd * 0.85f);
                        ShowHUD("● Step Forward");
                    }
                    PlaySound(_turnClip, 1.25f);
                    Debug.Log("[MicrogestureLocomotion] Thumb Tap");
                    break;
            }
        }

        private void ApplyMovement(Vector3 delta)
        {
            if (_cameraRig == null) return;

            Vector3 newPos = _cameraRig.transform.position + delta;
            newPos.x = Mathf.Clamp(newPos.x, roomBoundsX.x, roomBoundsX.y);
            newPos.z = Mathf.Clamp(newPos.z, roomBoundsZ.x, roomBoundsZ.y);
            _cameraRig.transform.position = newPos;
        }

        private void SnapTurn(float angle)
        {
            if (_cameraRig == null || _headTransform == null) return;
            Vector3 pivot = _headTransform.position;
            _cameraRig.transform.RotateAround(pivot, Vector3.up, angle);
        }

        private void HandleKeyboardSimulation()
        {
            if (Keyboard.current == null) return;

            if (Keyboard.current.upArrowKey.wasPressedThisFrame || Keyboard.current.wKey.wasPressedThisFrame)
            {
                OnMicrogestureRecognized(OVRHand.MicrogestureType.SwipeForward);
            }

            if (Keyboard.current.downArrowKey.wasPressedThisFrame || Keyboard.current.sKey.wasPressedThisFrame)
            {
                OnMicrogestureRecognized(OVRHand.MicrogestureType.SwipeBackward);
            }

            if (Keyboard.current.leftArrowKey.wasPressedThisFrame || Keyboard.current.aKey.wasPressedThisFrame || Keyboard.current.qKey.wasPressedThisFrame)
            {
                OnMicrogestureRecognized(OVRHand.MicrogestureType.SwipeLeft);
            }

            if (Keyboard.current.rightArrowKey.wasPressedThisFrame || Keyboard.current.dKey.wasPressedThisFrame || Keyboard.current.eKey.wasPressedThisFrame)
            {
                OnMicrogestureRecognized(OVRHand.MicrogestureType.SwipeRight);
            }

            if (Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                OnMicrogestureRecognized(OVRHand.MicrogestureType.ThumbTap);
            }
        }

        private void SetupAudio()
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                    audioSource.spatialBlend = 0.5f;
                    audioSource.playOnAwake = false;
                }
            }

            int rate = 44100;

            // 1. Swipe Sound: Smooth 35ms whoosh/click
            int samplesSwipe = Mathf.RoundToInt(rate * 0.035f);
            float[] dataSwipe = new float[samplesSwipe];
            for (int i = 0; i < samplesSwipe; i++)
            {
                float t = i / (float)rate;
                float env = Mathf.Sin(Mathf.PI * (i / (float)samplesSwipe));
                dataSwipe[i] = Mathf.Sin(2f * Mathf.PI * (450f + 350f * t) * t) * env * 0.45f;
            }
            _swipeClip = AudioClip.Create("Micro_Swipe", samplesSwipe, 1, rate, false);
            _swipeClip.SetData(dataSwipe, 0);

            // 2. Turn Sound: Crisp 25ms ratchet tick
            int samplesTurn = Mathf.RoundToInt(rate * 0.025f);
            float[] dataTurn = new float[samplesTurn];
            for (int i = 0; i < samplesTurn; i++)
            {
                float t = i / (float)rate;
                float env = Mathf.Exp(-95f * t);
                dataTurn[i] = Mathf.Sin(2f * Mathf.PI * 1650f * t) * env * 0.55f;
            }
            _turnClip = AudioClip.Create("Micro_Turn", samplesTurn, 1, rate, false);
            _turnClip.SetData(dataTurn, 0);
        }

        private void PlaySound(AudioClip clip, float pitch)
        {
            if (audioSource != null && clip != null)
            {
                audioSource.pitch = pitch;
                audioSource.PlayOneShot(clip, 0.7f);
            }
        }

        private void SetupHUD()
        {
            _hudObj = new GameObject("[Microgesture_HUD]");
            _hudObj.transform.SetParent(transform, false);

            _hudText = _hudObj.AddComponent<TextMesh>();
            _hudText.fontSize = 28;
            _hudText.characterSize = 0.0016f;
            _hudText.alignment = TextAlignment.Center;
            _hudText.anchor = TextAnchor.MiddleCenter;
            _hudText.color = new Color(0.95f, 0.85f, 0.55f, 0f); // Warm gold

            _hudObj.SetActive(false);
        }

        private void ShowHUD(string message)
        {
            if (_hudText == null) return;
            _hudText.text = message;
            _hudText.color = new Color(0.95f, 0.85f, 0.55f, 0.90f);
            _hudFadeTimer = 0.55f;
            _hudObj.SetActive(true);
        }

        private void UpdateHUD()
        {
            if (_hudObj == null || !_hudObj.activeSelf || _headTransform == null) return;

            // Float subtly in lower peripheral vision (0.65m forward, -0.22m down)
            Vector3 targetPos = _headTransform.position
                              + _headTransform.forward * 0.65f
                              - _headTransform.up * 0.22f;

            _hudObj.transform.position = targetPos;
            _hudObj.transform.rotation = Quaternion.LookRotation(_hudObj.transform.position - _headTransform.position);

            _hudFadeTimer -= Time.deltaTime;
            if (_hudFadeTimer <= 0f)
            {
                _hudObj.SetActive(false);
            }
            else
            {
                float alpha = Mathf.Clamp01(_hudFadeTimer / 0.55f);
                _hudText.color = new Color(0.95f, 0.85f, 0.55f, alpha * 0.90f);
            }
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
            if (_hudObj != null) Destroy(_hudObj);
        }
    }
}
