using System;
using System.IO;
using UnityEngine;
using OpenPerimeter.Calibration;
using OpenPerimeter.Device;
using OpenPerimeter.Display;
using OpenPerimeter.Patterns;
using OpenPerimeter.UI;

namespace OpenPerimeter.Core
{
    public class OpenPerimeterBootstrap : MonoBehaviour
    {
        [Header("Scene Component References")]
        [SerializeField] private PerimetrySessionController sessionController;
        [SerializeField] private StimulusPresenter stimulusPresenter;
        [SerializeField] private FixationTarget fixationTarget;
        [SerializeField] private AndroidDeviceController deviceController;
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
            if (deviceController == null) deviceController = FindFirstObjectByType<AndroidDeviceController>();
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
                var go = new GameObject("[AndroidDeviceController]");
                go.transform.SetParent(transform);
                deviceController = go.AddComponent<AndroidDeviceController>();
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

            // 3. Locate Norms & Sector Maps
            string dataDir = GetDataDirectoryPath();
            string sectorMapPath = Path.Combine(dataDir, "sector_map.json");
            string normsPath = Path.Combine(dataDir, "norms.json");

            // 4. Initialize Session Controller
            if (sessionController != null)
            {
                sessionController.Initialize(
                    activeProfile,
                    File.Exists(sectorMapPath) ? sectorMapPath : null,
                    File.Exists(normsPath) ? normsPath : null);
            }

            // 5. Connect UI
            if (userInterface != null)
            {
                userInterface.Initialize(this, sessionController, fixationTarget, deviceController);
            }

            UnityEngine.Debug.Log($"[OpenPerimeter] Bootstrap complete. Calibration: {calibrationStatusMessage}");
        }

        public CalibrationProfile LoadCalibrationProfile(out bool isCalibrated, out string message)
        {
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
                        UnityEngine.Debug.LogWarning("[OpenPerimeter] Calibration file signature failed or is demo.");
                    }
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogWarning($"[OpenPerimeter] Failed to read {calPath}: {ex.Message}");
                }
            }

            // Fallback: Check project assets / template
            string defaultJsonPath = Path.Combine(GetDataDirectoryPath(), "devices", "default_calibration.json");
            if (File.Exists(defaultJsonPath))
            {
                try
                {
                    string json = File.ReadAllText(defaultJsonPath);
                    profile = JsonUtility.FromJson<CalibrationProfile>(json);
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogWarning($"[OpenPerimeter] Failed to read default calibration: {ex.Message}");
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

        private string GetDataDirectoryPath()
        {
            string editorDataPath = Path.Combine(Application.dataPath, "OpenPerimeter", "Data");
            if (Directory.Exists(editorDataPath)) return editorDataPath;

            string persistentDataDir = Path.Combine(Application.persistentDataPath, "OpenPerimeterData");
            if (!Directory.Exists(persistentDataDir)) Directory.CreateDirectory(persistentDataDir);
            return persistentDataDir;
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
