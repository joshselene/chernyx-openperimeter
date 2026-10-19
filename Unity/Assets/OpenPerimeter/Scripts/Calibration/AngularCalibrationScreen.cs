using System;
using UnityEngine;
using OpenPerimeter.Calibration;
using OpenPerimeter.Core;

namespace OpenPerimeter.Calibration
{
    public class AngularCalibrationScreen : MonoBehaviour
    {
        [Header("Calibration Grid Configuration")]
        [SerializeField] private Material stereoViewportMaterial;
        [SerializeField] private float[] standardAngleRingsDeg = new float[] { 5f, 10f, 15f, 20f, 24f };
        [SerializeField] private Color gridLineColor = new Color(0.2f, 0.7f, 1.0f, 1.0f); // Cyan
        [SerializeField] private Color tickColor = new Color(1.0f, 1.0f, 0.0f, 1.0f);     // Yellow
        [SerializeField] private Color backgroundColor = new Color(0.12f, 0.12f, 0.12f, 1.0f); // Low photopic grey

        private CalibrationProfile activeProfile;
        private Texture2D gridTexture;
        private int textureSize = 1024;

        public void Initialize(CalibrationProfile profile, int texSize = 1024)
        {
            activeProfile = profile;
            textureSize = texSize;

            gridTexture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            RenderCalibrationGrid();

            if (stereoViewportMaterial != null)
            {
                stereoViewportMaterial.SetTexture("_MainTex", gridTexture);
            }
        }

        /// <summary>
        /// Generates the angular concentric circles and millimeter ticks.
        /// </summary>
        public void RenderCalibrationGrid()
        {
            if (activeProfile == null) return;

            Color32[] pixels = new Color32[textureSize * textureSize];
            Color32 bg32 = backgroundColor;
            for (int i = 0; i < pixels.Length; i++) pixels[i] = bg32;

            int center = textureSize / 2;
            float ppi = activeProfile.device_profile.display.ppi;
            float focalLengthMm = activeProfile.viewer_profile.focal_length_mm;

            // 1. Draw Concentric Visual Angle Rings (5, 10, 15, 20, 24 degrees)
            foreach (float deg in standardAngleRingsDeg)
            {
                float radiusMm = VisualAngleCalculator.DegreesToScreenMm(deg, focalLengthMm);
                float radiusPx = VisualAngleCalculator.MmToPixels(radiusMm, ppi);
                DrawCircle(pixels, center, center, Mathf.RoundToInt(radiusPx), gridLineColor);
            }

            // 2. Draw Major Cartesian Crosshairs
            for (int x = 0; x < textureSize; x++)
            {
                pixels[center * textureSize + x] = gridLineColor;
            }
            for (int y = 0; y < textureSize; y++)
            {
                pixels[y * textureSize + center] = gridLineColor;
            }

            // 3. Draw Millimeter Ticks every 5 mm along axes
            float maxMm = VisualAngleCalculator.PixelsToMm(center, ppi);
            for (float mm = 5f; mm < maxMm; mm += 5f)
            {
                int tickOffsetPx = Mathf.RoundToInt(VisualAngleCalculator.MmToPixels(mm, ppi));
                int tickHalfLength = (Mathf.RoundToInt(mm) % 10 == 0) ? 8 : 4;

                // Horizontal ticks
                for (int ty = -tickHalfLength; ty <= tickHalfLength; ty++)
                {
                    SetPixelSafe(pixels, center + tickOffsetPx, center + ty, tickColor);
                    SetPixelSafe(pixels, center - tickOffsetPx, center + ty, tickColor);
                }

                // Vertical ticks
                for (int tx = -tickHalfLength; tx <= tickHalfLength; tx++)
                {
                    SetPixelSafe(pixels, center + tx, center + tickOffsetPx, tickColor);
                    SetPixelSafe(pixels, center + tx, center - tickOffsetPx, tickColor);
                }
            }

            gridTexture.SetPixels32(pixels);
            gridTexture.Apply(false);
        }

        private void DrawCircle(Color32[] pixels, int cx, int cy, int radius, Color32 col)
        {
            int x = radius;
            int y = 0;
            int err = 0;

            while (x >= y)
            {
                PlotCircleOctants(pixels, cx, cy, x, y, col);
                y += 1;
                err += 1 + 2 * y;
                if (2 * (err - x) + 1 > 0)
                {
                    x -= 1;
                    err += 1 - 2 * x;
                }
            }
        }

        private void PlotCircleOctants(Color32[] pixels, int cx, int cy, int x, int y, Color32 col)
        {
            SetPixelSafe(pixels, cx + x, cy + y, col);
            SetPixelSafe(pixels, cx + y, cy + x, col);
            SetPixelSafe(pixels, cx - y, cy + x, col);
            SetPixelSafe(pixels, cx - x, cy + y, col);
            SetPixelSafe(pixels, cx - x, cy - y, col);
            SetPixelSafe(pixels, cx - y, cy - x, col);
            SetPixelSafe(pixels, cx + y, cy - x, col);
            SetPixelSafe(pixels, cx + x, cy - y, col);
        }

        private void SetPixelSafe(Color32[] pixels, int x, int y, Color32 col)
        {
            if (x >= 0 && x < textureSize && y >= 0 && y < textureSize)
            {
                pixels[y * textureSize + x] = col;
            }
        }

        /// <summary>
        /// Updates lens distortion parameters live for interactive optical bench adjustment.
        /// </summary>
        public void UpdateDistortionParameters(float k1, float k2, float eyeReliefScale)
        {
            if (activeProfile != null)
            {
                activeProfile.viewer_profile.distortion_coefficients.k1 = k1;
                activeProfile.viewer_profile.distortion_coefficients.k2 = k2;
            }

            if (stereoViewportMaterial != null)
            {
                stereoViewportMaterial.SetFloat("_LensDistortionK1", k1);
                stereoViewportMaterial.SetFloat("_LensDistortionK2", k2);
                stereoViewportMaterial.SetFloat("_EyeReliefScale", eyeReliefScale);
            }
        }
    }
}
