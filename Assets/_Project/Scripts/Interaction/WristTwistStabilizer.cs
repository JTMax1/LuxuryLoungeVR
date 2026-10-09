using System;
using UnityEngine;

namespace VRWatch.Interaction
{
    /// <summary>
    /// Stabilizes virtual watch attachment against rapid wrist pronation/supination (forearm twisting).
    /// Uses Swing-Twist quaternion decomposition to isolate and dampen the roll rotation along the
    /// forearm axis, preventing the watch and strap from unnaturally clipping into forearm flesh.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(100)] // Execute after tracking pose updates
    public class WristTwistStabilizer : MonoBehaviour
    {
        [Header("Target Tracking References")]
        [Tooltip("Tracked wrist joint (e.g. LeftHandAnchor, Hand_WristRoot, or Synthetic Wrist Bone).")]
        [SerializeField] private Transform targetWristJoint;

        [Tooltip("Optional elbow or forearm reference transform used to compute the physical forearm vector.")]
        [SerializeField] private Transform elbowOrForearmAnchor;

        [Tooltip("Automatically search for standard Meta XR left wrist and elbow transforms if unassigned.")]
        [SerializeField] private bool autoFindReferences = true;

        [Header("Twist & Pronation Control")]
        [Tooltip("How much of the hand roll (pronation/supination) transfers to the watch band. 0 = purely swing, 1 = 100% hand roll. Default 0.45 matches real-world skin/fascia lag.")]
        [Range(0f, 1f)]
        [SerializeField] private float twistInfluence = 0.45f;

        [Tooltip("Local axis of the target wrist joint representing the longitudinal forearm direction (typically forward).")]
        [SerializeField] private Vector3 localTwistAxis = Vector3.forward;

        [Header("Watch Placement Offsets")]
        [Tooltip("Position offset relative to the stabilized wrist orientation (in meters).")]
        [SerializeField] private Vector3 offsetPosition = new Vector3(0f, 0.025f, -0.04f);

        [Tooltip("Euler rotation offset to orient the watch dial comfortably on the wrist.")]
        [SerializeField] private Vector3 offsetRotation = new Vector3(0f, -90f, 0f);

        [Header("Smoothing & Execution")]
        [Tooltip("If true, follows the target actively in LateUpdate.")]
        [SerializeField] private bool activeFollow = true;

        [Tooltip("Slerp smoothing factor for rotational filtering. 0 = instant tracking.")]
        [Range(0f, 60f)]
        [SerializeField] private float smoothingSpeed = 35f;

        private Quaternion _currentStabilizedRotation;
        private bool _isInitialized;

        public Transform TargetWristJoint
        {
            get => targetWristJoint;
            set => targetWristJoint = value;
        }

        public Transform ElbowOrForearmAnchor
        {
            get => elbowOrForearmAnchor;
            set => elbowOrForearmAnchor = value;
        }

        public float TwistInfluence
        {
            get => twistInfluence;
            set => twistInfluence = Mathf.Clamp01(value);
        }

        public Vector3 OffsetPosition
        {
            get => offsetPosition;
            set => offsetPosition = value;
        }

        public Vector3 OffsetRotation
        {
            get => offsetRotation;
            set => offsetRotation = value;
        }

        public bool ActiveFollow
        {
            get => activeFollow;
            set => activeFollow = value;
        }

        private void Start()
        {
            if (targetWristJoint == null && autoFindReferences)
            {
                TryFindReferences();
            }

            if (targetWristJoint != null)
            {
                _currentStabilizedRotation = ComputeStabilizedRotation(targetWristJoint.rotation);
                _isInitialized = true;
            }
        }

        private void LateUpdate()
        {
            if (!activeFollow || targetWristJoint == null)
            {
                return;
            }

            // 1. Position follows 1:1 anchored to the wrist with offset
            Quaternion stabilizedWristRot = ComputeStabilizedRotation(targetWristJoint.rotation);

            if (!_isInitialized)
            {
                _currentStabilizedRotation = stabilizedWristRot;
                _isInitialized = true;
            }
            else if (smoothingSpeed > 0f)
            {
                float t = Mathf.Clamp01(Time.deltaTime * smoothingSpeed);
                _currentStabilizedRotation = Quaternion.Slerp(_currentStabilizedRotation, stabilizedWristRot, t);
            }
            else
            {
                _currentStabilizedRotation = stabilizedWristRot;
            }

            // 2. Final watch orientation with user offset
            Quaternion finalRotation = _currentStabilizedRotation * Quaternion.Euler(offsetRotation);
            Vector3 finalPosition = targetWristJoint.position + (finalRotation * offsetPosition);

            transform.position = finalPosition;
            transform.rotation = finalRotation;
        }

        /// <summary>
        /// Decomposes raw wrist rotation into swing (bending/flexion) and twist (axial roll),
        /// scaling the twist component by twistInfluence.
        /// </summary>
        public Quaternion ComputeStabilizedRotation(Quaternion rawRotation)
        {
            // Determine world-space forearm twist axis
            Vector3 worldTwistAxis;
            if (elbowOrForearmAnchor != null)
            {
                worldTwistAxis = (targetWristJoint.position - elbowOrForearmAnchor.position).normalized;
                if (worldTwistAxis.sqrMagnitude < 0.001f)
                {
                    worldTwistAxis = targetWristJoint.TransformDirection(localTwistAxis);
                }
            }
            else
            {
                worldTwistAxis = targetWristJoint.TransformDirection(localTwistAxis);
            }

            DecomposeSwingTwist(rawRotation, worldTwistAxis, out Quaternion swing, out Quaternion twist);

            // Interpolate twist from identity (0% roll) to raw twist (100% roll)
            Quaternion dampenedTwist = Quaternion.Slerp(Quaternion.identity, twist, twistInfluence);

            // Recombine swing and dampened twist
            return swing * dampenedTwist;
        }

        /// <summary>
        /// Mathematically decomposes an input rotation Q into Swing and Twist around a specified twist axis.
        /// Q = Swing * Twist, where Twist rotates solely around the twist axis.
        /// </summary>
        public static void DecomposeSwingTwist(Quaternion rotation, Vector3 twistAxis, out Quaternion swing, out Quaternion twist)
        {
            Vector3 axis = twistAxis.normalized;
            Vector3 vectorPart = new Vector3(rotation.x, rotation.y, rotation.z);
            Vector3 projection = Vector3.Project(vectorPart, axis);

            twist = new Quaternion(projection.x, projection.y, projection.z, rotation.w);
            float sqrMag = (twist.x * twist.x) + (twist.y * twist.y) + (twist.z * twist.z) + (twist.w * twist.w);

            if (sqrMag > 1e-6f)
            {
                float invLength = 1f / Mathf.Sqrt(sqrMag);
                twist.x *= invLength;
                twist.y *= invLength;
                twist.z *= invLength;
                twist.w *= invLength;
            }
            else
            {
                twist = Quaternion.identity;
            }

            swing = rotation * Quaternion.Inverse(twist);
        }

        /// <summary>
        /// Attempts to locate Meta XR tracked wrist joint in the active hierarchy.
        /// </summary>
        public void TryFindReferences()
        {
            string[] candidateWristNames = new string[]
            {
                "LeftHandAnchor",
                "Hand_WristRoot",
                "LeftHandOnControllerAnchor",
                "LeftControllerAnchor",
                "LeftHand"
            };

            foreach (string candidate in candidateWristNames)
            {
                GameObject obj = GameObject.Find(candidate);
                if (obj != null)
                {
                    targetWristJoint = obj.transform;
                    Debug.Log($"[{nameof(WristTwistStabilizer)}] Resolved wrist joint to '{obj.name}'.", this);
                    break;
                }
            }

            // Optional search for forearm / elbow bone
            string[] candidateElbowNames = new string[]
            {
                "LeftForearmAnchor",
                "LeftArmLower",
                "Hand_ForearmStub"
            };

            foreach (string candidate in candidateElbowNames)
            {
                GameObject obj = GameObject.Find(candidate);
                if (obj != null)
                {
                    elbowOrForearmAnchor = obj.transform;
                    break;
                }
            }
        }
    }
}
