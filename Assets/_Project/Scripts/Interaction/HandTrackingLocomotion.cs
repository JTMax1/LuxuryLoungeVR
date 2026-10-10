using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace VRWatch.Interaction
{
    /// <summary>
    /// Implements the Meta Horizon Home "Air Joystick" pinch locomotion gesture.
    /// Pinching the index finger and thumb on the free hand creates a virtual floating joystick:
    /// - Displacing the pinched hand forward/backward moves the player smoothly through the space.
    /// - Displacing left/right strafes or snap-turns.
    /// - Releasing the pinch immediately halts movement.
    /// Features a procedural glowing holographic anchor ring and line vector for intuitive visual feedback.
    /// </summary>
    [DisallowMultipleComponent]
    public class HandTrackingLocomotion : MonoBehaviour
    {
        public enum TurnMode
        {
            SnapTurn,
            SmoothTurn,
            None
        }

        [Header("Hand Settings")]
        [Tooltip("Hand used for locomotion joystick. Right hand is recommended so left hand can wear the watch.")]
        [SerializeField] private bool useRightHand = true;

        [Tooltip("Pinch strength threshold (0.0 to 1.0) to engage movement.")]
        [Range(0.5f, 0.95f)]
        [SerializeField] private float pinchThreshold = 0.70f;

        [Header("Movement Dynamics")]
        [Tooltip("Maximum movement speed in meters per second.")]
        [SerializeField] private float maxMoveSpeed = 1.6f;

        [Tooltip("Displacement distance (meters) from pinch origin for full speed.")]
        [SerializeField] private float maxDisplacement = 0.15f;

        [Tooltip("Deadzone distance (meters) where no movement occurs.")]
        [SerializeField] private float deadzone = 0.025f;

        [Tooltip("Acceleration / deceleration damping speed.")]
        [SerializeField] private float accelerationDamping = 10f;

        [Header("Turning Settings")]
        [SerializeField] private TurnMode turnMode = TurnMode.SnapTurn;
        [SerializeField] private float snapTurnAngle = 45f;
        [SerializeField] private float snapTurnDisplacement = 0.08f;
        [SerializeField] private float snapTurnCooldown = 0.40f;
        [SerializeField] private float smoothTurnSpeed = 65f;

        [Header("Room Boundaries")]
        [Tooltip("Room clamp bounds to prevent walking through gallery walls.")]
        [SerializeField] private Vector2 roomBoundsX = new Vector2(-4.2f, 4.2f);
        [SerializeField] private Vector2 roomBoundsZ = new Vector2(-4.2f, 4.2f);

        [Header("Visual Feedback")]
        [Tooltip("Color of the holographic virtual joystick anchor.")]
        [SerializeField] private Color anchorColor = new Color(0.85f, 0.70f, 0.25f, 0.85f); // Luxury satin brass glow

        // References
        private OVRCameraRig _cameraRig;
        private OVRHand _targetHand;
        private Transform _headTransform;
        private LoungeModeManager _modeManager;

        // Locomotion State
        private bool _isPinching = false;
        private Vector3 _pinchOrigin;
        private Vector3 _currentVelocity = Vector3.zero;
        private float _lastSnapTurnTime = -1f;

        // Visual Joystick Feedback
        private GameObject _visualizerRoot;
        private LineRenderer _visualizerLine;
        private MeshRenderer _anchorRingRenderer;

        private void Awake()
        {
            _cameraRig = GetComponent<OVRCameraRig>();
            if (_cameraRig == null)
            {
                _cameraRig = UnityEngine.Object.FindObjectOfType<OVRCameraRig>();
            }

            _modeManager = UnityEngine.Object.FindObjectOfType<LoungeModeManager>();
            SetupVisualizer();
        }

        private void Start()
        {
            FindHandReferences();
        }

        private void FindHandReferences()
        {
            string handName = useRightHand ? "[BuildingBlock] Hand Tracking right" : "[BuildingBlock] Hand Tracking left";
            var handGo = GameObject.Find(handName);
            if (handGo != null)
            {
                _targetHand = handGo.GetComponent<OVRHand>();
            }

            if (_cameraRig != null && _cameraRig.centerEyeAnchor != null)
            {
                _headTransform = _cameraRig.centerEyeAnchor;
            }
            else
            {
                var mainCam = Camera.main;
                if (mainCam != null) _headTransform = mainCam.transform;
            }
        }

        private void Update()
        {
            // Disable virtual locomotion in MR Passthrough (physical walking is used)
            if (_modeManager != null && _modeManager.IsPassthroughActive)
            {
                StopLocomotion();
                return;
            }

            // Keyboard fallback for Editor testing (WASD + Q/E)
            HandleKeyboardTesting();

            if (_targetHand == null || _headTransform == null)
            {
                FindHandReferences();
                if (_targetHand == null || _headTransform == null) return;
            }

            // Check pinch state
            bool isPinchingNow = CheckIsPinching();
            Vector3 currentHandPos = GetHandInteractionPosition();

            if (isPinchingNow)
            {
                if (!_isPinching)
                {
                    // Pinch initiated
                    _isPinching = true;
                    _pinchOrigin = currentHandPos;
                    ShowVisualizer(true);
                }

                // Process movement & turning while pinched
                ProcessPinchLocomotion(currentHandPos);
                UpdateVisualizer(_pinchOrigin, currentHandPos);
            }
            else
            {
                if (_isPinching)
                {
                    // Pinch released
                    StopLocomotion();
                }
            }

            // Apply smooth deceleration if not actively moving
            if (!_isPinching && _currentVelocity.sqrMagnitude > 0.0001f)
            {
                _currentVelocity = Vector3.Lerp(_currentVelocity, Vector3.zero, Time.deltaTime * accelerationDamping);
                ApplyMovement(_currentVelocity * Time.deltaTime);
            }
        }

        private bool CheckIsPinching()
        {
            if (_targetHand == null || !_targetHand.IsTracked) return false;
            float strength = _targetHand.GetFingerPinchStrength(OVRHand.HandFinger.Index);
            return strength >= pinchThreshold || _targetHand.GetFingerIsPinching(OVRHand.HandFinger.Index);
        }

        private Vector3 GetHandInteractionPosition()
        {
            if (_targetHand.PointerPose != null)
            {
                return _targetHand.PointerPose.position;
            }
            return _targetHand.transform.position;
        }

        private void ProcessPinchLocomotion(Vector3 currentHandPos)
        {
            Vector3 totalDelta = currentHandPos - _pinchOrigin;

            // Project onto horizontal plane aligned with head gaze
            Vector3 headForward = _headTransform.forward;
            headForward.y = 0f;
            headForward.Normalize();

            Vector3 headRight = _headTransform.right;
            headRight.y = 0f;
            headRight.Normalize();

            float forwardDist = Vector3.Dot(totalDelta, headForward);
            float strafeDist = Vector3.Dot(totalDelta, headRight);

            // 1. Process Translation
            Vector2 displacement2D = new Vector2(strafeDist, forwardDist);
            float magnitude = displacement2D.magnitude;

            Vector3 targetVelocity = Vector3.zero;

            if (magnitude > deadzone)
            {
                float activeRange = maxDisplacement - deadzone;
                float normalizedSpeed = Mathf.Clamp01((magnitude - deadzone) / activeRange);
                Vector2 direction = displacement2D.normalized;

                Vector3 worldDir = (headForward * direction.y + headRight * direction.x).normalized;
                targetVelocity = worldDir * (normalizedSpeed * maxMoveSpeed);
            }

            _currentVelocity = Vector3.Lerp(_currentVelocity, targetVelocity, Time.deltaTime * accelerationDamping);
            ApplyMovement(_currentVelocity * Time.deltaTime);

            // 2. Process Turning
            if (turnMode == TurnMode.SnapTurn)
            {
                if (Mathf.Abs(strafeDist) > snapTurnDisplacement && Time.time - _lastSnapTurnTime > snapTurnCooldown)
                {
                    float turnAngle = Mathf.Sign(strafeDist) * snapTurnAngle;
                    SnapTurn(turnAngle);
                    _lastSnapTurnTime = Time.time;
                }
            }
            else if (turnMode == TurnMode.SmoothTurn)
            {
                if (Mathf.Abs(strafeDist) > deadzone)
                {
                    float turnFactor = Mathf.Clamp((strafeDist - Mathf.Sign(strafeDist) * deadzone) / (maxDisplacement - deadzone), -1f, 1f);
                    SmoothTurn(turnFactor * smoothTurnSpeed * Time.deltaTime);
                }
            }
        }

        private void ApplyMovement(Vector3 delta)
        {
            if (_cameraRig == null) return;

            Vector3 newPos = _cameraRig.transform.position + delta;

            // Clamp inside room enclosure bounds
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

        private void SmoothTurn(float angleDelta)
        {
            if (_cameraRig == null || _headTransform == null) return;
            Vector3 pivot = _headTransform.position;
            _cameraRig.transform.RotateAround(pivot, Vector3.up, angleDelta);
        }

        private void StopLocomotion()
        {
            _isPinching = false;
            ShowVisualizer(false);
        }

        private void HandleKeyboardTesting()
        {
            if (Keyboard.current == null) return;

            Vector3 move = Vector3.zero;
            if (Keyboard.current.wKey.isPressed) move += _headTransform.forward;
            if (Keyboard.current.sKey.isPressed) move -= _headTransform.forward;
            if (Keyboard.current.dKey.isPressed) move += _headTransform.right;
            if (Keyboard.current.aKey.isPressed) move -= _headTransform.right;

            move.y = 0f;
            if (move.sqrMagnitude > 0.01f)
            {
                ApplyMovement(move.normalized * maxMoveSpeed * Time.deltaTime);
            }

            if (Keyboard.current.qKey.wasPressedThisFrame) SnapTurn(-45f);
            if (Keyboard.current.eKey.wasPressedThisFrame) SnapTurn(45f);
        }

        // --- Holographic Visualizer Setup ---

        private void SetupVisualizer()
        {
            _visualizerRoot = new GameObject("[AirJoystick_Visualizer]");
            _visualizerRoot.transform.SetParent(transform, false);

            // 1. Anchor Disc Ring
            var ringObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ringObj.name = "AnchorRing";
            ringObj.transform.SetParent(_visualizerRoot.transform, false);
            ringObj.transform.localScale = new Vector3(0.045f, 0.0015f, 0.045f);

            var col = ringObj.GetComponent<Collider>();
            if (col != null) Destroy(col);

            _anchorRingRenderer = ringObj.GetComponent<MeshRenderer>();
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            var mat = new Material(shader);
            mat.color = anchorColor;
            _anchorRingRenderer.material = mat;

            // 2. Vector Line
            _visualizerLine = _visualizerRoot.AddComponent<LineRenderer>();
            _visualizerLine.useWorldSpace = true;
            _visualizerLine.startWidth = 0.004f;
            _visualizerLine.endWidth = 0.002f;
            _visualizerLine.material = mat;
            _visualizerLine.startColor = anchorColor;
            _visualizerLine.endColor = new Color(anchorColor.r, anchorColor.g, anchorColor.b, 0.2f);
            _visualizerLine.positionCount = 2;

            ShowVisualizer(false);
        }

        private void ShowVisualizer(bool visible)
        {
            if (_visualizerRoot != null)
            {
                _visualizerRoot.SetActive(visible);
            }
        }

        private void UpdateVisualizer(Vector3 origin, Vector3 current)
        {
            if (_visualizerRoot == null || !_visualizerRoot.activeSelf) return;

            // Place ring at pinch origin facing camera
            _anchorRingRenderer.transform.position = origin;
            _anchorRingRenderer.transform.rotation = Quaternion.Euler(0f, _headTransform.eulerAngles.y, 0f);

            // Connect vector line from origin to hand
            _visualizerLine.SetPosition(0, origin);
            _visualizerLine.SetPosition(1, current);
        }

        private void OnDestroy()
        {
            if (_visualizerRoot != null)
            {
                Destroy(_visualizerRoot);
            }
        }
    }
}
