using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace OpenPerimeter.Storage
{
    public static class StudyExportService
    {
        public const string RUO_BANNER = "# RESEARCH USE ONLY - NOT FOR CLINICAL DECISIONS";

        /// <summary>
        /// Generates interoperable CSV formatted for paired agreement analysis with Humphrey HFA and Elisar AVA.
        /// Schema: one row per eye per point.
        /// </summary>
        public static string ExportSessionToCsv(StoredSessionRecord session)
        {
            var sb = new StringBuilder();
            sb.AppendLine(RUO_BANNER);
            sb.AppendLine($"# OpenPerimeter Test Session Export | Session ID: {session.sessionId} | Generated: {DateTime.UtcNow:o}");
            sb.AppendLine($"# Normative Status: {session.normsDisclaimer}");
            sb.AppendLine($"# Calibration Status: {(session.isCalibrated ? "RESEARCH_CALIBRATED" : "UNCALIBRATED_DEMO_MODE")}");
            sb.AppendLine($"# Reliability: {(session.reliability.isReliable ? "RELIABLE" : "UNRELIABLE")} (FL: {session.reliability.fixationLossPercent:F1}%, FP: {session.reliability.falsePositivePercent:F1}%, FN: {session.reliability.falseNegativePercent:F1}%)");
            
            // Header row
            sb.AppendLine("device,session_id,participant_id,tested_eye,test_pattern,algorithm,point_index,x_deg,y_deg,threshold_db_device,threshold_db_hfa_equiv,is_censored,stimulus_size,gcc_sector,fl_rate_pct,fp_rate_pct,fn_rate_pct,is_reliable");

            foreach (var pt in session.points)
            {
                sb.AppendLine(string.Join(",",
                    "OpenPerimeter",
                    session.sessionId,
                    session.participantId,
                    session.testedEye,
                    session.testPattern,
                    session.algorithm,
                    pt.pointIndex,
                    pt.xDeg.ToString("F1"),
                    pt.yDeg.ToString("F1"),
                    pt.thresholdDb.ToString("F2"),
                    pt.thresholdHfaEquivDb.ToString("F2"),
                    pt.censoringStatus,
                    pt.stimulusSize,
                    string.IsNullOrEmpty(pt.gccSector) ? "NONE" : pt.gccSector,
                    session.reliability.fixationLossPercent.ToString("F1"),
                    session.reliability.falsePositivePercent.ToString("F1"),
                    session.reliability.falseNegativePercent.ToString("F1"),
                    session.reliability.isReliable ? "1" : "0"
                ));
            }

            return sb.ToString();
        }

        /// <summary>
        /// Generates complete JSON representation for archival and statistical scripting.
        /// </summary>
        public static string ExportSessionToJson(StoredSessionRecord session)
        {
            return JsonUtility.ToJson(session, true);
        }

        /// <summary>
        /// Generates a randomized 3-device counterbalanced test order assignment log (HFA, AVA, OpenPerimeter).
        /// </summary>
        public static string GenerateRandomizedDeviceOrderLog(List<string> participantIds, int randomSeed = 2026)
        {
            var sb = new StringBuilder();
            sb.AppendLine(RUO_BANNER);
            sb.AppendLine("# Counterbalanced Test Order Assignment Log (Latin Square 3-Device)");
            sb.AppendLine("participant_id,assigned_sequence,device_order,mandatory_rest_minutes");

            string[][] sequences = new string[][]
            {
                new string[] { "Seq_1", "HFA -> OpenPerimeter -> AVA" },
                new string[] { "Seq_2", "HFA -> AVA -> OpenPerimeter" },
                new string[] { "Seq_3", "OpenPerimeter -> HFA -> AVA" },
                new string[] { "Seq_4", "OpenPerimeter -> AVA -> HFA" },
                new string[] { "Seq_5", "AVA -> HFA -> OpenPerimeter" },
                new string[] { "Seq_6", "AVA -> OpenPerimeter -> HFA" }
            };

            var rng = new System.Random(randomSeed);
            foreach (var pid in participantIds)
            {
                int seqIdx = rng.Next(0, sequences.Length);
                var seq = sequences[seqIdx];
                sb.AppendLine($"{pid},{seq[0]},{seq[1]},15");
            }

            return sb.ToString();
        }

        public static void SaveExportFiles(StoredSessionRecord session, string outputDirectory)
        {
            Directory.CreateDirectory(outputDirectory);
            string csvPath = Path.Combine(outputDirectory, $"{session.sessionId}_export.csv");
            string jsonPath = Path.Combine(outputDirectory, $"{session.sessionId}_export.json");

            File.WriteAllText(csvPath, ExportSessionToCsv(session));
            File.WriteAllText(jsonPath, ExportSessionToJson(session));

            UnityEngine.Debug.Log($"[OpenPerimeter] Export saved to {csvPath} and {jsonPath}");
        }
    }
}
