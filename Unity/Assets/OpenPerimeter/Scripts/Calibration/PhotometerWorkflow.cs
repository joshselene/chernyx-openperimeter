using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace OpenPerimeter.Calibration
{
    public enum CalibrationStep
    {
        Idle,
        DarkroomVerification,
        BackgroundSearch,
        LUTRamp,
        UniformityCheck,
        Finalization
    }

    public class PhotometerWorkflow : MonoBehaviour
    {
        [Header("Workflow Settings")]
        [SerializeField] private Material stereoViewportMaterial;
        [SerializeField] private string secretSigningKey = "OpenPerimeter-GlaucomaStudy-2026";

        private CalibrationProfile inProgressProfile;
        private CalibrationStep currentStep = CalibrationStep.Idle;
        private int currentLutSampleIndex = 0;
        private readonly int[] standardLutDacSteps = new int[] { 0, 8, 16, 24, 31, 48, 64, 96, 128, 160, 192, 224, 255 };

        public CalibrationStep CurrentStep => currentStep;
        public int CurrentLutSampleIndex => currentLutSampleIndex;
        public int TotalLutSamples => standardLutDacSteps.Length;
        public int CurrentTargetDac => (currentStep == CalibrationStep.LUTRamp && currentLutSampleIndex < standardLutDacSteps.Length)
            ? standardLutDacSteps[currentLutSampleIndex]
            : 0;

        public event Action<string> OnWorkflowStatusMessage;
        public event Action<CalibrationProfile> OnCalibrationCompleted;

        /// <summary>
        /// Starts a new laboratory calibration session.
        /// </summary>
        public void StartCalibrationSession(
            string deviceModel,
            string viewerModel,
            float displayPpi,
            int refreshRate,
            float focalLengthMm,
            float ipdMm,
            string operatorId)
        {
            inProgressProfile = new CalibrationProfile
            {
                calibration_id = $"CAL-{deviceModel.ToUpper()}-{DateTime.UtcNow:yyyyMMdd-HHmmss}",
                created_at_utc = DateTime.UtcNow.ToString("o"),
                operator_id = operatorId,
                laboratory = "Ophthalmology Perimetry Research Lab",
                is_demo_template = false
            };

            inProgressProfile.device_profile.model = deviceModel;
            inProgressProfile.device_profile.display.ppi = displayPpi;
            inProgressProfile.device_profile.display.refresh_rate_hz = refreshRate;
            inProgressProfile.viewer_profile.model = viewerModel;
            inProgressProfile.viewer_profile.focal_length_mm = focalLengthMm;
            inProgressProfile.viewer_profile.ipd_mm = ipdMm;

            inProgressProfile.photometric_measurements.lut_samples_dac = new List<int>();
            inProgressProfile.photometric_measurements.lut_samples_cdm2 = new List<float>();

            currentStep = CalibrationStep.DarkroomVerification;
            OnWorkflowStatusMessage?.Invoke("Step 1: Mount photometer behind viewer lens. Ensure darkroom ambient < 0.1 lux.");
        }

        /// <summary>
        /// Displays a uniform test patch at a specific DAC level for spot photometer alignment and measurement.
        /// </summary>
        public void DisplayPatch(int dacValue)
        {
            dacValue = Mathf.Clamp(dacValue, 0, 255);
            if (stereoViewportMaterial != null)
            {
                stereoViewportMaterial.SetFloat("_BackgroundLuminanceDAC", dacValue);
            }
        }

        /// <summary>
        /// Confirms darkroom zeroing and transitions to Background DAC search ($10.0\text{ cd/m}^2$).
        /// </summary>
        public void ConfirmDarkroomZeroed()
        {
            currentStep = CalibrationStep.BackgroundSearch;
            DisplayPatch(31); // Initial educated guess for 10 cd/m2 on typical OLED
            OnWorkflowStatusMessage?.Invoke("Step 2: Calibrate background luminance. Target: 10.0 cd/m2 (31.5 asb). Measure and enter reading.");
        }

        /// <summary>
        /// Sets the measured background luminance and records the corresponding DAC value.
        /// </summary>
        public void RecordBackgroundMeasurement(int dacValue, float measuredCdm2)
        {
            inProgressProfile.photometric_measurements.background_dac_value = dacValue;
            inProgressProfile.photometric_measurements.background_measured_cdm2 = measuredCdm2;

            currentStep = CalibrationStep.LUTRamp;
            currentLutSampleIndex = 0;
            DisplayPatch(standardLutDacSteps[currentLutSampleIndex]);
            OnWorkflowStatusMessage?.Invoke($"Step 3: Measuring LUT Ramp. Sample 1/{TotalLutSamples}: DAC {standardLutDacSteps[0]}. Enter cd/m2 reading.");
        }

        /// <summary>
        /// Records photometer reading for the current LUT step and advances to next.
        /// </summary>
        public void RecordLutSample(float measuredCdm2)
        {
            if (currentStep != CalibrationStep.LUTRamp) return;

            int currentDac = standardLutDacSteps[currentLutSampleIndex];
            inProgressProfile.photometric_measurements.lut_samples_dac.Add(currentDac);
            inProgressProfile.photometric_measurements.lut_samples_cdm2.Add(measuredCdm2);

            currentLutSampleIndex++;

            if (currentLutSampleIndex < standardLutDacSteps.Length)
            {
                int nextDac = standardLutDacSteps[currentLutSampleIndex];
                DisplayPatch(nextDac);
                OnWorkflowStatusMessage?.Invoke($"Sample {currentLutSampleIndex + 1}/{TotalLutSamples}: DAC {nextDac}. Enter cd/m2 reading.");
            }
            else
            {
                // Ramp completed
                FinalizeCalibration();
            }
        }

        private void FinalizeCalibration()
        {
            currentStep = CalibrationStep.Finalization;

            // Peak measured luminance is DAC 255
            float peakL = inProgressProfile.photometric_measurements.lut_samples_cdm2[inProgressProfile.photometric_measurements.lut_samples_cdm2.Count - 1];
            inProgressProfile.photometric_measurements.peak_measured_cdm2 = peakL;

            // Compute HFA offset
            inProgressProfile.photometric_measurements.hfa_offset_db = inProgressProfile.ComputeHfaOffsetDb();

            // Compute dynamic range: 10 * log10(L_max / delta_L_min)
            int bgDac = inProgressProfile.photometric_measurements.background_dac_value;
            float bgL = inProgressProfile.photometric_measurements.background_measured_cdm2;
            float minDeltaL = Mathf.Max(0.1f, inProgressProfile.GetLuminanceForDac(bgDac + 1) - bgL);
            inProgressProfile.photometric_measurements.measured_dynamic_range_db = 10.0f * Mathf.Log10(peakL / minDeltaL);

            // Compute HMAC-SHA256 signature
            string payload = $"{inProgressProfile.calibration_id}|{inProgressProfile.device_profile.model}|{peakL:F2}|{bgL:F2}|{bgDac}";
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretSigningKey)))
            {
                byte[] hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
                inProgressProfile.signature.hash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }

            // Save to persistent storage
            string json = JsonUtility.ToJson(inProgressProfile, true);
            string savePath = Path.Combine(Application.persistentDataPath, "calibration.json");
            File.WriteAllText(savePath, json);

            OnWorkflowStatusMessage?.Invoke($"Calibration Complete! L_max={peakL:F1} cd/m2. HFA Offset={inProgressProfile.photometric_measurements.hfa_offset_db:F2} dB. Dynamic Range={inProgressProfile.photometric_measurements.measured_dynamic_range_db:F1} dB. Saved to {savePath}");
            OnCalibrationCompleted?.Invoke(inProgressProfile);

            // Restore background patch
            DisplayPatch(bgDac);
        }
    }
}
