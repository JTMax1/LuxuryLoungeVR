using System;
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

        [Tooltip("The OVRPassthroughLayer component.")]
        [SerializeField] private OVRPassthroughLayer passthroughLayer;

        [Tooltip("Main XR Camera.")]
        [SerializeField] private Camera xrCamera;

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
                passthroughLayer = UnityEngine.Object.FindObjectOfType<OVRPassthroughLayer>();
            }

            if (xrCamera == null)
            {
                xrCamera = Camera.main;
                if (xrCamera == null)
                {
                    var eye = GameObject.Find("CenterEyeAnchor");
                    if (eye != null) xrCamera = eye.GetComponent<Camera>();
                }
            }

            GenerateToggleSound();
        }

        private void Start()
        {
            // Ensure Insight Passthrough is globally enabled in OVRManager
            if (OVRManager.instance != null)
            {
                OVRManager.instance.isInsightPassthroughEnabled = true;
            }

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
            // 1. Lounge Room Enclosure
            if (loungeEnvironment != null)
            {
                loungeEnvironment.SetActive(!passthrough);
            }

            // 2. Hardware Passthrough Layer
            if (passthroughLayer == null)
            {
                passthroughLayer = UnityEngine.Object.FindObjectOfType<OVRPassthroughLayer>();
            }

            if (passthroughLayer != null)
            {
                passthroughLayer.enabled = passthrough;
                passthroughLayer.hidden = !passthrough;
            }

            // 3. Camera Background Transparency
            if (xrCamera == null) xrCamera = Camera.main;
            if (xrCamera != null)
            {
                if (passthrough)
                {
                    // Passthrough underlay requires alpha = 0.0 to show real world
                    xrCamera.clearFlags = CameraClearFlags.SolidColor;
                    xrCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
                }
                else
                {
                    // Full VR mode uses rich dark gallery backdrop
                    xrCamera.clearFlags = CameraClearFlags.SolidColor;
                    xrCamera.backgroundColor = new Color(0.08f, 0.08f, 0.09f, 1.0f);
                }
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
