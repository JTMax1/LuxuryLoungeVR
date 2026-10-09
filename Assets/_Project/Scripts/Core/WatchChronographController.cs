using UnityEngine;
using UnityEngine.InputSystem;

namespace VRWatch.Core
{
    /// <summary>
    /// Controls mechanical chronograph stopwatch complications.
    /// Operates the 2 o'clock Start/Stop pusher, the 4 o'clock Reset pusher,
    /// and drives the central sweeping chronograph seconds hand with flyback snap reset.
    /// </summary>
    [DisallowMultipleComponent]
    public class WatchChronographController : MonoBehaviour
    {
        public enum ChronoState
        {
            Stopped,
            Running
        }

        [Header("Hand & Pusher References")]
        [Tooltip("Transform pivot for the central sweeping chronograph seconds hand.")]
        [SerializeField] private Transform chronoSecondHand;

        [Tooltip("Pusher cylinder at 2 o'clock used to Start and Stop the chronograph.")]
        [SerializeField] private Transform pusherStartStop;

        [Tooltip("Pusher cylinder at 4 o'clock used to Reset the chronograph to zero.")]
        [SerializeField] private Transform pusherReset;

        [Header("Audio Feedback")]
        [SerializeField] private WatchAudioController audioController;

        [Header("Rotation Settings")]
        [SerializeField] private Vector3 rotationAxis = Vector3.up;

        private ChronoState _state = ChronoState.Stopped;
        private float _elapsedSeconds = 0f;
        private Vector3 _startStopRestPos;
        private Vector3 _resetRestPos;
        private float _startStopPressTime = -1f;
        private float _resetPressTime = -1f;

        public ChronoState CurrentState => _state;
        public float ElapsedSeconds => _elapsedSeconds;
        public Transform ChronoSecondHand => chronoSecondHand;
        public Transform PusherStartStop => pusherStartStop;
        public Transform PusherReset => pusherReset;

        public void SetReferences(Transform chronoHand, Transform pusherStart, Transform pusherRst, WatchAudioController audioCtrl)
        {
            chronoSecondHand = chronoHand;
            pusherStartStop = pusherStart;
            pusherReset = pusherRst;
            audioController = audioCtrl;

            if (pusherStartStop != null) _startStopRestPos = pusherStartStop.localPosition;
            if (pusherReset != null) _resetRestPos = pusherReset.localPosition;
        }

        private void Awake()
        {
            if (pusherStartStop != null) _startStopRestPos = pusherStartStop.localPosition;
            if (pusherReset != null) _resetRestPos = pusherReset.localPosition;
            if (audioController == null) audioController = GetComponent<WatchAudioController>();
        }

        private void Update()
        {
            HandleEditorInput();

            if (_state == ChronoState.Running)
            {
                _elapsedSeconds += Time.deltaTime;
                UpdateHandRotation();
            }

            AnimatePushers();
        }

        /// <summary>
        /// Toggles between Running and Stopped state (2 o'clock pusher).
        /// </summary>
        public void ToggleStartStop()
        {
            if (_state == ChronoState.Stopped)
            {
                _state = ChronoState.Running;
                TriggerPusherAnimation(ref _startStopPressTime);
                if (audioController != null) audioController.PlayPusherClick(1.05f);
                Debug.Log("[Chronograph] Started stopwatch.");
            }
            else
            {
                _state = ChronoState.Stopped;
                TriggerPusherAnimation(ref _startStopPressTime);
                if (audioController != null) audioController.PlayPusherClick(0.95f);
                Debug.Log($"[Chronograph] Stopped stopwatch at {_elapsedSeconds:F2} seconds.");
            }
        }

        /// <summary>
        /// Resets the chronograph hand back to 12 o'clock with flyback snap (4 o'clock pusher).
        /// </summary>
        public void ResetChronograph()
        {
            TriggerPusherAnimation(ref _resetPressTime);

            if (audioController != null) audioController.PlayPusherClick(1.20f);

            _elapsedSeconds = 0f;
            _state = ChronoState.Stopped;

            if (chronoSecondHand != null)
            {
                chronoSecondHand.localRotation = Quaternion.identity;
            }

            Debug.Log("[Chronograph] Reset to 00:00.00.");
        }

        private void UpdateHandRotation()
        {
            if (chronoSecondHand == null) return;

            // 6 degrees per second (360 degrees per 60 seconds)
            float angle = (_elapsedSeconds * 6f) % 360f;
            chronoSecondHand.localRotation = Quaternion.AngleAxis(angle, rotationAxis.normalized);
        }

        private void TriggerPusherAnimation(ref float pressTime)
        {
            pressTime = Time.time;
        }

        private void AnimatePushers()
        {
            float pressDuration = 0.12f;
            float depressDistance = 0.0012f; // 1.2mm mechanical depression

            if (pusherStartStop != null && _startStopPressTime > 0f)
            {
                float elapsed = Time.time - _startStopPressTime;
                if (elapsed < pressDuration)
                {
                    float factor = Mathf.Sin((elapsed / pressDuration) * Mathf.PI);
                    Vector3 inward = -pusherStartStop.localPosition.normalized * depressDistance;
                    pusherStartStop.localPosition = _startStopRestPos + (inward * factor);
                }
                else
                {
                    pusherStartStop.localPosition = _startStopRestPos;
                    _startStopPressTime = -1f;
                }
            }

            if (pusherReset != null && _resetPressTime > 0f)
            {
                float elapsed = Time.time - _resetPressTime;
                if (elapsed < pressDuration)
                {
                    float factor = Mathf.Sin((elapsed / pressDuration) * Mathf.PI);
                    Vector3 inward = -pusherReset.localPosition.normalized * depressDistance;
                    pusherReset.localPosition = _resetRestPos + (inward * factor);
                }
                else
                {
                    pusherReset.localPosition = _resetRestPos;
                    _resetPressTime = -1f;
                }
            }
        }

        private void HandleEditorInput()
        {
            // Keyboard shortcuts: 'C' = Chrono Start/Stop, 'R' = Chrono Reset
            if (Keyboard.current != null)
            {
                if (Keyboard.current.cKey.wasPressedThisFrame) ToggleStartStop();
                if (Keyboard.current.rKey.wasPressedThisFrame) ResetChronograph();
            }
        }
    }
}
