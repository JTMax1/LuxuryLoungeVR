using UnityEngine;
using UnityEngine.InputSystem;

namespace VRWatch.Interaction
{
    /// <summary>
    /// Toggles between Full VR Luxury Lounge and Real-World Mixed Reality Passthrough.
    /// In Passthrough mode, the virtual lounge environment dissolves, allowing the user
    /// to experience the virtual luxury wristwatch anchored to their real-world physical wrist.
    /// </summary>
    [DisallowMultipleComponent]
    public class LoungeModeManager : MonoBehaviour
    {
        [Header("Scene References")]
        [Tooltip("The virtual collector lounge environment parent object.")]
        [SerializeField] private GameObject loungeEnvironment;

        [Tooltip("The OVRPassthroughLayer component (if present).")]
        [SerializeField] private MonoBehaviour passthroughLayer;

        [Header("Current Mode")]
        [Tooltip("If true, Passthrough MR is active. If false, Full VR Lounge is active.")]
        [SerializeField] private bool isPassthroughActive = false;

        [Header("Audio Feedback")]
        [SerializeField] private AudioSource audioFeedbackSource;

        private AudioClip _toggleClip;

        public bool IsPassthroughActive => isPassthroughActive;

        private void Awake()
        {
            if (loungeEnvironment == null)
            {
                loungeEnvironment = GameObject.Find("[Environment_Lounge]");
            }

            if (passthroughLayer == null)
            {
                GameObject ptObj = GameObject.Find("[BuildingBlock] Passthrough");
                if (ptObj != null)
                {
                    passthroughLayer = ptObj.GetComponent<MonoBehaviour>();
                }
            }

            GenerateToggleSound();
        }

        private void Start()
        {
            ApplyModeState(isPassthroughActive);
        }

        private void Update()
        {
            // Keyboard shortcut for Editor testing: 'P' toggles Passthrough
            if (Keyboard.current != null && Keyboard.current.pKey.wasPressedThisFrame)
            {
                ToggleMode();
            }
        }

        /// <summary>
        /// Toggles between VR Lounge and MR Passthrough.
        /// </summary>
        public void ToggleMode()
        {
            SetPassthroughMode(!isPassthroughActive);
        }

        /// <summary>
        /// Explicitly sets the display mode.
        /// </summary>
        public void SetPassthroughMode(bool enablePassthrough)
        {
            isPassthroughActive = enablePassthrough;
            ApplyModeState(isPassthroughActive);

            PlayToggleAudio();
            string modeName = isPassthroughActive ? "MR Passthrough (Real World)" : "Full VR (Luxury Lounge)";
            Debug.Log($"[{nameof(LoungeModeManager)}] Switched mode to: {modeName}");
        }

        private void ApplyModeState(bool passthrough)
        {
            if (loungeEnvironment != null)
            {
                // In Passthrough mode, deactivate the virtual room geometry
                loungeEnvironment.SetActive(!passthrough);
            }

            if (passthroughLayer != null)
            {
                passthroughLayer.enabled = passthrough;
            }
        }

        private void PlayToggleAudio()
        {
            if (audioFeedbackSource != null && _toggleClip != null)
            {
                audioFeedbackSource.pitch = isPassthroughActive ? 1.15f : 0.95f;
                audioFeedbackSource.PlayOneShot(_toggleClip, 0.7f);
            }
        }

        private void GenerateToggleSound()
        {
            if (audioFeedbackSource == null)
            {
                audioFeedbackSource = gameObject.AddComponent<AudioSource>();
                audioFeedbackSource.spatialBlend = 0.5f;
                audioFeedbackSource.playOnAwake = false;
            }

            int sampleRate = 44100;
            int totalSamples = Mathf.RoundToInt(sampleRate * 0.12f); // 120ms gentle chime
            float[] data = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = i / (float)sampleRate;
                float env = Mathf.Exp(-32f * t);
                float wave = Mathf.Sin(2f * Mathf.PI * 880f * t) * 0.6f
                           + Mathf.Sin(2f * Mathf.PI * 1320f * t) * 0.4f;
                data[i] = wave * env * 0.5f;
            }

            _toggleClip = AudioClip.Create("ModeToggle_Chime", totalSamples, 1, sampleRate, false);
            _toggleClip.SetData(data, 0);
        }
    }
}
