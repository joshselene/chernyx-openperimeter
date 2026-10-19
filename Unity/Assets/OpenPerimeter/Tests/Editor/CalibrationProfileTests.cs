using NUnit.Framework;
using OpenPerimeter.Calibration;
using System.Collections.Generic;

namespace OpenPerimeter.Tests
{
    [TestFixture]
    public class CalibrationProfileTests
    {
        private CalibrationProfile profile;

        [SetUp]
        public void Setup()
        {
            profile = new CalibrationProfile();
            profile.calibration_id = "TEST-CAL-001";
            profile.device_profile.model = "Pixel 7 Pro";
            profile.device_profile.display.ppi = 512f;
            profile.viewer_profile.focal_length_mm = 45.0f;
            profile.photometric_measurements.peak_measured_cdm2 = 500.0f;
            profile.photometric_measurements.background_measured_cdm2 = 10.0f;
            profile.photometric_measurements.background_dac_value = 31;
            profile.photometric_measurements.hfa_offset_db = profile.ComputeHfaOffsetDb();

            profile.photometric_measurements.lut_samples_dac = new List<int> { 0, 16, 31, 64, 128, 192, 255 };
            profile.photometric_measurements.lut_samples_cdm2 = new List<float> { 0.001f, 2.0f, 10.0f, 50.0f, 200.0f, 380.0f, 500.0f };
        }

        [Test]
        public void ComputeHfaOffsetDb_For500Cdm2_IsApproximately8Db()
        {
            // 10 * log10(3183.1 / 500) = 8.0388...
            float offset = profile.ComputeHfaOffsetDb();
            Assert.AreEqual(8.04f, offset, 0.05f);
        }

        [Test]
        public void DeviceDbToStimulusDeltaL_AtZeroDb_ReturnsPeakLuminance()
        {
            float deltaL = profile.DeviceDbToStimulusDeltaL(0f);
            Assert.AreEqual(500.0f, deltaL, 0.1f);
        }

        [Test]
        public void DeviceDbToStimulusDeltaL_At10Db_ReturnsOneTenthPeak()
        {
            // 10 dB = factor of 10 attenuation -> 50 cd/m2
            float deltaL = profile.DeviceDbToStimulusDeltaL(10.0f);
            Assert.AreEqual(50.0f, deltaL, 0.1f);
        }

        [Test]
        public void StimulusDeltaLToDeviceDb_InversionIsExact()
        {
            float targetDb = 15.5f;
            float deltaL = profile.DeviceDbToStimulusDeltaL(targetDb);
            float computedDb = profile.StimulusDeltaLToDeviceDb(deltaL);
            Assert.AreEqual(targetDb, computedDb, 0.01f);
        }

        [Test]
        public void CheckCensoring_NegativeDb_ReturnsFloorCensored()
        {
            // Requires brighter than device max
            CensoringStatus status = profile.CheckCensoring(-2.0f);
            Assert.AreEqual(CensoringStatus.FloorCensored, status);
        }

        [Test]
        public void CheckCensoring_NormalRange_ReturnsMeasured()
        {
            CensoringStatus status = profile.CheckCensoring(20.0f);
            Assert.AreEqual(CensoringStatus.Measured, status);
        }

        [Test]
        public void GetDacForLuminance_FindsBackgroundDacAccurately()
        {
            int dac = profile.GetDacForLuminance(10.0f);
            Assert.AreEqual(31, dac);
        }
    }
}
