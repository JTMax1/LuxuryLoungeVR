using System.Collections.Generic;
using UnityEngine;

namespace VRWatch.Interaction
{
    /// <summary>
    /// Perfectly positions and aligns the wristwatch on the tracked hand's physical wrist joint.
    /// Seamlessly tracks OVRSkeleton's Hand_WristRoot bone when hand tracking is active,
    /// or falls back to LeftHandAnchor with calibrated dorsal wrist offsets.
    /// Eliminates unnatural detached arm cylinders and ensures the watch face is always right-side up.
    /// </summary>
    [DisallowMultipleComponent]
    public class WatchWristAligner : MonoBehaviour
    {
        [Header("Wrist Offsets (Meters)")]
        [Tooltip("Offset above dorsal skin surface.")]
        [SerializeField] private float dorsalOffset = 0.024f;

        [Tooltip("Offset back along forearm from wrist joint.")]
        [SerializeField] private float forearmOffset = -0.035f;

        [Tooltip("Lateral offset across wrist.")]
        [SerializeField] private float lateralOffset = 0.000f;

        [Header("Rotation Calibration")]
        [SerializeField] private Vector3 additionalEulerRotation = Vector3.zero;

        private OVRSkeleton _leftSkeleton;
        private Transform _wristBoneTransform;

        private void Start()
        {
            FindLeftSkeleton();
            ApplyFallbackAnchor();
        }

        private void FindLeftSkeleton()
        {
            var leftHandObj = GameObject.Find("[BuildingBlock] Hand Tracking left");
            if (leftHandObj != null)
            {
                _leftSkeleton = leftHandObj.GetComponent<OVRSkeleton>();
            }
        }

        private void ApplyFallbackAnchor()
        {
            var leftAnchor = GameObject.Find("LeftHandAnchor");
            if (leftAnchor != null && transform.parent != leftAnchor.transform)
            {
                transform.SetParent(leftAnchor.transform, false);
            }

            transform.localPosition = new Vector3(lateralOffset, dorsalOffset, forearmOffset);
            transform.localRotation = Quaternion.Euler(additionalEulerRotation);
        }

        private void LateUpdate()
        {
            if (_leftSkeleton == null)
            {
                FindLeftSkeleton();
            }

            if (_leftSkeleton != null && _leftSkeleton.IsInitialized)
            {
                if (_wristBoneTransform == null)
                {
                    _wristBoneTransform = FindWristBone(_leftSkeleton);
                }

                if (_wristBoneTransform != null)
                {
                    // Snap directly to the actual tracked physical wrist bone
                    Vector3 targetPos = _wristBoneTransform.position
                                      + _wristBoneTransform.up * dorsalOffset
                                      - _wristBoneTransform.forward * 0.015f
                                      + _wristBoneTransform.right * lateralOffset;

                    transform.position = targetPos;
                    transform.rotation = _wristBoneTransform.rotation * Quaternion.Euler(additionalEulerRotation);
                    return;
                }
            }

            // Fallback if skeleton is not active
            ApplyFallbackAnchor();
        }

        private Transform FindWristBone(OVRSkeleton skeleton)
        {
            if (skeleton.Bones == null) return null;

            foreach (var b in skeleton.Bones)
            {
                if (b.Id == OVRSkeleton.BoneId.Hand_WristRoot)
                {
                    return b.Transform;
                }
            }

            return null;
        }
    }
}
