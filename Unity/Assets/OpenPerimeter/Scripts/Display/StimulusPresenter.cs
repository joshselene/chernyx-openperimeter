using System;
using System.Collections;
using System.Diagnostics;
using UnityEngine;
using OpenPerimeter.Calibration;
using OpenPerimeter.Core;

namespace OpenPerimeter.Display
{
    public class PresentationResult
    {
        public int pointIndex;
        public float xDeg;
        public float yDeg;
        public float stimulusDb;
        public float stimulusCdm2;
        public int stimulusDac;
        public GoldmannSize stimulusSize;
        public double presentationDurationMs;
        public bool userResponded;
        public double reactionTimeMs;
        public int droppedFrames;
        public bool isTimingValid;
        public long timestampUnixMs;
    }

    public class StimulusPresenter : MonoBehaviour
    {
        [Header("Stimulus Timing Parameters")]
        [SerializeField] private float stimulusDurationSeconds = 0.200f; // 200 ms standard
        [SerializeField] private float responseWindowSeconds = 1.000f;  // 1000 ms standard
        [SerializeField] private float minInterStimulusIntervalSec = 0.600f; // 600 ms jitter base
        [SerializeField] private float maxInterStimulusIntervalSec = 1.000f; // 1000 ms jitter max

        [Header("Display and Viewport")]
        [SerializeField] private Material stereoViewportMaterial;
        [SerializeField] private int targetRefreshRateHz = 60;

        private CalibrationProfile activeProfile;
        private Texture2D stimulusTexture;
        private Color32[] blankBuffer;
        private Color32 backgroundColor32;
        private int textureWidth = 1024;
        private int textureHeight = 1024;

        public float StimulusDurationSeconds { get => stimulusDurationSeconds; set => stimulusDurationSeconds = value; }
        public float ResponseWindowSeconds { get => responseWindowSeconds; set => responseWindowSeconds = value; }

        public void Initialize(CalibrationProfile profile, int texWidth = 1024, int texHeight = 1024)
        {
            activeProfile = profile;
            textureWidth = texWidth;
            textureHeight = texHeight;

            int bgDac = activeProfile != null ? activeProfile.photometric_measurements.background_dac_value : 31;
            backgroundColor32 = new Color32((byte)bgDac, (byte)bgDac, (byte)bgDac, 255);

            stimulusTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            blankBuffer = new Color32[textureWidth * textureHeight];
            ClearToBackground();

            if (stereoViewportMaterial != null)
            {
                stereoViewportMaterial.SetTexture("_MainTex", stimulusTexture);
                stereoViewportMaterial.SetFloat("_BackgroundLuminanceDAC", bgDac);
            }

            QualitySettings.vSyncCount = 1; // Strict Vsync lock
            Application.targetFrameRate = activeProfile != null && activeProfile.device_profile.display.refresh_rate_hz > 0
                ? activeProfile.device_profile.display.refresh_rate_hz
                : 60;
        }

        public void ClearToBackground()
        {
            for (int i = 0; i < blankBuffer.Length; i++)
            {
                blankBuffer[i] = backgroundColor32;
            }
            stimulusTexture.SetPixels32(blankBuffer);
            stimulusTexture.Apply(false);
        }

        /// <summary>
        /// Coroutine that presents a single static stimulus with strict vsync frame counting, duration validation, and HID click monitoring.
        /// </summary>
        public IEnumerator PresentStimulusRoutine(
            int pointIndex,
            float xDeg,
            float yDeg,
            float stimulusDb,
            GoldmannSize size,
            Action<PresentationResult> onComplete)
        {
            if (activeProfile == null)
            {
                UnityEngine.Debug.LogError("[OpenPerimeter] StimulusPresenter: No active calibration profile!");
                yield break;
            }

            // 1. Calculate stimulus luminance and DAC gray level
            float deltaL = activeProfile.DeviceDbToStimulusDeltaL(stimulusDb);
            float totalL = activeProfile.photometric_measurements.background_measured_cdm2 + deltaL;
            int stimulusDac = activeProfile.GetDacForLuminance(totalL);
            Color32 stimColor = new Color32((byte)stimulusDac, (byte)stimulusDac, (byte)stimulusDac, 255);

            // 2. Compute screen pixel position and radius
            Vector2 screenOffsetPx = VisualAngleCalculator.VisualFieldToScreenPixels(
                xDeg, yDeg, activeProfile.viewer_profile, activeProfile.device_profile.display);

            float radiusPx = VisualAngleCalculator.GetStimulusPixelDiameter(
                size, activeProfile.viewer_profile.focal_length_mm, activeProfile.device_profile.display.ppi) * 0.5f;

            int cx = Mathf.RoundToInt((textureWidth * 0.5f) + screenOffsetPx.x);
            int cy = Mathf.RoundToInt((textureHeight * 0.5f) + screenOffsetPx.y);
            int rInt = Mathf.Max(1, Mathf.RoundToInt(radiusPx));

            // 3. Draw circular stimulus onto buffer
            for (int dx = -rInt; dx <= rInt; dx++)
            {
                for (int dy = -rInt; dy <= rInt; dy++)
                {
                    if (dx * dx + dy * dy <= rInt * rInt)
                    {
                        int px = cx + dx;
                        int py = cy + dy;
                        if (px >= 0 && px < textureWidth && py >= 0 && py < textureHeight)
                        {
                            blankBuffer[py * textureWidth + px] = stimColor;
                        }
                    }
                }
            }

            stimulusTexture.SetPixels32(blankBuffer);
            stimulusTexture.Apply(false);

            // 4. Timing control: 200 ms presentation locked to frame count
            int refreshRate = Application.targetFrameRate > 0 ? Application.targetFrameRate : 60;
            int framesToPresent = Mathf.Max(1, Mathf.RoundToInt(stimulusDurationSeconds * refreshRate));

            Stopwatch durationWatch = Stopwatch.StartNew();
            Stopwatch reactionWatch = Stopwatch.StartNew();

            bool userClicked = false;
            double reactionTimeMs = -1.0;
            int frameCount = 0;
            int droppedFrames = 0;

            // Stimulus ON phase (200 ms)
            while (frameCount < framesToPresent)
            {
                yield return new WaitForEndOfFrame();
                frameCount++;

                if (!userClicked && CheckUserInput())
                {
                    userClicked = true;
                    reactionTimeMs = reactionWatch.Elapsed.TotalMilliseconds;
                }
            }

            durationWatch.Stop();
            double actualDurationMs = durationWatch.Elapsed.TotalMilliseconds;

            // Stimulus OFF phase: immediately restore uniform background
            ClearToBackground();

            // Continue monitoring response window (up to responseWindowSeconds)
            float remainingWindowSec = responseWindowSeconds - (float)(actualDurationMs / 1000.0);
            float elapsedRemaining = 0f;

            while (elapsedRemaining < remainingWindowSec)
            {
                if (!userClicked && CheckUserInput())
                {
                    userClicked = true;
                    reactionTimeMs = reactionWatch.Elapsed.TotalMilliseconds;
                }
                yield return null;
                elapsedRemaining += Time.deltaTime;
            }

            reactionWatch.Stop();

            // Evaluate timing error (< 1.5 frames acceptable)
            double expectedDurationMs = stimulusDurationSeconds * 1000.0;
            double durationDeltaMs = Math.Abs(actualDurationMs - expectedDurationMs);
            bool isTimingValid = durationDeltaMs < (1000.0 / refreshRate * 1.5);

            var result = new PresentationResult
            {
                pointIndex = pointIndex,
                xDeg = xDeg,
                yDeg = yDeg,
                stimulusDb = stimulusDb,
                stimulusCdm2 = totalL,
                stimulusDac = stimulusDac,
                stimulusSize = size,
                presentationDurationMs = actualDurationMs,
                userResponded = userClicked,
                reactionTimeMs = userClicked ? reactionTimeMs : -1.0,
                droppedFrames = droppedFrames,
                isTimingValid = isTimingValid,
                timestampUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            onComplete?.Invoke(result);
        }

        private bool CheckUserInput()
        {
            // Bluetooth HID clicker / Gamepad / Keyboard / Touch
            return Input.GetKeyDown(KeyCode.Space) ||
                   Input.GetKeyDown(KeyCode.JoystickButton0) ||
                   Input.GetKeyDown(KeyCode.Return) ||
                   Input.GetMouseButtonDown(0);
        }
    }
}
