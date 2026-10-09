using UnityEngine;
using UnityEngine.Events;

namespace VRWatch.Interaction
{
    /// <summary>
    /// Enables direct finger poke and click interactions on watch pushers, crowns, and UI buttons.
    /// Works with Meta XR hand tracking finger tips, controller raycasts, or mouse clicks.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class PusherInteractable : MonoBehaviour
    {
        [Header("Interaction Event")]
        [SerializeField] private UnityEvent onPushed;

        [Header("Cooldown Settings")]
        [SerializeField] private float debounceSeconds = 0.25f;

        private float _lastPushTime = -1f;

        public void AddListener(UnityAction action)
        {
            if (onPushed == null) onPushed = new UnityEvent();
            onPushed.AddListener(action);
        }

        private void OnMouseDown()
        {
            TriggerPush();
        }

        private void OnTriggerEnter(Collider other)
        {
            // Trigger when touched by a finger or controller collider
            if (other.name.Contains("Tip") || other.name.Contains("Hand") || other.name.Contains("Finger") || other.name.Contains("Index"))
            {
                TriggerPush();
            }
        }

        public void TriggerPush()
        {
            if (Time.time - _lastPushTime < debounceSeconds) return;

            _lastPushTime = Time.time;
            onPushed?.Invoke();
        }
    }
}
