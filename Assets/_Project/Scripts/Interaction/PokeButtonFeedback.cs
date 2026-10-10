using System;
using UnityEngine;
using UnityEngine.Events;
using Oculus.Interaction;

namespace VRWatch.Interaction
{
    /// <summary>
    /// Provides physical visual depression, haptic/tactile audio click feedback,
    /// and triple input detection:
    /// 1. Trigger Collider contact with fingers/controllers
    /// 2. Direct Proximity distance check to index fingertips
    /// 3. Meta XR Interaction SDK PokeInteractable.WhenSelect
    /// </summary>
    [DisallowMultipleComponent]
    public class PokeButtonFeedback : MonoBehaviour
    {
        [Header("Visual Feedback")]
        [Tooltip("The moving button cap transform. If null, uses this transform.")]
        [SerializeField] private Transform movableCap;

        [Tooltip("Local direction to depress when pushed (default -Y / down).")]
        [SerializeField] private Vector3 localPressDirection = Vector3.down;

        [Tooltip("Press travel distance in meters (default 0.006m = 6mm).")]
        [SerializeField] private float pressDepth = 0.006f;

        [Tooltip("Speed of spring recovery.")]
        [SerializeField] private float returnSpringSpeed = 22f;

        [Header("Audio Feedback")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private float debounceSeconds = 0.25f;

        [Header("Proximity Detection")]
        [Tooltip("Proximity radius in meters to trigger button when finger nears cap.")]
        [SerializeField] private float proximityTriggerRadius = 0.024f;

        [Header("Event")]
        [SerializeField] private UnityEvent onPushed = new UnityEvent();

        private Vector3 _originalLocalPos;
        private Vector3 _targetLocalPos;
        private float _lastPushTime = -1f;
        private AudioClip _clickClip;
        private bool _isInitialized = false;

        public UnityEvent OnPushed => onPushed;

        public void AddListener(UnityAction action)
        {
            if (onPushed == null) onPushed = new UnityEvent();
            onPushed.AddListener(action);
        }

        private void Awake()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (_isInitialized) return;
            _isInitialized = true;

            if (movableCap == null)
            {
                movableCap = transform;
            }

            _originalLocalPos = movableCap.localPosition;
            _targetLocalPos = _originalLocalPos;

            SetupAudio();
            SetupPokeWrapper();
        }

        private void SetupPokeWrapper()
        {
            var wrapper = GetComponent<InteractableUnityEventWrapper>();
            if (wrapper != null && wrapper.WhenSelect != null)
            {
                wrapper.WhenSelect.AddListener(TriggerPush);
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
                    audioSource.spatialBlend = 0.85f;
                    audioSource.playOnAwake = false;
                    audioSource.minDistance = 0.05f;
                    audioSource.maxDistance = 2.0f;
                }
            }

            int sampleRate = 44100;
            int totalSamples = Mathf.RoundToInt(sampleRate * 0.040f); // 40ms crisp mechanical click
            float[] data = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = i / (float)sampleRate;
                float env = Mathf.Exp(-85f * t);
                float wave = Mathf.Sin(2f * Mathf.PI * 1850f * t) * 0.7f
                           + Mathf.Sin(2f * Mathf.PI * 3400f * t) * 0.3f;
                data[i] = wave * env * 0.70f;
            }

            _clickClip = AudioClip.Create("Button_TactileClick", totalSamples, 1, sampleRate, false);
            _clickClip.SetData(data, 0);
        }

        private void Update()
        {
            if (movableCap == null) return;

            // 1. Proximity check against tracked index fingertips and controllers
            CheckProximity(HandFingertipPokeRig.LeftIndexTip);
            CheckProximity(HandFingertipPokeRig.RightIndexTip);
            CheckProximity(HandFingertipPokeRig.LeftControllerTip);
            CheckProximity(HandFingertipPokeRig.RightControllerTip);

            // 2. Smooth spring return
            movableCap.localPosition = Vector3.Lerp(
                movableCap.localPosition,
                _targetLocalPos,
                Time.deltaTime * returnSpringSpeed
            );

            if (Vector3.Distance(movableCap.localPosition, _originalLocalPos) > 0.0005f)
            {
                _targetLocalPos = _originalLocalPos;
            }
        }

        private void CheckProximity(Transform tip)
        {
            if (tip == null) return;
            float dist = Vector3.Distance(movableCap.position, tip.position);
            if (dist < proximityTriggerRadius)
            {
                TriggerPush();
            }
        }

        /// <summary>
        /// Fires button press with mechanical travel depression and audio click.
        /// </summary>
        public void TriggerPush()
        {
            if (Time.time - _lastPushTime < debounceSeconds) return;
            _lastPushTime = Time.time;

            // Instant physical depression
            if (movableCap != null)
            {
                movableCap.localPosition = _originalLocalPos + localPressDirection.normalized * pressDepth;
                _targetLocalPos = _originalLocalPos;
            }

            // Play audio click
            if (audioSource != null && _clickClip != null)
            {
                audioSource.pitch = UnityEngine.Random.Range(0.96f, 1.04f);
                audioSource.PlayOneShot(_clickClip, 0.85f);
            }

            // Fire event
            onPushed?.Invoke();
        }

        private void OnTriggerEnter(Collider other)
        {
            string n = other.name.ToLower();
            if (n.Contains("tip") || n.Contains("index") || n.Contains("finger") || n.Contains("hand") || n.Contains("poke") || n.Contains("ctrl"))
            {
                TriggerPush();
            }
        }

        private void OnMouseDown()
        {
            TriggerPush();
        }
    }
}
