using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

namespace OpenPerimeter.Device
{
    /// <summary>
    /// Cross-platform mobile device control for Android and iOS research builds.
    /// Locks brightness / keep-awake and polls battery + thermal telemetry so OLED
    /// luminance throttling can pause a session safely.
    ///
    /// Player Settings expectations (verify in Editor):
    /// - Android: IL2CPP, ARM64, Min API 29, package org.openperimeter.research
    /// - iOS: IL2CPP, ARM64, Target OS 15.0+, bundle org.openperimeter.research
    /// - Both: Landscape Left fixed orientation; disable adaptive brightness / True Tone on device
    /// </summary>
    public class MobileDeviceController : MonoBehaviour
    {
        [Header("Display Settings")]
        [SerializeField] private bool lockMaxBrightness = true;
        [SerializeField] private bool keepScreenOn = true;

        [Header("Thermal & Battery Monitoring")]
        [SerializeField] private float pollingIntervalSeconds = 5.0f;
        [SerializeField] private float thermalWarningThresholdCelsius = 42.0f;

        private float currentBatteryTemperatureCelsius = 0f;
        private int currentBatteryLevelPercent = 100;
        // Aligned across platforms: 0=None/Nominal, 1=Light/Fair, 2=Moderate/Serious, 3+=Severe/Critical
        private int currentThermalStatus = 0;
        private float lastPollTime = 0f;

        public float BatteryTemperatureCelsius => currentBatteryTemperatureCelsius;
        public int BatteryLevelPercent => currentBatteryLevelPercent;
        public int ThermalStatus => currentThermalStatus;
        public bool IsOverheating =>
            currentBatteryTemperatureCelsius >= thermalWarningThresholdCelsius || currentThermalStatus >= 2;

        public event Action<string> OnDeviceWarning;

#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern int OpenPerimeter_GetThermalState();
#endif

        private void Awake()
        {
            // VR cardboard viewers require a fixed landscape plane on both mobile OSes.
            Screen.orientation = ScreenOrientation.LandscapeLeft;

            if (keepScreenOn)
            {
                Screen.sleepTimeout = SleepTimeout.NeverSleep;
            }

            ApplyWindowBrightnessLock();
        }

        private void Start()
        {
            PollBatteryAndThermalStatus();
            CheckCalibrationFileExists();
        }

        private void Update()
        {
            if (Time.time - lastPollTime >= pollingIntervalSeconds)
            {
                lastPollTime = Time.time;
                PollBatteryAndThermalStatus();
            }
        }

        /// <summary>
        /// Locks display brightness to the calibrated maximum and keeps the screen on.
        /// Android: WindowManager.LayoutParams.screenBrightness + FLAG_KEEP_SCREEN_ON.
        /// iOS / Editor fallback: Unity Screen.brightness (requires user allow-brightness on some iOS versions).
        /// </summary>
        public void ApplyWindowBrightnessLock()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    activity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
                    {
                        using (var window = activity.Call<AndroidJavaObject>("getWindow"))
                        using (var layoutParams = window.Call<AndroidJavaObject>("getAttributes"))
                        {
                            // 1.0f = maximum display brightness override
                            layoutParams.Set("screenBrightness", lockMaxBrightness ? 1.0f : -1.0f);
                            window.Call("setAttributes", layoutParams);

                            // FLAG_KEEP_SCREEN_ON = 128 (0x80)
                            window.Call("addFlags", 128);
                        }
                    }));
                }
                Debug.Log("[OpenPerimeter] Android window brightness locked to 1.0f and screen-on enforced.");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[OpenPerimeter] Failed to lock Android window brightness: {ex.Message}");
                Screen.brightness = lockMaxBrightness ? 1.0f : Screen.brightness;
            }
#elif UNITY_IOS && !UNITY_EDITOR
            try
            {
                if (lockMaxBrightness)
                {
                    Screen.brightness = 1.0f;
                }
                Debug.Log("[OpenPerimeter] iOS Screen.brightness locked to 1.0f; sleepTimeout=NeverSleep.");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[OpenPerimeter] Failed to lock iOS screen brightness: {ex.Message}");
            }
#else
            if (lockMaxBrightness)
            {
                Screen.brightness = 1.0f;
            }
#endif
        }

        /// <summary>
        /// Polls battery level and platform thermal status.
        /// Android: BATTERY_CHANGED extras + PowerManager.getCurrentThermalStatus (API 29+).
        /// iOS: SystemInfo.batteryLevel + NSProcessInfo.thermalState (via native plugin).
        /// </summary>
        public void PollBatteryAndThermalStatus()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var intentFilter = new AndroidJavaObject("android.content.IntentFilter", "android.intent.action.BATTERY_CHANGED"))
                using (var batteryStatus = activity.Call<AndroidJavaObject>("registerReceiver", null, intentFilter))
                {
                    if (batteryStatus != null)
                    {
                        int tempTenths = batteryStatus.Call<int>("getIntExtra", "temperature", 0);
                        currentBatteryTemperatureCelsius = tempTenths / 10.0f;

                        int level = batteryStatus.Call<int>("getIntExtra", "level", -1);
                        int scale = batteryStatus.Call<int>("getIntExtra", "scale", -1);
                        if (level >= 0 && scale > 0)
                        {
                            currentBatteryLevelPercent = Mathf.RoundToInt((level / (float)scale) * 100f);
                        }
                    }
                }

                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var powerManager = activity.Call<AndroidJavaObject>("getSystemService", "power"))
                {
                    if (powerManager != null)
                    {
                        currentThermalStatus = powerManager.Call<int>("getCurrentThermalStatus");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[OpenPerimeter] Android thermal polling error: {ex.Message}");
            }
#elif UNITY_IOS && !UNITY_EDITOR
            try
            {
                float batteryFrac = SystemInfo.batteryLevel;
                if (batteryFrac >= 0f)
                {
                    currentBatteryLevelPercent = Mathf.RoundToInt(batteryFrac * 100f);
                }

                // iOS does not expose battery temperature to apps; rely on ProcessInfo thermal state.
                currentBatteryTemperatureCelsius = 0f;
                currentThermalStatus = OpenPerimeter_GetThermalState();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[OpenPerimeter] iOS thermal polling error: {ex.Message}");
            }
#else
            // Editor / other platforms: use SystemInfo when available, otherwise benign stubs.
            float batteryFrac = SystemInfo.batteryLevel;
            currentBatteryLevelPercent = batteryFrac >= 0f
                ? Mathf.RoundToInt(batteryFrac * 100f)
                : 95;
            currentBatteryTemperatureCelsius = 31.5f;
            currentThermalStatus = 0;
#endif

            if (IsOverheating)
            {
                string tempPart = currentBatteryTemperatureCelsius > 0.1f
                    ? $"{currentBatteryTemperatureCelsius:F1}°C, "
                    : string.Empty;
                string msg =
                    $"WARNING: Device thermal elevation detected ({tempPart}Status {currentThermalStatus}). OLED luminance may throttle.";
                Debug.LogWarning($"[OpenPerimeter] {msg}");
                OnDeviceWarning?.Invoke(msg);
            }
        }

        /// <summary>
        /// Verifies that a valid calibration.json file exists for this device model
        /// under Application.persistentDataPath (same path on Android and iOS).
        /// </summary>
        public bool CheckCalibrationFileExists()
        {
            string calPath = Path.Combine(Application.persistentDataPath, "calibration.json");
            if (!File.Exists(calPath))
            {
                string warn =
                    $"WARNING: No calibration.json found for {SystemInfo.deviceModel} at {calPath}. Running in UNCALIBRATED DEMO MODE.";
                Debug.LogWarning($"[OpenPerimeter] {warn}");
                OnDeviceWarning?.Invoke(warn);
                return false;
            }
            return true;
        }
    }
}
