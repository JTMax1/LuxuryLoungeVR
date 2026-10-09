using System;
using UnityEngine;

namespace VRWatch.Core
{
    /// <summary>
    /// Generates and spatializes realistic mechanical watch audio.
    /// Synthesizes an authentic 28,800 vph (4Hz / 8 ticks per second) Swiss lever escapement
    /// heartbeat and provides distance-attenuated ear proximity boost and tactile click sound effects.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    [DisallowMultipleComponent]
    public class WatchAudioController : MonoBehaviour
    {
        [Header("Escapement Characteristics")]
        [Tooltip("Oscillation frequency in vibrations per hour. Standard modern luxury movements (Rolex, ETA, Omega) beat at 28,800 vph (8 ticks/sec = 4Hz).")]
        [SerializeField] private int vibrationsPerHour = 28800;

        [Tooltip("Base volume for the ticking sound.")]
        [Range(0f, 1f)]
        [SerializeField] private float baseVolume = 0.35f;

        [Header("Ear Proximity Boost")]
        [Tooltip("Reference transform for the player's head (defaults to Camera.main or CenterEyeAnchor).")]
        [SerializeField] private Transform headTransform;

        [Tooltip("Distance in meters within which proximity volume boost engages.")]
        [SerializeField] private float proximityDistance = 0.35f;

        [Tooltip("Maximum volume when held right next to the ear.")]
        [Range(0f, 1f)]
        [SerializeField] private float maxProximityVolume = 0.95f;

        private AudioSource _tickAudioSource;
        private AudioSource _sfxAudioSource;
        private AudioClip _escapementClip;
        private AudioClip _clickClip;

        private void Awake()
        {
            _tickAudioSource = GetComponent<AudioSource>();
            ConfigureTickAudioSource();

            // Additional secondary audio source for pusher / crown clicks
            _sfxAudioSource = gameObject.AddComponent<AudioSource>();
            ConfigureSfxAudioSource();

            GenerateAudioClips();
        }

        private void Start()
        {
            if (headTransform == null)
            {
                if (Camera.main != null)
                {
                    headTransform = Camera.main.transform;
                }
                else
                {
                    GameObject centerEye = GameObject.Find("CenterEyeAnchor");
                    if (centerEye != null) headTransform = centerEye.transform;
                }
            }

            if (_escapementClip != null)
            {
                _tickAudioSource.clip = _escapementClip;
                _tickAudioSource.Play();
            }
        }

        private void Update()
        {
            if (headTransform == null) return;

            float distance = Vector3.Distance(transform.position, headTransform.position);

            if (distance < proximityDistance)
            {
                float t = 1f - Mathf.Clamp01(distance / proximityDistance);
                _tickAudioSource.volume = Mathf.Lerp(baseVolume, maxProximityVolume, t * t);
            }
            else
            {
                _tickAudioSource.volume = baseVolume;
            }
        }

        public void PlayPusherClick(float pitch = 1f)
        {
            if (_sfxAudioSource != null && _clickClip != null)
            {
                _sfxAudioSource.pitch = pitch;
                _sfxAudioSource.PlayOneShot(_clickClip, 0.8f);
            }
        }

        private void ConfigureTickAudioSource()
        {
            _tickAudioSource.loop = true;
            _tickAudioSource.playOnAwake = false;
            _tickAudioSource.spatialBlend = 1.0f; // Full 3D spatialization
            _tickAudioSource.minDistance = 0.05f;
            _tickAudioSource.maxDistance = 0.80f;
            _tickAudioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            _tickAudioSource.volume = baseVolume;
        }

        private void ConfigureSfxAudioSource()
        {
            _sfxAudioSource.loop = false;
            _sfxAudioSource.playOnAwake = false;
            _sfxAudioSource.spatialBlend = 1.0f;
            _sfxAudioSource.minDistance = 0.05f;
            _sfxAudioSource.maxDistance = 1.20f;
            _sfxAudioSource.volume = 0.8f;
        }

        /// <summary>
        /// Mathematically synthesizes authentic Swiss lever escapement audio samples.
        /// An 8-beat escapement alternates between entry pallet and exit pallet ruby jewel strikes.
        /// </summary>
        private void GenerateAudioClips()
        {
            int sampleRate = 44100;
            float beatsPerSecond = vibrationsPerHour / 3600f; // e.g. 8.0 beats/sec
            float cycleDuration = 1.0f; // 1 second looped buffer
            int totalSamples = Mathf.RoundToInt(sampleRate * cycleDuration);
            float[] tickData = new float[totalSamples];

            int beatsInCycle = Mathf.RoundToInt(beatsPerSecond * cycleDuration);
            float sampleInterval = totalSamples / (float)beatsInCycle;

            for (int b = 0; b < beatsInCycle; b++)
            {
                int startIndex = Mathf.RoundToInt(b * sampleInterval);
                bool isTick = (b % 2 == 0);

                // Entry pallet jewel strike (~3200Hz) vs Exit pallet jewel strike (~4200Hz)
                float freq1 = isTick ? 3200f : 4200f;
                float freq2 = isTick ? 5400f : 6600f;
                float decayRate = isTick ? 240f : 310f; // Quick sharp mechanical impulse

                int impulseLength = Mathf.Min(sampleRate / 25, totalSamples - startIndex); // ~40ms

                for (int i = 0; i < impulseLength; i++)
                {
                    float t = i / (float)sampleRate;
                    float env = Mathf.Exp(-decayRate * t);

                    // Primary impulse + harmonic resonance + slight initial noise burst
                    float wave = (Mathf.Sin(2f * Mathf.PI * freq1 * t) * 0.65f)
                               + (Mathf.Sin(2f * Mathf.PI * freq2 * t) * 0.25f);

                    // Sub-millisecond impact transient
                    if (t < 0.003f)
                    {
                        wave += UnityEngine.Random.Range(-0.4f, 0.4f);
                    }

                    tickData[startIndex + i] += wave * env * 0.45f;
                }
            }

            _escapementClip = AudioClip.Create("MechanicalEscapement_28800", totalSamples, 1, sampleRate, false);
            _escapementClip.SetData(tickData, 0);

            // Synthesize crisp mechanical pusher click
            int clickSamples = Mathf.RoundToInt(sampleRate * 0.06f); // 60ms click
            float[] clickData = new float[clickSamples];
            for (int i = 0; i < clickSamples; i++)
            {
                float t = i / (float)sampleRate;
                float env = Mathf.Exp(-180f * t);
                float wave = (Mathf.Sin(2f * Mathf.PI * 1850f * t) * 0.6f)
                           + (Mathf.Sin(2f * Mathf.PI * 3900f * t) * 0.4f);
                if (t < 0.004f) wave += UnityEngine.Random.Range(-0.6f, 0.6f);
                clickData[i] = wave * env * 0.7f;
            }

            _clickClip = AudioClip.Create("MechanicalPusher_Click", clickSamples, 1, sampleRate, false);
            _clickClip.SetData(clickData, 0);
        }
    }
}
