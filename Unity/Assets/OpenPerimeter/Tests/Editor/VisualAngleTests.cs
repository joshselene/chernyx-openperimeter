using NUnit.Framework;
using UnityEngine;
using OpenPerimeter.Calibration;
using OpenPerimeter.Core;

namespace OpenPerimeter.Tests
{
    [TestFixture]
    public class VisualAngleTests
    {
        private ViewerProfileSpec viewer;
        private DeviceDisplaySpec display;

        [SetUp]
        public void Setup()
        {
            viewer = new ViewerProfileSpec
            {
                focal_length_mm = 45.0f,
                ipd_mm = 64.0f,
                eye_relief_mm = 13.0f,
                distortion_coefficients = new DistortionCoefficients { k1 = 0.34f, k2 = 0.12f }
            };

            display = new DeviceDisplaySpec
            {
                ppi = 512.0f,
                resolution_px = new int[] { 1440, 3120 },
                refresh_rate_hz = 90
            };
        }

        [Test]
        public void GoldmannSizes_SubtendCorrectAngularDiameters()
        {
            Assert.AreEqual(0.4310f, VisualAngleCalculator.GetAngularDiameterDeg(GoldmannSize.SizeIII), 0.001f);
            Assert.AreEqual(1.7240f, VisualAngleCalculator.GetAngularDiameterDeg(GoldmannSize.SizeV), 0.001f);
            // Size V has 4x diameter of Size III (16x area)
            Assert.AreEqual(4.0f, VisualAngleCalculator.GetAngularDiameterDeg(GoldmannSize.SizeV) / VisualAngleCalculator.GetAngularDiameterDeg(GoldmannSize.SizeIII), 0.01f);
        }

        [Test]
        public void DegreesToScreenMm_ForSmallAngle_MatchesTrigonometry()
        {
            // r = 45 * tan(10 deg) = 45 * 0.17632698 = 7.9347 mm
            float mm = VisualAngleCalculator.DegreesToScreenMm(10.0f, 45.0f);
            Assert.AreEqual(7.935f, mm, 0.01f);
        }

        [Test]
        public void ScreenMmToDegrees_InvertsExact()
        {
            float originalDeg = 15.0f;
            float mm = VisualAngleCalculator.DegreesToScreenMm(originalDeg, 45.0f);
            float invertedDeg = VisualAngleCalculator.ScreenMmToDegrees(mm, 45.0f);
            Assert.AreEqual(originalDeg, invertedDeg, 0.001f);
        }

        [Test]
        public void GoldmannSizeIII_PixelDiameter_On512PpiIsApprox6To7Pixels()
        {
            float diameterPx = VisualAngleCalculator.GetStimulusPixelDiameter(GoldmannSize.SizeIII, 45.0f, 512.0f);
            // r_mm = 45 * tan(0.431 deg) = 45 * 0.0075225 = 0.3385 mm
            // px = 0.3385 * (512 / 25.4) = 6.82 pixels
            Assert.AreEqual(6.82f, diameterPx, 0.1f);
        }

        [Test]
        public void VisualFieldToScreenPixels_OriginAtCenter_ReturnsZero()
        {
            Vector2 screenPx = VisualAngleCalculator.VisualFieldToScreenPixels(0f, 0f, viewer, display);
            Assert.AreEqual(0f, screenPx.x, 0.01f);
            Assert.AreEqual(0f, screenPx.y, 0.01f);
        }
    }
}
