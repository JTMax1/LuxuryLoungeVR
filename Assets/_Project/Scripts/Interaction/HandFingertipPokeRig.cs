using System.Collections.Generic;
using UnityEngine;

namespace VRWatch.Interaction
{
    /// <summary>
    /// Tracks the player's physical index fingertips and controllers at runtime,
    /// ensuring trigger colliders are always present on both hands for touch interactions.
    /// Exposes global fingertip transforms for direct proximity detection.
    /// </summary>
    [DisallowMultipleComponent]
    public class HandFingertipPokeRig : MonoBehaviour
    {
        public static Transform LeftIndexTip { get; private set; }
        public static Transform RightIndexTip { get; private set; }
        public static Transform LeftControllerTip { get; private set; }
        public static Transform RightControllerTip { get; private set; }

        [Header("Fingertip Settings")]
        [SerializeField] private float fingertipRadius = 0.018f;

        private OVRSkeleton _leftSkeleton;
        private OVRSkeleton _rightSkeleton;
        private SphereCollider _leftCollider;
        private SphereCollider _rightCollider;

        private void Start()
        {
            FindSkeletons();
            FindControllers();
        }

        private void Update()
        {
            EnsureFingertipColliders();
        }

        private void FindSkeletons()
        {
            var leftHand = GameObject.Find("[BuildingBlock] Hand Tracking left");
            if (leftHand != null) _leftSkeleton = leftHand.GetComponent<OVRSkeleton>();

            var rightHand = GameObject.Find("[BuildingBlock] Hand Tracking right");
            if (rightHand != null) _rightSkeleton = rightHand.GetComponent<OVRSkeleton>();
        }

        private void FindControllers()
        {
            var leftCtrl = GameObject.Find("LeftControllerAnchor");
            if (leftCtrl != null)
            {
                LeftControllerTip = leftCtrl.transform;
                EnsureControllerCollider(leftCtrl, "LeftCtrl_PokeTrigger");
            }

            var rightCtrl = GameObject.Find("RightControllerAnchor");
            if (rightCtrl != null)
            {
                RightControllerTip = rightCtrl.transform;
                EnsureControllerCollider(rightCtrl, "RightCtrl_PokeTrigger");
            }
        }

        private void EnsureControllerCollider(GameObject ctrlGo, string name)
        {
            Transform existing = ctrlGo.transform.Find(name);
            if (existing == null)
            {
                var tipObj = new GameObject(name);
                tipObj.transform.SetParent(ctrlGo.transform, false);
                tipObj.transform.localPosition = new Vector3(0f, 0f, 0.05f); // Forward tip of controller
                var sc = tipObj.AddComponent<SphereCollider>();
                sc.isTrigger = true;
                sc.radius = fingertipRadius;
                var rb = tipObj.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                rb.useGravity = false;
            }
        }

        private void EnsureFingertipColliders()
        {
            if (_leftSkeleton == null || _rightSkeleton == null)
            {
                FindSkeletons();
            }

            // Left Hand Fingertip
            if (_leftSkeleton != null && _leftSkeleton.IsInitialized && LeftIndexTip == null)
            {
                Transform tipBone = FindIndexTipBone(_leftSkeleton);
                if (tipBone != null)
                {
                    LeftIndexTip = tipBone;
                    _leftCollider = AttachFingertipTrigger(tipBone.gameObject, "Left_Fingertip_PokeCollider");
                }
            }

            // Right Hand Fingertip
            if (_rightSkeleton != null && _rightSkeleton.IsInitialized && RightIndexTip == null)
            {
                Transform tipBone = FindIndexTipBone(_rightSkeleton);
                if (tipBone != null)
                {
                    RightIndexTip = tipBone;
                    _rightCollider = AttachFingertipTrigger(tipBone.gameObject, "Right_Fingertip_PokeCollider");
                }
            }
        }

        private Transform FindIndexTipBone(OVRSkeleton skeleton)
        {
            if (skeleton.Bones == null) return null;

            foreach (var b in skeleton.Bones)
            {
                if (b.Id == OVRSkeleton.BoneId.Hand_IndexTip)
                {
                    return b.Transform;
                }
            }

            // Fallback: search by name in children
            return FindChildRecursive(skeleton.transform, "index_tip") ??
                   FindChildRecursive(skeleton.transform, "b_index_null") ??
                   FindChildRecursive(skeleton.transform, "IndexTip");
        }

        private SphereCollider AttachFingertipTrigger(GameObject boneObj, string colliderName)
        {
            Transform existing = boneObj.transform.Find(colliderName);
            if (existing != null) return existing.GetComponent<SphereCollider>();

            GameObject colObj = new GameObject(colliderName);
            colObj.transform.SetParent(boneObj.transform, false);
            colObj.transform.localPosition = Vector3.zero;
            colObj.transform.localRotation = Quaternion.identity;

            var sc = colObj.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.radius = fingertipRadius;

            var rb = colObj.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            return sc;
        }

        private Transform FindChildRecursive(Transform parent, string substr)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                if (child.name.ToLower().Contains(substr.ToLower())) return child;
                var found = FindChildRecursive(child, substr);
                if (found != null) return found;
            }
            return null;
        }
    }
}
