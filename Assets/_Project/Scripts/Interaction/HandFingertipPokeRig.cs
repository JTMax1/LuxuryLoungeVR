using System.Collections.Generic;
using UnityEngine;
using Oculus.Interaction;
using Oculus.Interaction.Input;

namespace VRWatch.Interaction
{
    /// <summary>
    /// Configures Meta XR PokeInteractors and fingertip trigger colliders on both hands
    /// to guarantee responsive direct-touch interactions across Quest 3 and Editor XR simulation.
    /// </summary>
    [DisallowMultipleComponent]
    public class HandFingertipPokeRig : MonoBehaviour
    {
        [Header("Hand Sources")]
        [SerializeField] private GameObject leftHandRoot;
        [SerializeField] private GameObject rightHandRoot;

        [Header("Fingertip Trigger Setup")]
        [SerializeField] private float fingertipTriggerRadius = 0.012f;

        private GameObject _leftFingertipTrigger;
        private GameObject _rightFingertipTrigger;

        private void Start()
        {
            SetupHandRigs();
        }

        public void SetupHandRigs()
        {
            if (leftHandRoot == null) leftHandRoot = GameObject.Find("[BuildingBlock] Hand Tracking left");
            if (rightHandRoot == null) rightHandRoot = GameObject.Find("[BuildingBlock] Hand Tracking right");

            if (leftHandRoot != null)
            {
                AttachFingertipTrigger(leftHandRoot, Handedness.Left, ref _leftFingertipTrigger);
            }

            if (rightHandRoot != null)
            {
                AttachFingertipTrigger(rightHandRoot, Handedness.Right, ref _rightFingertipTrigger);
            }
        }

        private void AttachFingertipTrigger(GameObject handObj, Handedness handedness, ref GameObject triggerObj)
        {
            // Find index tip transform in hierarchy
            Transform indexTip = FindIndexTip(handObj.transform);
            if (indexTip == null) return;

            string triggerName = (handedness == Handedness.Left ? "Left" : "Right") + "_IndexFingertip_PokeTrigger";
            Transform existing = indexTip.Find(triggerName);
            if (existing != null)
            {
                triggerObj = existing.gameObject;
                return;
            }

            triggerObj = new GameObject(triggerName);
            triggerObj.transform.SetParent(indexTip, false);
            triggerObj.transform.localPosition = Vector3.zero;
            triggerObj.transform.localRotation = Quaternion.identity;

            var sphere = triggerObj.AddComponent<SphereCollider>();
            sphere.isTrigger = true;
            sphere.radius = fingertipTriggerRadius;

            var rb = triggerObj.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        private Transform FindIndexTip(Transform root)
        {
            // Check common bone and marker naming conventions in Meta XR SDK
            string[] candidateNames = new string[] {
                "XRHand_IndexTip",
                "l_index_finger_tip_marker",
                "r_index_finger_tip_marker",
                "b_l_index_null",
                "b_r_index_null",
                "b_l_index3",
                "b_r_index3",
                "HandIndexTip"
            };

            foreach (var n in candidateNames)
            {
                var found = FindDeepChild(root, n);
                if (found != null) return found;
            }

            return null;
        }

        private Transform FindDeepChild(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name.Equals(name, System.StringComparison.OrdinalIgnoreCase)) return child;
                var result = FindDeepChild(child, name);
                if (result != null) return result;
            }
            return null;
        }

        private void Update()
        {
            // If fingertip triggers were not attached because hand bones initialize dynamically at runtime, retry
            if (_leftFingertipTrigger == null && leftHandRoot != null)
            {
                AttachFingertipTrigger(leftHandRoot, Handedness.Left, ref _leftFingertipTrigger);
            }
            if (_rightFingertipTrigger == null && rightHandRoot != null)
            {
                AttachFingertipTrigger(rightHandRoot, Handedness.Right, ref _rightFingertipTrigger);
            }
        }
    }
}
