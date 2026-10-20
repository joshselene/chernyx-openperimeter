using System;
using System.Collections;
using System.Diagnostics;
using UnityEngine;
using OpenPerimeter.Calibration;
using OpenPerimeter.Core;
using OpenPerimeter.Patterns;

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
        public string inputSource = "None";
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
        [SerializeField] private FixationTarget fixationTarget;
        [SerializeField] private int targetRefreshRateHz = 60;

        private CalibrationProfile activeProfile;
        private Texture2D stimulusTexture;
        private Color32[] blankBuffer;
        private Color32 backgroundColor32;
        private int textureWidth = 1024;
        private int textureHeight = 1024;

        public float StimulusDurationSeconds { get => stimulusDurationSeconds; set => stimulusDurationSeconds = value; }
        public float ResponseWindowSeconds { get => responseWindowSeconds; set => responseWindowSeconds = value; }
        public Material ViewportMaterial { get => stereoViewportMaterial; set => stereoViewportMaterial = value; }
        public FixationTarget FixationTarget { get => fixationTarget; set => fixationTarget = value; }
        public Texture2D StimulusTexture => stimulusTexture;
        public Color32 BackgroundColor => backgroundColor32;

        public void Initialize(CalibrationProfile profile, int texWidth = 1024, int texHeight = 1024)
        {
            activeProfile = profile;
            textureWidth = texWidth;
            textureHeight = texHeight;

            int bgDac = activeProfile != null ? activeProfile.photometric_measurements.background_dac_value : 31;
            backgroundColor32 = new Color32((byte)bgDac, (byte)bgDac, (byte)bgDac, 255);

            if (stimulusTexture == null || stimulusTexture.width != textureWidth || stimulusTexture.height != textureHeight)
            {
                if (stimulusTexture != null) Destroy(stimulusTexture);

                stimulusTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp
                };
            }

            blankBuffer = new Color32[textureWidth * textureHeight];
            ClearToBackground();

            if (stereoViewportMaterial != null)
            {
                stereoViewportMaterial.SetTexture("_MainTex", stimulusTexture);
                stereoViewportMaterial.SetFloat("_BackgroundLuminanceDAC", bgDac);
                if (activeProfile != null && activeProfile.viewer_profile != null)
                {
                    stereoViewportMaterial.SetFloat("_LensDistortionK1", activeProfile.viewer_profile.distortion_coefficients.k1);
                    stereoViewportMaterial.SetFloat("_LensDistortionK2", activeProfile.viewer_profile.distortion_coefficients.k2);
                }
            }

            QualitySettings.vSyncCount = 1; // Strict Vsync lock
            Application.targetFrameRate = activeProfile != null && activeProfile.device_profile.display.refresh_rate_hz > 0
                ? activeProfile.device_profile.display.refresh_rate_hz
                : targetRefreshRateHz;
        }

        public void SetTestedEye(EyeSide eye)
        {
            if (stereoViewportMaterial != null)
            {
                stereoViewportMaterial.SetInt("_TestedEye", eye == EyeSide.OD ? 0 : 1);
            }
        }

        public void ClearToBackground()
        {
            if (blankBuffer == null || stimulusTexture == null) return;

            for (int i = 0; i < blankBuffer.Length; i++)
            {
                blankBuffer[i] = backgroundColor32;
            }

            DrawFixationToBuffer(blankBuffer);

            stimulusTexture.SetPixels32(blankBuffer);
            stimulusTexture.Apply(false);
        }

        private void DrawFixationToBuffer(Color32[] buffer)
        {
            if (fixationTarget == null || activeProfile == null) return;

            float focalLengthMm = activeProfile.viewer_profile.focal_length_mm;
            float ppi = activeProfile.device_profile.display.ppi;
            float pixelsPerDegree = VisualAngleCalculator.DegreesToScreenMm(1.0f, focalLengthMm) * (ppi / 25.4f);

            int cx = textureWidth / 2;
            int cy = textureHeight / 2;
            float halfSizePx = (fixationTarget.TargetType == FixationTargetType.CentralCross ? 0.5f : 0.3f) * pixelsPerDegree * 0.5f;
            int armPx = Mathf.Max(2, Mathf.RoundToInt(halfSizePx));

            Color32 amberColor = new Color32(217, 179, 26, 255); // Warm photopic amber

            // Draw Crosshair
            for (int x = cx - armPx; x <= cx + armPx; x++)
            {
                if (x >= 0 && x < textureWidth && cy >= 0 && cy < textureHeight)
                    buffer[cy * textureWidth + x] = amberColor;
            }
            for (int y = cy - armPx; y <= cy + armPx; y++)
            {
                if (cx >= 0 && cx < textureWidth && y >= 0 && y < textureHeight)
                    buffer[y * textureWidth + cx] = amberColor;
            }
        }

        /// <summary>
        /// Coroutine that presents a single static stimulus with strict vsync frame counting, duration validation, and multi-input monitoring.
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

            PerimetryInputHandler.ResetDebounce();

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

            // Restore clean background with fixation target
            for (int i = 0; i < blankBuffer.Length; i++)
            {
                blankBuffer[i] = backgroundColor32;
            }
            DrawFixationToBuffer(blankBuffer);

            // 3. Draw circular stimulus onto buffer with screen bounds protection
            bool isVisibleOnScreen = (cx + rInt >= 0 && cx - rInt < textureWidth &&
                                      cy + rInt >= 0 && cy - rInt < textureHeight);

            if (isVisibleOnScreen)
            {
                int r2 = rInt * rInt;
                for (int dx = -rInt; dx <= rInt; dx++)
                {
                    for (int dy = -rInt; dy <= rInt; dy++)
                    {
                        if (dx * dx + dy * dy <= r2)
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
            }
            else
            {
                UnityEngine.Debug.LogWarning($"[OpenPerimeter] Stimulus point {pointIndex} ({xDeg}°, {yDeg}°) out of screen bounds ({cx},{cy})!");
            }

            stimulusTexture.SetPixels32(blankBuffer);
            stimulusTexture.Apply(false);

            // 4. Timing control: 200 ms presentation locked to frame count
            int refreshRate = Application.targetFrameRate > 0 ? Application.targetFrameRate : targetRefreshRateHz;
            int framesToPresent = Mathf.Max(1, Mathf.RoundToInt(stimulusDurationSeconds * refreshRate));
            float expectedFrameDurationSec = 1.0f / Mathf.Max(1, refreshRate);

            Stopwatch durationWatch = Stopwatch.StartNew();
            Stopwatch reactionWatch = Stopwatch.StartNew();

            bool userClicked = false;
            double reactionTimeMs = -1.0;
            string inputSource = "None";
            int frameCount = 0;
            int droppedFrames = 0;
            float lastFrameTime = Time.realtimeSinceStartup;

            // Stimulus ON phase (200 ms)
            while (frameCount < framesToPresent)
            {
                yield return new WaitForEndOfFrame();
                frameCount++;

                float currentFrameTime = Time.realtimeSinceStartup;
                float delta = currentFrameTime - lastFrameTime;
                if (delta > expectedFrameDurationSec * 1.5f)
                {
                    int dropped = Mathf.RoundToInt(delta / expectedFrameDurationSec) - 1;
                    if (dropped > 0) droppedFrames += dropped;
                }
                lastFrameTime = currentFrameTime;

                if (!userClicked && PerimetryInputHandler.CheckResponse(out InputSourceType src))
                {
                    userClicked = true;
                    reactionTimeMs = reactionWatch.Elapsed.TotalMilliseconds;
                    inputSource = src.ToString();
                }
            }

            durationWatch.Stop();
            double actualDurationMs = durationWatch.Elapsed.TotalMilliseconds;

            // Stimulus OFF phase: immediately restore uniform background with fixation mark
            ClearToBackground();

            // Continue monitoring response window (up to responseWindowSeconds)
            float remainingWindowSec = responseWindowSeconds - (float)(actualDurationMs / 1000.0);
            float elapsedRemaining = 0f;

            while (elapsedRemaining < remainingWindowSec)
            {
                if (!userClicked && PerimetryInputHandler.CheckResponse(out InputSourceType src))
                {
                    userClicked = true;
                    reactionTimeMs = reactionWatch.Elapsed.TotalMilliseconds;
                    inputSource = src.ToString();
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
                timestampUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                inputSource = inputSource
            };

            onComplete?.Invoke(result);
        }

        private void OnDestroy()
        {
            if (stimulusTexture != null)
            {
                Destroy(stimulusTexture);
                stimulusTexture = null;
            }
        }
    }
}
