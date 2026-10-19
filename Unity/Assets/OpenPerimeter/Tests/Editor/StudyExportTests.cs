using NUnit.Framework;
using System.Collections.Generic;
using OpenPerimeter.Reliability;
using OpenPerimeter.Storage;

namespace OpenPerimeter.Tests
{
    [TestFixture]
    public class StudyExportTests
    {
        private StoredSessionRecord session;

        [SetUp]
        public void Setup()
        {
            session = new StoredSessionRecord
            {
                sessionId = "SES-TEST-001",
                participantId = "OP-SUBJ-001",
                timestampUnixMs = 1770000000000,
                testedEye = "OD",
                testPattern = "24-2",
                algorithm = "ZEST",
                stimulusSize = "SIZE_III",
                deviceModel = "Pixel 7 Pro",
                viewerModel = "Cardboard v2",
                calibrationId = "CAL-001",
                isCalibrated = true,
                testDurationSec = 285.0,
                meanDeviationDb = -4.5f,
                patternStandardDeviationDb = 3.2f,
                isNormsValidated = false,
                normsDisclaimer = "UNVALIDATED NORMS",
                reliability = new ReliabilitySummary
                {
                    fixationLossCount = 1,
                    fixationLossTotal = 10,
                    fixationLossPercent = 10.0f,
                    falsePositiveCount = 0,
                    falsePositiveTotal = 10,
                    falsePositivePercent = 0.0f,
                    falseNegativeCount = 1,
                    falseNegativeTotal = 10,
                    falseNegativePercent = 10.0f,
                    isReliable = true
                },
                points = new List<StoredTestPoint>
                {
                    new StoredTestPoint
                    {
                        pointIndex = 0,
                        xDeg = -9.0f,
                        yDeg = 21.0f,
                        thresholdDb = 25.5f,
                        thresholdHfaEquivDb = 17.5f,
                        censoringStatus = "MEASURED",
                        presentationCount = 4,
                        stimulusSize = "SIZE_III",
                        gccSector = "NONE"
                    },
                    new StoredTestPoint
                    {
                        pointIndex = 1,
                        xDeg = -3.0f,
                        yDeg = 3.0f,
                        thresholdDb = 0.0f,
                        thresholdHfaEquivDb = -8.04f,
                        censoringStatus = "FLOOR_CENSORED",
                        presentationCount = 3,
                        stimulusSize = "SIZE_III",
                        gccSector = "SN_MACULA"
                    }
                }
            };
        }

        [Test]
        public void ExportSessionToCsv_IncludesRuoBanner()
        {
            string csv = StudyExportService.ExportSessionToCsv(session);
            StringAssert.Contains("RESEARCH USE ONLY - NOT FOR CLINICAL DECISIONS", csv);
        }

        [Test]
        public void ExportSessionToCsv_ContainsExpectedHeaderAndRows()
        {
            string csv = StudyExportService.ExportSessionToCsv(session);
            StringAssert.Contains("device,session_id,participant_id,tested_eye,test_pattern", csv);
            StringAssert.Contains("OpenPerimeter,SES-TEST-001,OP-SUBJ-001,OD,24-2,ZEST,0,-9.0,21.0,25.50,17.50,MEASURED,SIZE_III,NONE", csv);
            StringAssert.Contains("OpenPerimeter,SES-TEST-001,OP-SUBJ-001,OD,24-2,ZEST,1,-3.0,3.0,0.00,-8.04,FLOOR_CENSORED,SIZE_III,SN_MACULA", csv);
        }

        [Test]
        public void GenerateRandomizedDeviceOrderLog_GeneratesExpectedAssignments()
        {
            var pids = new List<string> { "P01", "P02", "P03" };
            string log = StudyExportService.GenerateRandomizedDeviceOrderLog(pids, 42);

            StringAssert.Contains("participant_id,assigned_sequence,device_order", log);
            StringAssert.Contains("P01", log);
            StringAssert.Contains("P02", log);
            StringAssert.Contains("P03", log);
        }
    }
}
