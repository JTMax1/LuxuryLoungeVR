using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace VRWatch.Core
{
    /// <summary>
    /// Manages the VIP collector watch catalog.
    /// Allows seamless switching between distinct luxury timepiece variants
    /// (Classic Steel Chronograph, Rose Gold Heritage, Titanium Stealth Diver)
    /// with instant material swaps, tactile clasp snap audio, and showcase tray synchronization.
    /// </summary>
    [DisallowMultipleComponent]
    public class WatchCatalogManager : MonoBehaviour
    {
        [Serializable]
        public class WatchModelPreset
        {
            public string modelName;
            public Material caseMaterial;
            public Material bezelMaterial;
            public Material dialMaterial;
            public Material handsMaterial;
            public Material chronoHandMaterial;
            public Material strapMaterial;
            public Material markersMaterial;
        }

        [Header("Active Wristwatch Rig")]
        [SerializeField] private GameObject activeWatchRoot;

        [Header("Catalog Presets")]
        [SerializeField] private List<WatchModelPreset> catalogPresets = new List<WatchModelPreset>();
        [SerializeField] private int activePresetIndex = 0;

        [Header("Audio Feedback")]
        [SerializeField] private AudioSource audioSource;

        private AudioClip _claspSnapClip;

        public int ActivePresetIndex => activePresetIndex;
        public int TotalPresets => catalogPresets.Count;

        public WatchModelPreset CurrentPreset =>
            (catalogPresets != null && activePresetIndex >= 0 && activePresetIndex < catalogPresets.Count)
                ? catalogPresets[activePresetIndex]
                : null;

        private void Awake()
        {
            if (activeWatchRoot == null)
            {
                activeWatchRoot = GameObject.Find("[WristWatch_Prototype]");
            }

            GenerateClaspAudio();
        }

        private void Start()
        {
            if (catalogPresets.Count > 0)
            {
                ApplyPreset(activePresetIndex, playAudio: false);
            }
        }

        private void Update()
        {
            // Keyboard shortcut 'N' cycles to Next Watch model in catalog
            if (Keyboard.current != null && Keyboard.current.nKey.wasPressedThisFrame)
            {
                EquipNextPreset();
            }
        }

        /// <summary>
        /// Cycles to the next watch variant in the VIP catalog.
        /// </summary>
        public void EquipNextPreset()
        {
            if (catalogPresets.Count == 0) return;
            int nextIndex = (activePresetIndex + 1) % catalogPresets.Count;
            EquipPreset(nextIndex);
        }

        /// <summary>
        /// Alias for EquipNextPreset used by UI and Poke buttons.
        /// </summary>
        public void CyclePreset()
        {
            EquipNextPreset();
        }

        /// <summary>
        /// Equips a specific catalog preset by index.
        /// </summary>
        public void EquipPreset(int index)
        {
            if (index < 0 || index >= catalogPresets.Count) return;
            activePresetIndex = index;
            ApplyPreset(activePresetIndex, playAudio: true);
            Debug.Log($"[{nameof(WatchCatalogManager)}] Equipped luxury model: '{catalogPresets[activePresetIndex].modelName}'");
        }

        public void AddPreset(WatchModelPreset preset)
        {
            catalogPresets.Add(preset);
        }

        public void ClearPresets()
        {
            catalogPresets.Clear();
        }

        private void ApplyPreset(int index, bool playAudio)
        {
            if (activeWatchRoot == null || index < 0 || index >= catalogPresets.Count) return;
            var preset = catalogPresets[index];

            // 1. Case & Metal Elements (WatchBody, Bezel, Crown, Lugs, Pushers, CenterPin)
            AssignMaterialToChild(activeWatchRoot.transform, "WatchBody", preset.caseMaterial);
            Material bezelMat = preset.bezelMaterial != null ? preset.bezelMaterial : preset.caseMaterial;
            AssignMaterialToChild(activeWatchRoot.transform, "WatchBezel", bezelMat);
            AssignMaterialToChild(activeWatchRoot.transform, "WatchCrown", preset.caseMaterial);
            AssignMaterialToChild(activeWatchRoot.transform, "Pusher_StartStop", preset.caseMaterial);
            AssignMaterialToChild(activeWatchRoot.transform, "Pusher_Reset", preset.caseMaterial);
            for (int i = 0; i < 4; i++)
            {
                AssignMaterialToChild(activeWatchRoot.transform, $"Lug_{i}", preset.caseMaterial);
            }

            // 2. Straps
            AssignMaterialToChild(activeWatchRoot.transform, "WatchStrap_Upper", preset.strapMaterial);
            AssignMaterialToChild(activeWatchRoot.transform, "WatchStrap_Lower", preset.strapMaterial);

            // 3. Dial, Markers, and Hands under WatchFace
            Transform face = activeWatchRoot.transform.Find("WatchFace");
            if (face != null)
            {
                AssignMaterialToChild(face, "DialMesh", preset.dialMaterial);
                AssignMaterialToChild(face, "CenterPin", preset.caseMaterial);

                // Hour Markers (0..11)
                Material markMat = preset.markersMaterial != null ? preset.markersMaterial : preset.handsMaterial;
                for (int h = 0; h < 12; h++)
                {
                    AssignMaterialToChild(face, $"Marker_{h}", markMat);
                }

                // Time Hands
                Transform hourPivot = face.Find("HourPivot");
                if (hourPivot != null) AssignMaterialToChild(hourPivot, "HourHand_Mesh", preset.handsMaterial);

                Transform minutePivot = face.Find("MinutePivot");
                if (minutePivot != null) AssignMaterialToChild(minutePivot, "MinuteHand_Mesh", preset.handsMaterial);

                Transform secondPivot = face.Find("SecondPivot");
                if (secondPivot != null) AssignMaterialToChild(secondPivot, "SecondHand_Mesh", preset.handsMaterial);

                // Chronograph Hand
                Transform chronoPivot = face.Find("ChronoSecondPivot");
                if (chronoPivot != null) AssignMaterialToChild(chronoPivot, "ChronoSecondHand_Mesh", preset.chronoHandMaterial);
            }

            if (playAudio)
            {
                PlayClaspAudio();
            }
        }

        private void AssignMaterialToChild(Transform parent, string childName, Material mat)
        {
            if (parent == null || mat == null) return;
            Transform child = parent.Find(childName);
            if (child != null)
            {
                var mr = child.GetComponent<MeshRenderer>();
                if (mr != null) mr.sharedMaterial = mat;
            }
        }

        private void PlayClaspAudio()
        {
            if (audioSource != null && _claspSnapClip != null)
            {
                audioSource.pitch = UnityEngine.Random.Range(0.96f, 1.04f);
                audioSource.PlayOneShot(_claspSnapClip, 0.85f);
            }
        }

        private void GenerateClaspAudio()
        {
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 0.8f;
                audioSource.playOnAwake = false;
            }

            int sampleRate = 44100;
            int totalSamples = Mathf.RoundToInt(sampleRate * 0.09f); // 90ms solid magnetic buckle lock
            float[] data = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = i / (float)sampleRate;
                float env = Mathf.Exp(-95f * t);
                float wave = Mathf.Sin(2f * Mathf.PI * 1450f * t) * 0.55f
                           + Mathf.Sin(2f * Mathf.PI * 2850f * t) * 0.35f
                           + Mathf.Sin(2f * Mathf.PI * 5200f * t) * 0.15f;
                if (t < 0.005f) wave += UnityEngine.Random.Range(-0.5f, 0.5f);
                data[i] = wave * env * 0.8f;
            }

            _claspSnapClip = AudioClip.Create("WatchClasp_Lock", totalSamples, 1, sampleRate, false);
            _claspSnapClip.SetData(data, 0);
        }
    }
}
