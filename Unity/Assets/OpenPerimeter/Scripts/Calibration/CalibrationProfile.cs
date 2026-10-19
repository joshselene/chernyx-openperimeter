using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace OpenPerimeter.Calibration
{
    public enum CensoringStatus
    {
        Measured = 0,
        FloorCensored = 1,   // Scotoma too deep; required stimulus > L_max (cannot be presented bright enough)
        CeilingCensored = 2  // Sensitivity too high; required stimulus < minimum DAC quantization step
    }

    [Serializable]
    public class DeviceDisplaySpec
    {
        public string panel_technology = "OLED";
        public int[] resolution_px = new int[] { 1080, 2400 };
        public float ppi = 409f;
        public int refresh_rate_hz = 60;
        public string color_mode = "Natural";
    }

    [Serializable]
    public class DeviceProfileSpec
    {
        public string manufacturer = "Generic";
        public string model = "Generic OLED";
        public string android_version = "14";
        public DeviceDisplaySpec display = new DeviceDisplaySpec();
    }

    [Serializable]
    public class DistortionCoefficients
    {
        public float k1 = 0.34f;
        public float k2 = 0.12f;
    }

    [Serializable]
    public class ViewerProfileSpec
    {
        public string manufacturer = "Google";
        public string model = "Cardboard v2";
        public string lens_type = "Biconvex Acrylic";
        public float focal_length_mm = 45.0f;
        public float ipd_mm = 64.0f;
        public float eye_relief_mm = 13.0f;
        public DistortionCoefficients distortion_coefficients = new DistortionCoefficients();
    }

    [Serializable]
    public class PhotometricMeasurements
    {
        public string photometer_model = "Konica Minolta LS-100";
        public string photometer_serial = "000000";
        public string last_npl_calibration_date = "2026-01-01";
        public float background_target_cdm2 = 10.0f;
        public float background_measured_cdm2 = 10.0f;
        public int background_dac_value = 31;
        public float peak_measured_cdm2 = 500.0f;
        public float measured_dynamic_range_db = 34.0f;
        public float hfa_offset_db = 8.04f;
        public List<int> lut_samples_dac = new List<int>();
        public List<float> lut_samples_cdm2 = new List<float>();
    }

    [Serializable]
    public class CalibrationSignature
    {
        public string algorithm = "HMAC-SHA256";
        public string hash = "";
    }

    [Serializable]
    public class CalibrationProfile
    {
        public const float HFA_MAX_LUMINANCE_CDM2 = 3183.1f; // 10,000 asb / pi
        public const float HFA_BACKGROUND_CDM2 = 10.03f;     // 31.5 asb / pi

        public string schema_version = "1.0.0";
        public string calibration_id = "";
        public string created_at_utc = "";
        public string laboratory = "";
        public string operator_id = "";
        public bool is_demo_template = false;

        public DeviceProfileSpec device_profile = new DeviceProfileSpec();
        public ViewerProfileSpec viewer_profile = new ViewerProfileSpec();
        public PhotometricMeasurements photometric_measurements = new PhotometricMeasurements();
        public CalibrationSignature signature = new CalibrationSignature();

        /// <summary>
        /// Computes constant offset between HFA 0 dB (3183.1 cd/m2) and device 0 dB (L_max).
        /// Delta dB = 10 * log10(3183.1 / L_max)
        /// </summary>
        public float ComputeHfaOffsetDb()
        {
            if (photometric_measurements.peak_measured_cdm2 <= 0.001f)
                return 0f;
            return 10.0f * Mathf.Log10(HFA_MAX_LUMINANCE_CDM2 / photometric_measurements.peak_measured_cdm2);
        }

        /// <summary>
        /// Converts device decibels (relative to device peak L_max) to stimulus luminance increment delta_L in cd/m2.
        /// delta_L = L_max * 10^(-dB / 10)
        /// </summary>
        public float DeviceDbToStimulusDeltaL(float deviceDb)
        {
            float lMax = photometric_measurements.peak_measured_cdm2;
            if (deviceDb < 0f) return lMax;
            return lMax * Mathf.Pow(10.0f, -deviceDb / 10.0f);
        }

        /// <summary>
        /// Converts stimulus increment delta_L (cd/m2) to device decibels.
        /// dB_device = 10 * log10(L_max / delta_L)
        /// </summary>
        public float StimulusDeltaLToDeviceDb(float deltaL)
        {
            float lMax = photometric_measurements.peak_measured_cdm2;
            if (deltaL <= 0.0001f) return photometric_measurements.measured_dynamic_range_db;
            if (deltaL >= lMax) return 0f;
            return 10.0f * Mathf.Log10(lMax / deltaL);
        }

        /// <summary>
        /// Converts device dB to equivalent HFA dB.
        /// dB_HFA = dB_device - offset
        /// </summary>
        public float DeviceDbToHfaDb(float deviceDb)
        {
            return deviceDb - photometric_measurements.hfa_offset_db;
        }

        /// <summary>
        /// Converts HFA dB to device dB.
        /// dB_device = dB_HFA + offset
        /// </summary>
        public float HfaDbToDeviceDb(float hfaDb)
        {
            return hfaDb + photometric_measurements.hfa_offset_db;
        }

        /// <summary>
        /// Evaluates whether a target dB level is measurable or censored.
        /// </summary>
        public CensoringStatus CheckCensoring(float deviceDb)
        {
            if (deviceDb < 0f)
            {
                // Requires stimulus brighter than phone's peak luminance
                return CensoringStatus.FloorCensored;
            }

            float deltaL = DeviceDbToStimulusDeltaL(deviceDb);
            float targetTotalL = photometric_measurements.background_measured_cdm2 + deltaL;

            if (targetTotalL > photometric_measurements.peak_measured_cdm2)
            {
                return CensoringStatus.FloorCensored;
            }

            // Check if deltaL is smaller than the minimum 1-DAC step above background
            int bgDac = photometric_measurements.background_dac_value;
            float minResolvableDeltaL = GetLuminanceForDac(bgDac + 1) - GetLuminanceForDac(bgDac);
            if (deltaL < minResolvableDeltaL * 0.5f)
            {
                return CensoringStatus.CeilingCensored;
            }

            return CensoringStatus.Measured;
        }

        /// <summary>
        /// Finds the 8-bit DAC integer (0-255) for a requested target luminance (cd/m2) using monotonic piecewise linear interpolation.
        /// </summary>
        public int GetDacForLuminance(float targetCdm2)
        {
            var dacList = photometric_measurements.lut_samples_dac;
            var cdm2List = photometric_measurements.lut_samples_cdm2;

            if (dacList == null || cdm2List == null || dacList.Count == 0 || dacList.Count != cdm2List.Count)
            {
                // Fallback gamma 2.2 approximation
                float normalized = Mathf.Clamp01(targetCdm2 / Mathf.Max(1f, photometric_measurements.peak_measured_cdm2));
                return Mathf.RoundToInt(Mathf.Pow(normalized, 1.0f / 2.2f) * 255f);
            }

            if (targetCdm2 <= cdm2List[0]) return dacList[0];
            if (targetCdm2 >= cdm2List[cdm2List.Count - 1]) return dacList[dacList.Count - 1];

            for (int i = 0; i < cdm2List.Count - 1; i++)
            {
                if (targetCdm2 >= cdm2List[i] && targetCdm2 <= cdm2List[i + 1])
                {
                    float t = (targetCdm2 - cdm2List[i]) / Mathf.Max(0.0001f, (cdm2List[i + 1] - cdm2List[i]));
                    float interpolatedDac = Mathf.Lerp((float)dacList[i], (float)dacList[i + 1], t);
                    return Mathf.Clamp(Mathf.RoundToInt(interpolatedDac), 0, 255);
                }
            }

            return 255;
        }

        /// <summary>
        /// Retrieves measured luminance (cd/m2) for a given DAC level.
        /// </summary>
        public float GetLuminanceForDac(int dac)
        {
            dac = Mathf.Clamp(dac, 0, 255);
            var dacList = photometric_measurements.lut_samples_dac;
            var cdm2List = photometric_measurements.lut_samples_cdm2;

            if (dacList == null || cdm2List == null || dacList.Count == 0 || dacList.Count != cdm2List.Count)
            {
                // Fallback gamma 2.2
                return photometric_measurements.peak_measured_cdm2 * Mathf.Pow(dac / 255f, 2.2f);
            }

            if (dac <= dacList[0]) return cdm2List[0];
            if (dac >= dacList[dacList.Count - 1]) return cdm2List[cdm2List.Count - 1];

            for (int i = 0; i < dacList.Count - 1; i++)
            {
                if (dac >= dacList[i] && dac <= dacList[i + 1])
                {
                    float t = (float)(dac - dacList[i]) / Mathf.Max(1, (dacList[i + 1] - dacList[i]));
                    return Mathf.Lerp(cdm2List[i], cdm2List[i + 1], t);
                }
            }

            return photometric_measurements.peak_measured_cdm2;
        }

        /// <summary>
        /// Cryptographic HMAC-SHA256 signature verification to prevent tampering or uncalibrated runs.
        /// </summary>
        public bool VerifySignature(string secretKey)
        {
            if (is_demo_template) return false;
            if (string.IsNullOrEmpty(signature.hash)) return false;

            string payload = $"{calibration_id}|{device_profile.model}|{photometric_measurements.peak_measured_cdm2:F2}|{photometric_measurements.background_measured_cdm2:F2}|{photometric_measurements.background_dac_value}";
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey)))
            {
                byte[] hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
                string computedHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                return string.Equals(computedHash, signature.hash, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
