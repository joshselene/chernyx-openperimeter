using System;
using System.IO;
using UnityEngine;
using OpenPerimeter.Calibration;
using OpenPerimeter.Device;
using OpenPerimeter.Display;
using OpenPerimeter.Patterns;
using OpenPerimeter.Storage;
using OpenPerimeter.UI;

namespace OpenPerimeter.Core
{
    public class OpenPerimeterBootstrap : MonoBehaviour
    {
        [Header("Scene Component References")]
        [SerializeField] private PerimetrySessionController sessionController;
        [SerializeField] private StimulusPresenter stimulusPresenter;
        [SerializeField] private FixationTarget fixationTarget;
        [SerializeField] private MobileDeviceController deviceController;
        [SerializeField] private OpenPerimeterUI userInterface;
        [SerializeField] private Material stereoViewportMaterial;

        [Header("Configuration")]
        [SerializeField] private string secretSigningKey = "OpenPerimeter-GlaucomaStudy-2026";
        [SerializeField] private bool allowUncalibratedDemoRun = true;

        private CalibrationProfile activeProfile;
        private bool isProfileCalibrated = false;
        private string calibrationStatusMessage = "";

        public CalibrationProfile ActiveProfile => activeProfile;
        public bool IsProfileCalibrated => isProfileCalibrated;
        public string CalibrationStatusMessage => calibrationStatusMessage;
        public PerimetrySessionController SessionController => sessionController;
        public StimulusPresenter StimulusPresenter => stimulusPresenter;
        public FixationTarget FixationTarget => fixationTarget;

        private void Awake()
        {
            EnsureDependencies();
        }

        private void Start()
        {
            InitializeApplication();
        }

        public void EnsureDependencies()
        {
            if (sessionController == null) sessionController = FindFirstObjectByType<PerimetrySessionController>();
            if (stimulusPresenter == null) stimulusPresenter = FindFirstObjectByType<StimulusPresenter>();
            if (fixationTarget == null) fixationTarget = FindFirstObjectByType<FixationTarget>();
            if (deviceController == null) deviceController = FindFirstObjectByType<MobileDeviceController>();
            if (userInterface == null) userInterface = FindFirstObjectByType<OpenPerimeterUI>();

            if (sessionController == null)
            {
                var go = new GameObject("[PerimetrySessionController]");
                go.transform.SetParent(transform);
                sessionController = go.AddComponent<PerimetrySessionController>();
            }

            if (stimulusPresenter == null)
            {
                var go = new GameObject("[StimulusPresenter]");
                go.transform.SetParent(transform);
                stimulusPresenter = go.AddComponent<StimulusPresenter>();
            }

            if (fixationTarget == null)
            {
                var go = new GameObject("[FixationTarget]");
                go.transform.SetParent(transform);
                fixationTarget = go.AddComponent<FixationTarget>();
            }

            if (deviceController == null)
            {
                var go = new GameObject("[MobileDeviceController]");
                go.transform.SetParent(transform);
                deviceController = go.AddComponent<MobileDeviceController>();
            }

            if (userInterface == null)
            {
                var go = new GameObject("[OpenPerimeterUI]");
                go.transform.SetParent(transform);
                userInterface = go.AddComponent<OpenPerimeterUI>();
            }

            // Wire cross-component references
            stimulusPresenter.FixationTarget = fixationTarget;
            if (stereoViewportMaterial != null)
            {
                stimulusPresenter.ViewportMaterial = stereoViewportMaterial;
            }

            sessionController.StimulusPresenter = stimulusPresenter;
            sessionController.FixationTarget = fixationTarget;
        }

        public void InitializeApplication()
        {
            // 1. Load Calibration Profile
            activeProfile = LoadCalibrationProfile(out isProfileCalibrated, out calibrationStatusMessage);

            // 2. Initialize Viewport and Display
            if (stimulusPresenter != null)
            {
                stimulusPresenter.Initialize(activeProfile);
                stimulusPresenter.SetTestedEye(sessionController != null ? sessionController.TestedEye : EyeSide.OD);
            }

            // 3. Packaged norms & sector maps (Resources — works on Android and iOS)
            string sectorMapJson = PackagedResearchData.LoadTextOrNull("sector_map.json");
            string normsJson = PackagedResearchData.LoadTextOrNull("norms.json");

            // 4. Initialize Session Controller
            if (sessionController != null)
            {
                sessionController.Initialize(activeProfile, sectorMapJson, normsJson);
            }

            // 5. Connect UI
            if (userInterface != null)
            {
                userInterface.Initialize(this, sessionController, fixationTarget, deviceController);
            }

            Debug.Log($"[OpenPerimeter] Bootstrap complete. Calibration: {calibrationStatusMessage}");
        }

        public CalibrationProfile LoadCalibrationProfile(out bool isCalibrated, out string message)
        {
            // Operator-pushed photometer profile (adb / Xcode / Files app → persistentDataPath)
            string calPath = Path.Combine(Application.persistentDataPath, "calibration.json");
            CalibrationProfile profile = null;

            if (File.Exists(calPath))
            {
                try
                {
                    string json = File.ReadAllText(calPath);
                    profile = JsonUtility.FromJson<CalibrationProfile>(json);
                    if (profile != null && profile.VerifySignature(secretSigningKey))
                    {
                        isCalibrated = true;
                        message = $"Verified Photometer Calibration ({profile.calibration_id})";
                        return profile;
                    }
                    else
                    {
                        Debug.LogWarning("[OpenPerimeter] Calibration file signature failed or is demo.");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[OpenPerimeter] Failed to read {calPath}: {ex.Message}");
                }
            }

            // Fallback: packaged default template (Resources, both mobile platforms)
            string defaultJson = PackagedResearchData.LoadTextOrNull("devices/default_calibration.json");
            if (!string.IsNullOrEmpty(defaultJson))
            {
                try
                {
                    profile = JsonUtility.FromJson<CalibrationProfile>(defaultJson);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[OpenPerimeter] Failed to parse default calibration: {ex.Message}");
                }
            }

            // In-memory fallback if no file found on device
            if (profile == null)
            {
                profile = CreateDefaultInMemoryProfile();
            }

            isCalibrated = false;
            message = "UNCALIBRATED DEMO / DEV MODE - Synthetic Display Profile";
            return profile;
        }

        private CalibrationProfile CreateDefaultInMemoryProfile()
        {
            var p = new CalibrationProfile
            {
                calibration_id = "CAL-FALLBACK-DEV-001",
                is_demo_template = true,
                laboratory = "OpenPerimeter Default Laboratory",
                operator_id = "DEV-01",
                created_at_utc = DateTime.UtcNow.ToString("o")
            };
            p.device_profile.model = SystemInfo.deviceModel;
            p.device_profile.display.ppi = Screen.dpi > 0 ? Screen.dpi : 409f;
            p.device_profile.display.refresh_rate_hz = (int)Screen.currentResolution.refreshRateRatio.value > 0
                ? (int)Screen.currentResolution.refreshRateRatio.value
                : 60;
            p.photometric_measurements.background_measured_cdm2 = 10.0f;
            p.photometric_measurements.background_dac_value = 31;
            p.photometric_measurements.peak_measured_cdm2 = 500.0f;
            p.photometric_measurements.hfa_offset_db = p.ComputeHfaOffsetDb();
            return p;
        }
    }
}
