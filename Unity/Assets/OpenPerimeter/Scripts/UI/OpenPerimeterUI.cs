using System;
using UnityEngine;
using OpenPerimeter.Calibration;
using OpenPerimeter.Core;
using OpenPerimeter.Device;
using OpenPerimeter.Patterns;
using OpenPerimeter.Storage;

namespace OpenPerimeter.UI
{
    public class OpenPerimeterUI : MonoBehaviour
    {
        private OpenPerimeterBootstrap bootstrap;
        private PerimetrySessionController sessionController;
        private FixationTarget fixationTarget;
        private AndroidDeviceController deviceController;

        private string participantId = "OP-SUBJ-001";
        private float currentProgress = 0f;
        private StoredSessionRecord lastCompletedRecord = null;
        private string activeWarningMessage = "";
        private float warningDismissTime = 0f;

        private GUIStyle headerStyle;
        private GUIStyle badgeStyle;
        private GUIStyle buttonStyle;
        private GUIStyle boxStyle;
        private GUIStyle labelStyle;
        private bool stylesInitialized = false;

        public void Initialize(
            OpenPerimeterBootstrap boot,
            PerimetrySessionController controller,
            FixationTarget target,
            AndroidDeviceController device)
        {
            bootstrap = boot;
            sessionController = controller;
            fixationTarget = target;
            deviceController = device;

            if (sessionController != null)
            {
                sessionController.OnSessionProgress -= HandleSessionProgress;
                sessionController.OnSessionProgress += HandleSessionProgress;

                sessionController.OnSessionCompleted -= HandleSessionCompleted;
                sessionController.OnSessionCompleted += HandleSessionCompleted;

                sessionController.OnSessionInterrupted -= HandleSessionInterrupted;
                sessionController.OnSessionInterrupted += HandleSessionInterrupted;
            }

            if (deviceController != null)
            {
                deviceController.OnDeviceWarning -= HandleWarning;
                deviceController.OnDeviceWarning += HandleWarning;
            }

            if (fixationTarget != null)
            {
                fixationTarget.OnHeadDriftExceeded -= (deg) => HandleWarning($"Head drift exceeded {deg:F1}°! Please look straight at the amber cross.");
            }
        }

        private void HandleSessionProgress(float progress)
        {
            currentProgress = progress;
        }

        private void HandleSessionCompleted(StoredSessionRecord record)
        {
            lastCompletedRecord = record;
        }

        private void HandleSessionInterrupted(string reason)
        {
            HandleWarning($"Session ended: {reason}");
        }

        private void HandleWarning(string message)
        {
            activeWarningMessage = message;
            warningDismissTime = Time.realtimeSinceStartup + 5.0f;
        }

        private void InitStyles()
        {
            if (stylesInitialized) return;

            headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            headerStyle.normal.textColor = Color.white;

            badgeStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold
            };

            boxStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 13,
                alignment = TextAnchor.UpperLeft
            };

            labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13
            };
            labelStyle.normal.textColor = Color.white;

            stylesInitialized = true;
        }

        private void OnGUI()
        {
            InitStyles();

            // Calculate scaled GUI matrix for crisp presentation on high-DPI Android screens
            float scale = Mathf.Max(1.0f, Screen.dpi / 160f);
            if (scale > 2.5f) scale = 2.5f;

            // Draw Top Banner
            float bannerHeight = 65f * scale;
            GUI.Box(new Rect(10, 10, Screen.width - 20, bannerHeight), GUIContent.none);

            GUILayout.BeginArea(new Rect(20, 15, Screen.width - 40, bannerHeight));
            GUILayout.BeginHorizontal();
            GUILayout.Label("OpenPerimeter: Research Automated Visual Field", headerStyle);
            GUILayout.FlexibleSpace();

            bool isCalibrated = bootstrap != null && bootstrap.IsProfileCalibrated;
            GUI.color = isCalibrated ? new Color(0.2f, 0.9f, 0.3f) : new Color(1.0f, 0.6f, 0.1f);
            string badgeText = isCalibrated ? "RESEARCH CALIBRATED" : "UNCALIBRATED DEMO / DEV MODE";
            GUILayout.Box(badgeText, badgeStyle, GUILayout.Width(240 * (scale * 0.7f)));
            GUI.color = Color.white;
            GUILayout.EndHorizontal();

            GUILayout.Label("RUO - NOT A MEDICAL DEVICE - FOR INVESTIGATIONAL BENCH TESTING ONLY", labelStyle);
            GUILayout.EndArea();

            // Active Warning Banner
            if (!string.IsNullOrEmpty(activeWarningMessage) && Time.realtimeSinceStartup < warningDismissTime)
            {
                GUI.color = new Color(1.0f, 0.2f, 0.2f, 0.9f);
                GUI.Box(new Rect(20, bannerHeight + 20, Screen.width - 40, 40 * scale), activeWarningMessage, badgeStyle);
                GUI.color = Color.white;
            }

            // Draw Main Interaction Panels
            if (sessionController == null) return;

            if (sessionController.IsRunning)
            {
                DrawActiveSessionPanel(scale, bannerHeight);
            }
            else if (lastCompletedRecord != null)
            {
                DrawResultsPanel(scale, bannerHeight);
            }
            else
            {
                DrawSetupPanel(scale, bannerHeight);
            }
        }

        private void DrawSetupPanel(float scale, float bannerHeight)
        {
            float panelWidth = Mathf.Min(Screen.width - 40, 520 * scale);
            float panelX = (Screen.width - panelWidth) * 0.5f;
            float panelY = bannerHeight + 30;

            GUILayout.BeginArea(new Rect(panelX, panelY, panelWidth, Screen.height - panelY - 20), boxStyle);
            GUILayout.Space(10);
            GUILayout.Label("<b>Perimetry Session Setup</b>", headerStyle);
            GUILayout.Space(10);

            // Participant ID
            GUILayout.BeginHorizontal();
            GUILayout.Label("Participant ID:", labelStyle, GUILayout.Width(140));
            participantId = GUILayout.TextField(participantId, 30);
            GUILayout.EndHorizontal();
            GUILayout.Space(8);

            // Eye Selection
            GUILayout.BeginHorizontal();
            GUILayout.Label("Tested Eye:", labelStyle, GUILayout.Width(140));
            if (GUILayout.Button(sessionController.TestedEye == EyeSide.OD ? "[ OD (Right Eye) ]" : "OD (Right Eye)", buttonStyle))
            {
                sessionController.TestedEye = EyeSide.OD;
                if (bootstrap != null && bootstrap.StimulusPresenter != null)
                    bootstrap.StimulusPresenter.SetTestedEye(EyeSide.OD);
            }
            if (GUILayout.Button(sessionController.TestedEye == EyeSide.OS ? "[ OS (Left Eye) ]" : "OS (Left Eye)", buttonStyle))
            {
                sessionController.TestedEye = EyeSide.OS;
                if (bootstrap != null && bootstrap.StimulusPresenter != null)
                    bootstrap.StimulusPresenter.SetTestedEye(EyeSide.OS);
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(8);

            // Test Pattern
            GUILayout.BeginHorizontal();
            GUILayout.Label("Test Pattern:", labelStyle, GUILayout.Width(140));
            if (GUILayout.Button(sessionController.PatternType == TestPatternType.Pattern24_2 ? "[ 24-2 (54 pts) ]" : "24-2 (54 pts)", buttonStyle))
            {
                sessionController.PatternType = TestPatternType.Pattern24_2;
            }
            if (GUILayout.Button(sessionController.PatternType == TestPatternType.Pattern10_2 ? "[ 10-2 (68 pts) ]" : "10-2 (68 pts)", buttonStyle))
            {
                sessionController.PatternType = TestPatternType.Pattern10_2;
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(8);

            // Strategy
            GUILayout.BeginHorizontal();
            GUILayout.Label("Algorithm:", labelStyle, GUILayout.Width(140));
            if (GUILayout.Button(sessionController.UseZestStrategy ? "[ ZEST (Bayesian) ]" : "ZEST (Bayesian)", buttonStyle))
            {
                sessionController.UseZestStrategy = true;
            }
            if (GUILayout.Button(!sessionController.UseZestStrategy ? "[ 4-2 Staircase ]" : "4-2 Staircase", buttonStyle))
            {
                sessionController.UseZestStrategy = false;
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(12);

            // Fixation Pose Reset
            if (GUILayout.Button("Zero Head Pose (Tare Gyroscope)", buttonStyle, GUILayout.Height(36 * scale)))
            {
                if (fixationTarget != null)
                {
                    fixationTarget.ResetBaselinePose();
                    HandleWarning("Head orientation tared. Keep head upright during test.");
                }
            }
            GUILayout.Space(12);

            // Start Button
            GUI.color = new Color(0.1f, 0.8f, 0.3f);
            if (GUILayout.Button("START EXAMINATION", buttonStyle, GUILayout.Height(48 * scale)))
            {
                sessionController.StartSession(participantId);
            }
            GUI.color = Color.white;

            GUILayout.EndArea();
        }

        private void DrawActiveSessionPanel(float scale, float bannerHeight)
        {
            float panelWidth = Mathf.Min(Screen.width - 40, 500 * scale);
            float panelX = (Screen.width - panelWidth) * 0.5f;
            float panelY = bannerHeight + 20;

            GUILayout.BeginArea(new Rect(panelX, panelY, panelWidth, Screen.height - panelY - 20), boxStyle);
            GUILayout.Label($"<b>Examination in Progress: {sessionController.TestedEye}</b>", headerStyle);
            GUILayout.Space(8);

            // Progress Bar
            GUILayout.Label($"Progress: {(currentProgress * 100f):F0}% (Presentations: {sessionController.PresentationCounter})", labelStyle);
            Rect progressRect = GUILayoutUtility.GetRect(panelWidth - 30, 20 * scale);
            GUI.Box(progressRect, GUIContent.none);
            GUI.color = new Color(0.2f, 0.7f, 1.0f);
            GUI.Box(new Rect(progressRect.x, progressRect.y, progressRect.width * Mathf.Clamp01(currentProgress), progressRect.height), GUIContent.none);
            GUI.color = Color.white;
            GUILayout.Space(10);

            // Reliability Monitor Live Readout
            if (sessionController.ReliabilityMonitor != null)
            {
                var rel = sessionController.ReliabilityMonitor.GetSummary();
                GUILayout.Label($"Fixation Losses: {rel.fixationLossCount}/{rel.fixationLossTotal} ({rel.fixationLossPercent:F0}%)", labelStyle);
                GUILayout.Label($"False Positives: {rel.falsePositiveCount}/{rel.falsePositiveTotal} ({rel.falsePositivePercent:F0}%)", labelStyle);
            }
            GUILayout.Space(12);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(sessionController.IsPaused ? "RESUME" : "PAUSE", buttonStyle, GUILayout.Height(40 * scale)))
            {
                if (sessionController.IsPaused) sessionController.ResumeSession();
                else sessionController.PauseSession("Investigator paused");
            }

            GUI.color = new Color(1.0f, 0.3f, 0.3f);
            if (GUILayout.Button("ABORT SESSION", buttonStyle, GUILayout.Height(40 * scale)))
            {
                sessionController.CancelSession("Cancelled by operator");
            }
            GUI.color = Color.white;
            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }

        private void DrawResultsPanel(float scale, float bannerHeight)
        {
            float panelWidth = Mathf.Min(Screen.width - 40, 540 * scale);
            float panelX = (Screen.width - panelWidth) * 0.5f;
            float panelY = bannerHeight + 20;

            GUILayout.BeginArea(new Rect(panelX, panelY, panelWidth, Screen.height - panelY - 20), boxStyle);
            GUILayout.Label("<b>Examination Completed</b>", headerStyle);
            GUILayout.Space(8);

            GUILayout.Label($"Session ID: {lastCompletedRecord.sessionId}", labelStyle);
            GUILayout.Label($"Participant: {lastCompletedRecord.participantId} | Eye: {lastCompletedRecord.testedEye}", labelStyle);
            GUILayout.Label($"Duration: {lastCompletedRecord.testDurationSec:F1} sec | Pattern: {lastCompletedRecord.testPattern}", labelStyle);
            GUILayout.Label($"Mean Deviation (MD): {lastCompletedRecord.meanDeviationDb:F2} dB", labelStyle);
            GUILayout.Label($"Pattern Standard Deviation (PSD): {lastCompletedRecord.patternStandardDeviationDb:F2} dB", labelStyle);

            GUI.color = lastCompletedRecord.reliability.isReliable ? Color.green : Color.yellow;
            GUILayout.Label($"Reliability: {(lastCompletedRecord.reliability.isReliable ? "RELIABLE" : "UNRELIABLE")} (FL: {lastCompletedRecord.reliability.fixationLossPercent:F0}%, FP: {lastCompletedRecord.reliability.falsePositivePercent:F0}%)", labelStyle);
            GUI.color = Color.white;

            GUILayout.Space(12);
            GUILayout.Label("<b>Data Export:</b> CSV & JSON written to persistent storage.", labelStyle);

            GUILayout.Space(14);
            if (GUILayout.Button("NEW SESSION", buttonStyle, GUILayout.Height(44 * scale)))
            {
                lastCompletedRecord = null;
                currentProgress = 0f;
            }

            GUILayout.EndArea();
        }
    }
}
