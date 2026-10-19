using System;
using System.IO;
using UnityEngine;

namespace OpenPerimeter.Device
{
    public class AndroidDeviceController : MonoBehaviour
    {
        [Header("Display Settings")]
        [SerializeField] private bool lockMaxBrightness = true;
        [SerializeField] private bool keepScreenOn = true;

        [Header("Thermal & Battery Monitoring")]
        [SerializeField] private float pollingIntervalSeconds = 5.0f;
        [SerializeField] private float thermalWarningThresholdCelsius = 42.0f;

        private float currentBatteryTemperatureCelsius = 0f;
        private int currentBatteryLevelPercent = 100;
        private int currentThermalStatus = 0; // 0=None, 1=Light, 2=Moderate, 3=Severe, 4=Critical
        private float lastPollTime = 0f;

        public float BatteryTemperatureCelsius => currentBatteryTemperatureCelsius;
        public int BatteryLevelPercent => currentBatteryLevelPercent;
        public int ThermalStatus => currentThermalStatus;
        public bool IsOverheating => currentBatteryTemperatureCelsius >= thermalWarningThresholdCelsius || currentThermalStatus >= 2;

        public event Action<string> OnDeviceWarning;

        private void Awake()
        {
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
        /// Overrides Android WindowManager brightness to lock maximum calibrated brightness and prevent OS auto-dimming.
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
                UnityEngine.Debug.Log("[OpenPerimeter] Android Window brightness locked to 1.0f and screen-on enforced.");
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"[OpenPerimeter] Failed to lock Android window brightness: {ex.Message}");
            }
#else
            Screen.brightness = 1.0f;
#endif
        }

        /// <summary>
        /// Polls battery temperature and Android PowerManager thermal status.
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

                // Android 10+ (API 29+) ThermalStatus
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
                UnityEngine.Debug.LogWarning($"[OpenPerimeter] Thermal polling error: {ex.Message}");
            }
#else
            currentBatteryTemperatureCelsius = 31.5f;
            currentBatteryLevelPercent = 95;
            currentThermalStatus = 0;
#endif

            if (IsOverheating)
            {
                string msg = $"WARNING: Device thermal elevation detected ({currentBatteryTemperatureCelsius:F1}°C, Status {currentThermalStatus}). OLED luminance may throttle.";
                UnityEngine.Debug.LogWarning($"[OpenPerimeter] {msg}");
                OnDeviceWarning?.Invoke(msg);
            }
        }

        /// <summary>
        /// Verifies that a valid calibration.json file exists for this device model.
        /// </summary>
        public bool CheckCalibrationFileExists()
        {
            string calPath = Path.Combine(Application.persistentDataPath, "calibration.json");
            if (!File.Exists(calPath))
            {
                string warn = $"WARNING: No calibration.json found for {SystemInfo.deviceModel} at {calPath}. Running in UNCALIBRATED DEMO MODE.";
                UnityEngine.Debug.LogWarning($"[OpenPerimeter] {warn}");
                OnDeviceWarning?.Invoke(warn);
                return false;
            }
            return true;
        }
    }
}
