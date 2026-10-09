using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace VRWatch.Core
{
    /// <summary>
    /// Controls the Super-LumiNova photoluminescent night mode.
    /// Dims the gallery ambient and directional lighting to a dramatic twilight
    /// while charging the watch hands and hour markers with vivid Swiss Super-LumiNova
    /// BGW9 / C3 emission bloom.
    /// </summary>
    [DisallowMultipleComponent]
    public class WatchLumeController : MonoBehaviour
    {
        [Header("Night Mode State")]
        [SerializeField] private bool isNightMode = false;
        [SerializeField] private float transitionSpeed = 2.5f;

        [Header("Luminescent Material")]
        [Tooltip("Material applied to emissive hands and indices.")]
        [SerializeField] private Material lumeMaterial;

        [Tooltip("Daytime base emission color (typically black / unlit).")]
        [SerializeField] private Color dayEmission = Color.black;

        [Tooltip("Nighttime HDR photoluminescent emission color.")]
        [ColorUsage(true, true)]
        [SerializeField] private Color nightEmission = new Color(0.20f, 1.0f, 0.75f) * 3.8f;

        [Header("Lighting Targets")]
        [SerializeField] private Light directionalKeyLight;
        [SerializeField] private Light pedestalSpotlight;
        [SerializeField] private List<Light> fillLights = new List<Light>();

        private float _targetBlend = 0f;
        private float _currentBlend = 0f;

        // Cached day/night light values
        private float _dirLightDayInt = 1.5f;
        private float _spotLightDayInt = 3.5f;
        private Color _ambientDayColor = new Color(1.0f, 0.98f, 0.95f) * 1.2f;

        private float _dirLightNightInt = 0.10f;
        private float _spotLightNightInt = 0.70f;
        private Color _ambientNightColor = new Color(0.04f, 0.05f, 0.08f) * 1.0f;

        public bool IsNightMode => isNightMode;

        private void Awake()
        {
            FindSceneLights();
        }

        private void Start()
        {
            _targetBlend = isNightMode ? 1f : 0f;
            _currentBlend = _targetBlend;
            ApplyBlend(_currentBlend);
        }

        private void Update()
        {
            // Keyboard shortcut 'L' toggles Super-LumiNova Night Mode
            if (Keyboard.current != null && Keyboard.current.lKey.wasPressedThisFrame)
            {
                ToggleNightMode();
            }

            if (!Mathf.Approximately(_currentBlend, _targetBlend))
            {
                _currentBlend = Mathf.MoveTowards(_currentBlend, _targetBlend, Time.deltaTime * transitionSpeed);
                ApplyBlend(_currentBlend);
            }
        }

        /// <summary>
        /// Toggles between daytime luxury showroom and nighttime Super-LumiNova glow.
        /// </summary>
        public void ToggleNightMode()
        {
            SetNightMode(!isNightMode);
        }

        public void SetNightMode(bool enableNight)
        {
            isNightMode = enableNight;
            _targetBlend = isNightMode ? 1f : 0f;
            Debug.Log($"[{nameof(WatchLumeController)}] Super-LumiNova Night Mode: {(isNightMode ? "ON (Phosphor Glow)" : "OFF (Daylight Showroom)")}");
        }

        private void ApplyBlend(float t)
        {
            // 1. Material Emission
            if (lumeMaterial != null)
            {
                Color currentEmission = Color.Lerp(dayEmission, nightEmission, t);
                lumeMaterial.SetColor("_EmissionColor", currentEmission);
                if (t > 0.01f)
                {
                    lumeMaterial.EnableKeyword("_EMISSION");
                }
                else
                {
                    lumeMaterial.DisableKeyword("_EMISSION");
                }
            }

            // 2. Directional Key Light
            if (directionalKeyLight != null)
            {
                directionalKeyLight.intensity = Mathf.Lerp(_dirLightDayInt, _dirLightNightInt, t);
            }

            // 3. Overhead Spotlight
            if (pedestalSpotlight != null)
            {
                pedestalSpotlight.intensity = Mathf.Lerp(_spotLightDayInt, _spotLightNightInt, t);
            }

            // 4. Fill Lights
            for (int i = 0; i < fillLights.Count; i++)
            {
                if (fillLights[i] != null)
                {
                    fillLights[i].intensity = Mathf.Lerp(2.0f, 0.08f, t);
                }
            }

            // 5. RenderSettings Ambient Light
            RenderSettings.ambientLight = Color.Lerp(_ambientDayColor, _ambientNightColor, t);
        }

        private void FindSceneLights()
        {
            if (directionalKeyLight == null)
            {
                GameObject dl = GameObject.Find("Directional Light");
                if (dl != null) directionalKeyLight = dl.GetComponent<Light>();
            }

            if (pedestalSpotlight == null)
            {
                GameObject sl = GameObject.Find("Spotlight_Pedestal");
                if (sl != null) pedestalSpotlight = sl.GetComponent<Light>();
            }

            if (fillLights.Count == 0)
            {
                GameObject fl1 = GameObject.Find("FillLight_Left");
                if (fl1 != null) fillLights.Add(fl1.GetComponent<Light>());

                GameObject fl2 = GameObject.Find("FillLight_Right");
                if (fl2 != null) fillLights.Add(fl2.GetComponent<Light>());
            }
        }
    }
}
