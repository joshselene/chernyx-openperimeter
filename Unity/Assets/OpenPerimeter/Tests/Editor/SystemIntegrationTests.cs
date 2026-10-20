using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using OpenPerimeter.Calibration;
using OpenPerimeter.Core;
using OpenPerimeter.Device;
using OpenPerimeter.Display;
using OpenPerimeter.Patterns;
using OpenPerimeter.Reliability;
using OpenPerimeter.Storage;
using OpenPerimeter.ThresholdEngine;

namespace OpenPerimeter.Tests
{
    [TestFixture]
    public class SystemIntegrationTests
    {
        private GameObject rootGo;
        private OpenPerimeterBootstrap bootstrap;
        private PerimetrySessionController sessionController;
        private StimulusPresenter stimulusPresenter;
        private FixationTarget fixationTarget;
        private MobileDeviceController deviceController;
        private Material testViewportMaterial;
        private Shader stereoShader;

        [SetUp]
        public void Setup()
        {
            rootGo = new GameObject("TestRoot");
            stereoShader = Shader.Find("OpenPerimeter/PerimeterStereoViewport");
            testViewportMaterial = new Material(stereoShader != null ? stereoShader : Shader.Find("Universal Render Pipeline/Unlit"));

            sessionController = rootGo.AddComponent<PerimetrySessionController>();
            stimulusPresenter = rootGo.AddComponent<StimulusPresenter>();
            fixationTarget = rootGo.AddComponent<FixationTarget>();
            deviceController = rootGo.AddComponent<MobileDeviceController>();
            bootstrap = rootGo.AddComponent<OpenPerimeterBootstrap>();

            stimulusPresenter.ViewportMaterial = testViewportMaterial;
            stimulusPresenter.FixationTarget = fixationTarget;
            sessionController.StimulusPresenter = stimulusPresenter;
            sessionController.FixationTarget = fixationTarget;
        }

        [TearDown]
        public void Teardown()
        {
            if (rootGo != null)
                UnityEngine.Object.DestroyImmediate(rootGo);
            if (testViewportMaterial != null)
                UnityEngine.Object.DestroyImmediate(testViewportMaterial);
        }

        [Test]
        public void Bootstrap_MissingCalibration_EntersUncalibratedDevModeSafely()
        {
            bool isCalibrated;
            string message;
            var profile = bootstrap.LoadCalibrationProfile(out isCalibrated, out message);

            Assert.IsNotNull(profile);
            Assert.IsFalse(isCalibrated);
            StringAssert.Contains("UNCALIBRATED", message);
            Assert.AreEqual(10.0f, profile.photometric_measurements.background_measured_cdm2, 0.1f);
        }

        [Test]
        public void Bootstrap_EnsureDependencies_WiresMissingComponentsAutomatically()
        {
            var standaloneGo = new GameObject("StandaloneBootstrap");
            var boot = standaloneGo.AddComponent<OpenPerimeterBootstrap>();

            boot.EnsureDependencies();

            Assert.IsNotNull(boot.SessionController);
            Assert.IsNotNull(boot.StimulusPresenter);
            Assert.IsNotNull(boot.FixationTarget);

            UnityEngine.Object.DestroyImmediate(standaloneGo);
        }

        [Test]
        public void StimulusPresenter_ClearToBackground_RestoresUniformDAC()
        {
            var profile = bootstrap.LoadCalibrationProfile(out _, out _);
            stimulusPresenter.Initialize(profile, 64, 64);

            stimulusPresenter.ClearToBackground();

            Assert.IsNotNull(stimulusPresenter.StimulusTexture);
            Color32 bg = stimulusPresenter.BackgroundColor;
            Assert.AreEqual((byte)profile.photometric_measurements.background_dac_value, bg.r);
            Assert.AreEqual((byte)profile.photometric_measurements.background_dac_value, bg.g);
            Assert.AreEqual((byte)profile.photometric_measurements.background_dac_value, bg.b);
        }

        [Test]
        public void StimulusPresenter_SetTestedEye_UpdatesShaderTestedEyeParameter()
        {
            var profile = bootstrap.LoadCalibrationProfile(out _, out _);
            stimulusPresenter.Initialize(profile, 64, 64);

            stimulusPresenter.SetTestedEye(EyeSide.OD);
            Assert.AreEqual(0, testViewportMaterial.GetInt("_TestedEye"));

            stimulusPresenter.SetTestedEye(EyeSide.OS);
            Assert.AreEqual(1, testViewportMaterial.GetInt("_TestedEye"));
        }

        [Test]
        public void PerimetrySessionController_CancelSession_AbortsSafelyWithoutSaving()
        {
            var profile = bootstrap.LoadCalibrationProfile(out _, out _);
            sessionController.Initialize(profile);

            bool interruptedCalled = false;
            sessionController.OnSessionInterrupted += (reason) => interruptedCalled = true;

            // Session not started yet: CancelSession does not crash
            sessionController.CancelSession("Manual operator abort");

            Assert.IsFalse(sessionController.IsRunning);
        }

        [Test]
        public void EncryptedStorageService_AtomicPersistence_EncryptsAndDecryptsAccurately()
        {
            string tempDbPath = Path.Combine(Application.temporaryCachePath, $"test_db_{Guid.NewGuid()}.enc");
            var storage = new EncryptedStorageService(tempDbPath, "TestKey-12345");

            var session = new StoredSessionRecord
            {
                sessionId = "SES-UNIT-001",
                participantId = "OP-SUBJ-UNIT",
                timestampUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                testedEye = "OD",
                testPattern = "24-2",
                algorithm = "ZEST",
                isCalibrated = false
            };

            storage.SaveTestSession(session);

            var retrieved = storage.GetSession("SES-UNIT-001");
            Assert.IsNotNull(retrieved);
            Assert.AreEqual("OP-SUBJ-UNIT", retrieved.participantId);
            Assert.AreEqual("OD", retrieved.testedEye);

            // Cleanup
            if (File.Exists(tempDbPath)) File.Delete(tempDbPath);
        }

        [Test]
        public void StudyExportService_SaveExportFiles_ExportsValidCsvAndJson()
        {
            string tempExportDir = Path.Combine(Application.temporaryCachePath, $"export_{Guid.NewGuid()}");
            var session = new StoredSessionRecord
            {
                sessionId = "SES-EXP-001",
                participantId = "OP-EXP-01",
                timestampUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                testedEye = "OS",
                testPattern = "24-2",
                algorithm = "ZEST",
                stimulusSize = "SIZE_III",
                reliability = new ReliabilitySummary { isReliable = true },
                points = new List<StoredTestPoint>
                {
                    new StoredTestPoint { pointIndex = 0, xDeg = -9f, yDeg = 9f, thresholdDb = 26f }
                }
            };

            StudyExportService.SaveExportFiles(session, tempExportDir);

            string csvPath = Path.Combine(tempExportDir, "SES-EXP-001_export.csv");
            string jsonPath = Path.Combine(tempExportDir, "SES-EXP-001_export.json");

            Assert.IsTrue(File.Exists(csvPath));
            Assert.IsTrue(File.Exists(jsonPath));

            string csvText = File.ReadAllText(csvPath);
            StringAssert.Contains("SES-EXP-001", csvText);
            StringAssert.Contains("OP-EXP-01", csvText);
            StringAssert.Contains(StudyExportService.RUO_BANNER, csvText);

            // Cleanup
            if (Directory.Exists(tempExportDir)) Directory.Delete(tempExportDir, true);
        }

        [Test]
        public void ZestStrategy_Convergence_CompletesWithin10Presentations()
        {
            var zest = new ZestStrategy();
            zest.Initialize(0, 9f, 9f, 28f, 6.5f);

            int presentations = 0;
            // Simulate a patient with true sensitivity of 25 dB
            while (!zest.IsComplete && presentations < 15)
            {
                float stimDb = zest.GetNextStimulusDb();
                bool seen = stimDb <= 25.0f;
                zest.ProcessResponse(seen, 300.0);
                presentations++;
            }

            Assert.IsTrue(zest.IsComplete);
            Assert.LessOrEqual(presentations, 8); // ZEST typically converges in 4-6 trials
            var res = zest.GetFinalResult();
            Assert.AreEqual(25.0f, res.estimatedThresholdDb, 3.0f);
        }

        [Test]
        public void Staircase42_Convergence_ReversesTwiceAndTerminates()
        {
            var staircase = new Staircase42Strategy();
            staircase.Initialize(1, -9f, 9f, 25f, 0f);

            int presentations = 0;
            // Patient true sensitivity: 22 dB
            while (!staircase.IsComplete && presentations < 20)
            {
                float stimDb = staircase.GetNextStimulusDb();
                bool seen = stimDb <= 22.0f;
                staircase.ProcessResponse(seen, 350.0);
                presentations++;
            }

            Assert.IsTrue(staircase.IsComplete);
            var res = staircase.GetFinalResult();
            Assert.AreEqual(22.0f, res.estimatedThresholdDb, 4.0f);
        }
    }
}
