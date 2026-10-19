using System;
using UnityEngine;
using OpenPerimeter.Calibration;

namespace OpenPerimeter.Core
{
    public enum GoldmannSize
    {
        SizeI = 1,   // 0.108 deg
        SizeII = 2,  // 0.216 deg
        SizeIII = 3, // 0.431 deg (Standard)
        SizeIV = 4,  // 0.862 deg
        SizeV = 5    // 1.724 deg (Macular/Severe scotoma extension)
    }

    public static class VisualAngleCalculator
    {
        // Goldmann stimulus diameters in visual angle degrees
        public const float SIZE_I_DEG = 0.1077f;
        public const float SIZE_II_DEG = 0.2155f;
        public const float SIZE_III_DEG = 0.4310f;
        public const float SIZE_IV_DEG = 0.8620f;
        public const float SIZE_V_DEG = 1.7240f;

        /// <summary>
        /// Returns the angular diameter in visual degrees for a Goldmann size.
        /// </summary>
        public static float GetAngularDiameterDeg(GoldmannSize size)
        {
            switch (size)
            {
                case GoldmannSize.SizeI: return SIZE_I_DEG;
                case GoldmannSize.SizeII: return SIZE_II_DEG;
                case GoldmannSize.SizeIII: return SIZE_III_DEG;
                case GoldmannSize.SizeIV: return SIZE_IV_DEG;
                case GoldmannSize.SizeV: return SIZE_V_DEG;
                default: return SIZE_III_DEG;
            }
        }

        /// <summary>
        /// Converts visual angle in degrees to physical displacement on screen surface in millimeters.
        /// r_mm = f_lens * tan(theta_deg * pi / 180)
        /// </summary>
        public static float DegreesToScreenMm(float degrees, float focalLengthMm)
        {
            float rad = degrees * Mathf.Deg2Rad;
            return focalLengthMm * Mathf.Tan(rad);
        }

        /// <summary>
        /// Converts physical displacement on screen surface in millimeters to visual angle in degrees.
        /// theta_deg = atan(r_mm / f_lens) * 180 / pi
        /// </summary>
        public static float ScreenMmToDegrees(float rMm, float focalLengthMm)
        {
            float rad = Mathf.Atan(rMm / focalLengthMm);
            return rad * Mathf.Rad2Deg;
        }

        /// <summary>
        /// Converts physical millimeters to screen pixels using display PPI.
        /// </summary>
        public static float MmToPixels(float mm, float ppi)
        {
            return mm * (ppi / 25.4f);
        }

        /// <summary>
        /// Converts screen pixels to physical millimeters using display PPI.
        /// </summary>
        public static float PixelsToMm(float pixels, float ppi)
        {
            return pixels * (25.4f / ppi);
        }

        /// <summary>
        /// Calculates pixel diameter of a Goldmann stimulus on the target display.
        /// </summary>
        public static float GetStimulusPixelDiameter(GoldmannSize size, float focalLengthMm, float ppi)
        {
            float angularDiameter = GetAngularDiameterDeg(size);
            float diameterMm = DegreesToScreenMm(angularDiameter, focalLengthMm);
            return MmToPixels(diameterMm, ppi);
        }

        /// <summary>
        /// Calculates screen pixel coordinates (x, y) relative to viewport center for a visual field point (theta_x, theta_y).
        /// Applies inverse optical lens distortion using Newton-Raphson so that the post-lens retinal image lands at the exact visual angle.
        /// </summary>
        public static Vector2 VisualFieldToScreenPixels(
            float thetaXDeg, 
            float thetaYDeg, 
            ViewerProfileSpec viewer, 
            DeviceDisplaySpec display)
        {
            // 1. Calculate ideal undistorted displacement in millimeters
            float xMm = DegreesToScreenMm(thetaXDeg, viewer.focal_length_mm);
            float yMm = DegreesToScreenMm(thetaYDeg, viewer.focal_length_mm);

            // 2. Convert to ideal pixels
            float idealPxX = MmToPixels(xMm, display.ppi);
            float idealPxY = MmToPixels(yMm, display.ppi);

            // 3. Compensate for lens radial distortion:
            // Forward distortion model of lens is: r_distorted = r * (1 + k1*r_norm^2 + k2*r_norm^4)
            // We need to find pre-warped screen position r_pre such that r_lens(r_pre) = r_ideal.
            float rIdeal = Mathf.Sqrt(idealPxX * idealPxX + idealPxY * idealPxY);
            if (rIdeal < 0.001f)
            {
                return Vector2.zero;
            }

            float maxRadiusPx = (display.resolution_px[1] / 2f); // normalize to half height
            float k1 = viewer.distortion_coefficients.k1;
            float k2 = viewer.distortion_coefficients.k2;

            // Solve r_pre with Newton-Raphson
            float rNormTarget = rIdeal / maxRadiusPx;
            float rPre = rNormTarget; // initial guess

            for (int iter = 0; iter < 6; iter++)
            {
                float r2 = rPre * rPre;
                float r4 = r2 * r2;
                float f = rPre * (1.0f + k1 * r2 + k2 * r4) - rNormTarget;
                float df = 1.0f + 3.0f * k1 * r2 + 5.0f * k2 * r4;
                if (Mathf.Abs(df) < 1e-6f) break;
                float delta = f / df;
                rPre -= delta;
                if (Mathf.Abs(delta) < 1e-5f) break;
            }

            float finalRadiusPx = rPre * maxRadiusPx;
            float scale = finalRadiusPx / rIdeal;

            return new Vector2(idealPxX * scale, idealPxY * scale);
        }
    }
}
