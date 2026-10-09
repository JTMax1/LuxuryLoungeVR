using UnityEngine;

namespace VRWatch.Interaction
{
    /// <summary>
    /// Attaches and aligns a virtual watch to a target wrist transform (such as Meta XR synthetic wrist joint,
    /// tracked hand joint, or controller anchor) with adjustable local position and rotation offsets.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(100)] // Runs after tracking updates
    public class WristAnchorFollower : MonoBehaviour
    {
        [Header("Target Anchor")]
        [Tooltip("The tracked wrist joint or anchor transform to follow.")]
        [SerializeField] private Transform targetWristBone;

        [Tooltip("If true, automatically search for the left wrist anchor or hand root if targetWristBone is not assigned.")]
        [SerializeField] private bool autoFindLeftWrist = true;

        [Header("Offsets")]
        [Tooltip("Position offset relative to target wrist bone orientation (in meters).")]
        [SerializeField] private Vector3 offsetPosition = new Vector3(0f, 0.02f, -0.05f);

        [Tooltip("Euler rotation offset relative to target wrist bone orientation (in degrees).")]
        [SerializeField] private Vector3 offsetRotation = Vector3.zero;

        [Header("Follow Settings")]
        [Tooltip("Whether to actively follow the target transform in LateUpdate.")]
        [SerializeField] private bool followActive = true;

        public Transform TargetWristBone
        {
            get => targetWristBone;
            set => targetWristBone = value;
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

        public bool FollowActive
        {
            get => followActive;
            set => followActive = value;
        }

        private void Start()
        {
            if (targetWristBone == null && autoFindLeftWrist)
            {
                TryFindLeftWristAnchor();
            }
        }

        private void LateUpdate()
        {
            if (!followActive || targetWristBone == null)
            {
                return;
            }

            // Calculate target position and rotation in world space relative to the wrist bone
            Quaternion targetRot = targetWristBone.rotation * Quaternion.Euler(offsetRotation);
            Vector3 targetPos = targetWristBone.TransformPoint(offsetPosition);

            transform.position = targetPos;
            transform.rotation = targetRot;
        }

        /// <summary>
        /// Attempts to locate standard Meta XR left wrist transforms in the active hierarchy.
        /// </summary>
        public void TryFindLeftWristAnchor()
        {
            // Candidate names commonly used in Meta XR Core SDK and Interaction SDK rigs
            string[] candidateNames = new string[]
            {
                "Hand_WristRoot",
                "LeftHandAnchor",
                "LeftControllerAnchor",
                "LeftHand",
                "LeftHandOnController"
            };

            foreach (string candidate in candidateNames)
            {
                GameObject foundObj = GameObject.Find(candidate);
                if (foundObj != null)
                {
                    targetWristBone = foundObj.transform;
                    Debug.Log($"[{nameof(WristAnchorFollower)}] Auto-bound wrist anchor to '{foundObj.name}'.", this);
                    return;
                }
            }

            // Also search child transforms under OVRCameraRig if present
            var cameraRigs = FindObjectsByType<Component>();
            foreach (var comp in cameraRigs)
            {
                if (comp.GetType().Name.Contains("OVRCameraRig"))
                {
                    Transform leftAnchor = comp.transform.Find("TrackingSpace/LeftHandAnchor");
                    if (leftAnchor != null)
                    {
                        targetWristBone = leftAnchor;
                        Debug.Log($"[{nameof(WristAnchorFollower)}] Auto-bound wrist anchor to LeftHandAnchor in {comp.name}.", this);
                        return;
                    }
                }
            }
        }
    }
}
