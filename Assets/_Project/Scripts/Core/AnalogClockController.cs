using System;
using UnityEngine;

namespace VRWatch.Core
{
    /// <summary>
    /// Drives analog watch hands (hour, minute, second) based on real local system time.
    /// Supports discrete quartz ticking or continuous high-frequency sweeping.
    /// </summary>
    [SelectionBase]
    [DisallowMultipleComponent]
    public class AnalogClockController : MonoBehaviour
    {
        [Header("Hand Pivot References")]
        [Tooltip("Transform pivot for the hour hand. Must rotate around rotationAxis.")]
        [SerializeField] private Transform hourHand;

        [Tooltip("Transform pivot for the minute hand. Must rotate around rotationAxis.")]
        [SerializeField] private Transform minuteHand;

        [Tooltip("Transform pivot for the second hand. Must rotate around rotationAxis.")]
        [SerializeField] private Transform secondHand;

        [Header("Clock Mechanics")]
        [Tooltip("If true, the second hand glides continuously. If false, it steps once per second like a quartz movement.")]
        [SerializeField] private bool smoothSecondHand = true;

        [Tooltip("Local axis around which hands rotate. Default is Vector3.up (0, 1, 0) for horizontal dial faces.")]
        [SerializeField] private Vector3 rotationAxis = Vector3.up;

        [Tooltip("Optional Euler offset in degrees added to the calculated time rotation.")]
        [SerializeField] private float dialRotationOffset = 0f;

        public bool SmoothSecondHand
        {
            get => smoothSecondHand;
            set => smoothSecondHand = value;
        }

        public Transform HourHand => hourHand;
        public Transform MinuteHand => minuteHand;
        public Transform SecondHand => secondHand;

        public void SetHands(Transform hour, Transform minute, Transform second)
        {
            hourHand = hour;
            minuteHand = minute;
            secondHand = second;
        }

        private void Awake()
        {
            ValidateReferences();
        }

        private void OnValidate()
        {
            if (rotationAxis == Vector3.zero)
            {
                rotationAxis = Vector3.up;
            }
        }

        private void Update()
        {
            DateTime now = DateTime.Now;

            float secondFraction = smoothSecondHand ? (now.Millisecond / 1000f) : 0f;
            float totalSeconds = now.Second + secondFraction;

            float totalMinutes = now.Minute + (totalSeconds / 60f);
            float hour12 = now.Hour % 12;
            float totalHours = hour12 + (totalMinutes / 60f);

            // Standard clock angles (360 degrees per cycle):
            // Seconds: 6 degrees per second
            // Minutes: 6 degrees per minute
            // Hours: 30 degrees per hour (360 / 12)
            float secondAngle = (totalSeconds * 6f) + dialRotationOffset;
            float minuteAngle = (totalMinutes * 6f) + dialRotationOffset;
            float hourAngle = (totalHours * 30f) + dialRotationOffset;

            Vector3 normalizedAxis = rotationAxis.normalized;

            if (secondHand != null)
            {
                secondHand.localRotation = Quaternion.AngleAxis(secondAngle, normalizedAxis);
            }

            if (minuteHand != null)
            {
                minuteHand.localRotation = Quaternion.AngleAxis(minuteAngle, normalizedAxis);
            }

            if (hourHand != null)
            {
                hourHand.localRotation = Quaternion.AngleAxis(hourAngle, normalizedAxis);
            }
        }

        private void ValidateReferences()
        {
            if (hourHand == null)
            {
                Debug.LogWarning($"[{nameof(AnalogClockController)}] Hour hand reference is not assigned on {gameObject.name}.", this);
            }

            if (minuteHand == null)
            {
                Debug.LogWarning($"[{nameof(AnalogClockController)}] Minute hand reference is not assigned on {gameObject.name}.", this);
            }

            if (secondHand == null)
            {
                Debug.LogWarning($"[{nameof(AnalogClockController)}] Second hand reference is not assigned on {gameObject.name}.", this);
            }
        }
    }
}
