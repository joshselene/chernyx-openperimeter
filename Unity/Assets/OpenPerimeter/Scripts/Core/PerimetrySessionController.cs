using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using OpenPerimeter.Calibration;
using OpenPerimeter.Display;
using OpenPerimeter.Patterns;
using OpenPerimeter.Reliability;
using OpenPerimeter.Storage;
using OpenPerimeter.ThresholdEngine;

namespace OpenPerimeter.Core
{
    public class PerimetrySessionController : MonoBehaviour
    {
        [Header("Subsystems")]
        [SerializeField] private StimulusPresenter stimulusPresenter;
        [SerializeField] private FixationTarget fixationTarget;

        [Header("Session Settings")]
        [SerializeField] private EyeSide testedEye = EyeSide.OD;
        [SerializeField] private TestPatternType patternType = TestPatternType.Pattern24_2;
        [SerializeField] private bool useZestStrategy = true;
        [SerializeField] private bool enableGoldmannSizeVExtension = false;

        private CalibrationProfile activeProfile;
        private ReliabilityMonitor reliabilityMonitor;
        private NormativeDeviationEngine deviationEngine;
        private MacularSectorMapper sectorMapper;
        private EncryptedStorageService storageService;

        private List<PatternPoint> activePoints;
        private Dictionary<int, IThresholdStrategy> strategyMap;
        private List<StoredPresentationEvent> presentationEvents;
        private bool isSessionRunning = false;
        private bool isPaused = false;
        private int presentationCounter = 0;
        private Coroutine sessionCoroutine;

        public event Action<float> OnSessionProgress; // 0.0 to 1.0
        public event Action<StoredSessionRecord> OnSessionCompleted;
        public event Action<string> OnSessionInterrupted;

        public bool IsRunning => isSessionRunning;
        public bool IsPaused => isPaused;
        public EyeSide TestedEye { get => testedEye; set => testedEye = value; }
        public TestPatternType PatternType { get => patternType; set => patternType = value; }
        public bool UseZestStrategy { get => useZestStrategy; set => useZestStrategy = value; }
        public ReliabilityMonitor ReliabilityMonitor => reliabilityMonitor;
        public CalibrationProfile ActiveProfile => activeProfile;
        public int PresentationCounter => presentationCounter;
        public StimulusPresenter StimulusPresenter { get => stimulusPresenter; set => stimulusPresenter = value; }
        public FixationTarget FixationTarget { get => fixationTarget; set => fixationTarget = value; }

        /// <summary>
        /// Initializes session subsystems. Optional JSON arguments are raw file contents
        /// (from Resources via PackagedResearchData), not filesystem paths — so Android
        /// and iOS both resolve packaged norms/sector maps without StreamingAssets I/O.
        /// </summary>
        public void Initialize(
            CalibrationProfile profile,
            string sectorMapJson = null,
            string normsJson = null)
        {
            activeProfile = profile;
            reliabilityMonitor = new ReliabilityMonitor();
            deviationEngine = new NormativeDeviationEngine();
            sectorMapper = new MacularSectorMapper();
            storageService = new EncryptedStorageService();

            if (!string.IsNullOrEmpty(normsJson))
                deviationEngine.LoadNormsFromJson(normsJson);

            if (!string.IsNullOrEmpty(sectorMapJson))
                sectorMapper.LoadFromJson(sectorMapJson);

            if (fixationTarget != null)
            {
                fixationTarget.OnHeadDriftExceeded -= HandleHeadDriftExceeded;
                fixationTarget.OnHeadDriftExceeded += HandleHeadDriftExceeded;

                fixationTarget.OnHeadPoseRestored -= HandleHeadPoseRestored;
                fixationTarget.OnHeadPoseRestored += HandleHeadPoseRestored;
            }

            if (stimulusPresenter != null)
            {
                stimulusPresenter.SetTestedEye(testedEye);
            }
        }

        private void HandleHeadDriftExceeded(float driftDeg)
        {
            PauseSession($"Head drift exceeded {driftDeg:F1}°");
        }

        private void HandleHeadPoseRestored()
        {
            ResumeSession();
        }

        public void StartSession(string participantId)
        {
            if (activeProfile == null)
            {
                UnityEngine.Debug.LogError("[OpenPerimeter] Cannot start session without calibration profile!");
                return;
            }

            if (stimulusPresenter != null)
            {
                stimulusPresenter.SetTestedEye(testedEye);
            }

            if (sessionCoroutine != null)
            {
                StopCoroutine(sessionCoroutine);
            }

            sessionCoroutine = StartCoroutine(RunSessionRoutine(participantId));
        }

        public void PauseSession(string reason = "")
        {
            isPaused = true;
            if (reliabilityMonitor != null)
                reliabilityMonitor.IsPaused = true;
            UnityEngine.Debug.Log($"[OpenPerimeter] Session Paused: {reason}");
        }

        public void ResumeSession()
        {
            isPaused = false;
            if (reliabilityMonitor != null)
                reliabilityMonitor.IsPaused = false;
            UnityEngine.Debug.Log("[OpenPerimeter] Session Resumed.");
        }

        public void CancelSession(string reason = "User aborted")
        {
            if (!isSessionRunning) return;

            if (sessionCoroutine != null)
            {
                StopCoroutine(sessionCoroutine);
                sessionCoroutine = null;
            }

            isSessionRunning = false;
            isPaused = false;

            if (stimulusPresenter != null)
            {
                stimulusPresenter.ClearToBackground();
            }

            UnityEngine.Debug.LogWarning($"[OpenPerimeter] Session cancelled: {reason}. Record NOT saved as complete examination.");
            OnSessionInterrupted?.Invoke(reason);
        }

        private IEnumerator RunSessionRoutine(string participantId)
        {
            isSessionRunning = true;
            isPaused = false;
            presentationCounter = 0;
            reliabilityMonitor.Reset();
            presentationEvents = new List<StoredPresentationEvent>();

            // 1. Generate grid points
            activePoints = (patternType == TestPatternType.Pattern24_2)
                ? TestPatternDefinition.Generate24_2(testedEye)
                : TestPatternDefinition.Generate10_2(testedEye);

            // Filter out blind-spot location points from active testing
            var testPoints = activePoints.FindAll(p => !p.isBlindSpotLocation);

            // Assign GCC sectors
            foreach (var pt in testPoints)
            {
                pt.gccSector = sectorMapper.GetSectorForPoint(pt.xDeg, pt.yDeg);
            }

            // 2. Initialize threshold strategies
            strategyMap = new Dictionary<int, IThresholdStrategy>();
            foreach (var pt in testPoints)
            {
                IThresholdStrategy strat = useZestStrategy 
                    ? (IThresholdStrategy)new ZestStrategy() 
                    : new Staircase42Strategy();

                strat.Initialize(pt.index, pt.xDeg, pt.yDeg, 28.0f, 6.5f);
                strategyMap[pt.index] = strat;
            }

            Stopwatch sessionTimer = Stopwatch.StartNew();

            // 3. Main testing loop: iterate until all test points are complete
            while (HasIncompletePoints(testPoints))
            {
                while (isPaused) yield return new WaitForSeconds(0.2f);

                // Catch trials: Heijl-Krakau blind spot (every 15) and False Positive (every 20)
                if (presentationCounter > 0 && presentationCounter % 15 == 0)
                {
                    yield return PresentBlindSpotCatchTrialRoutine();
                }
                else if (presentationCounter > 0 && presentationCounter % 20 == 0)
                {
                    yield return PresentFalsePositiveCatchTrialRoutine();
                }

                var incompleteList = testPoints.FindAll(p => !strategyMap[p.index].IsComplete);
                if (incompleteList.Count == 0) break;

                PatternPoint activePt = incompleteList[UnityEngine.Random.Range(0, incompleteList.Count)];
                var engine = strategyMap[activePt.index];

                float stimulusDb = engine.GetNextStimulusDb();
                PresentationResult presResult = null;

                yield return stimulusPresenter.PresentStimulusRoutine(
                    activePt.index,
                    activePt.xDeg,
                    activePt.yDeg,
                    stimulusDb,
                    GoldmannSize.SizeIII,
                    (res) => presResult = res);

                if (presResult != null)
                {
                    engine.ProcessResponse(presResult.userResponded, presResult.reactionTimeMs);
                    presentationCounter++;

                    presentationEvents.Add(new StoredPresentationEvent
                    {
                        presentationId = presentationCounter,
                        pointIndex = activePt.index,
                        stimulusDb = stimulusDb,
                        stimulusCdm2 = presResult.stimulusCdm2,
                        userResponded = presResult.userResponded,
                        reactionTimeMs = presResult.reactionTimeMs,
                        catchTrialType = "NONE",
                        timestampUnixMs = presResult.timestampUnixMs
                    });

                    // Update progress
                    int completedCount = testPoints.FindAll(p => strategyMap[p.index].IsComplete).Count;
                    OnSessionProgress?.Invoke(completedCount / (float)testPoints.Count);
                }

                // Random inter-stimulus interval (600 to 900 ms)
                yield return new WaitForSeconds(UnityEngine.Random.Range(0.6f, 0.9f));
            }

            sessionTimer.Stop();

            // 4. Finalize results
            var finalPointResults = new List<ThresholdPointResult>();
            var storedPoints = new List<StoredTestPoint>();

            foreach (var pt in testPoints)
            {
                var result = strategyMap[pt.index].GetFinalResult();
                finalPointResults.Add(result);

                storedPoints.Add(new StoredTestPoint
                {
                    pointIndex = pt.index,
                    xDeg = pt.xDeg,
                    yDeg = pt.yDeg,
                    thresholdDb = result.estimatedThresholdDb,
                    thresholdHfaEquivDb = activeProfile.DeviceDbToHfaDb(result.estimatedThresholdDb),
                    censoringStatus = result.censoringStatus.ToString().ToUpper(),
                    presentationCount = result.presentationCount,
                    stimulusSize = "SIZE_III",
                    gccSector = pt.gccSector
                });
            }

            // 5. Global Indices
            var globalIndices = deviationEngine.CalculateGlobalIndices(finalPointResults);

            // 6. Assemble Session Record
            string sessionId = $"SES-{participantId}-{DateTime.UtcNow:yyyyMMddHHmmss}";
            var sessionRecord = new StoredSessionRecord
            {
                sessionId = sessionId,
                participantId = participantId,
                timestampUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                testedEye = testedEye.ToString(),
                testPattern = (patternType == TestPatternType.Pattern24_2) ? "24-2" : "10-2",
                algorithm = useZestStrategy ? "ZEST" : "STAIRCASE_4_2",
                stimulusSize = "SIZE_III",
                deviceModel = activeProfile.device_profile.model,
                viewerModel = activeProfile.viewer_profile.model,
                calibrationId = activeProfile.calibration_id,
                isCalibrated = activeProfile.VerifySignature("OpenPerimeter-GlaucomaStudy-2026"),
                testDurationSec = sessionTimer.Elapsed.TotalSeconds,
                reliability = reliabilityMonitor.GetSummary(),
                meanDeviationDb = globalIndices.meanDeviationDb,
                patternStandardDeviationDb = globalIndices.patternStandardDeviationDb,
                isNormsValidated = globalIndices.isNormsValidated,
                normsDisclaimer = globalIndices.normsDisclaimer,
                points = storedPoints,
                presentationLog = presentationEvents
            };

            // 7. Persist & Export
            storageService.SaveTestSession(sessionRecord);
            string exportDir = Path.Combine(Application.persistentDataPath, "exports");
            StudyExportService.SaveExportFiles(sessionRecord, exportDir);

            isSessionRunning = false;
            sessionCoroutine = null;
            OnSessionCompleted?.Invoke(sessionRecord);
        }

        private IEnumerator PresentBlindSpotCatchTrialRoutine()
        {
            Vector2 blindSpotCenter = TestPatternDefinition.GetPhysiologicalBlindSpotCenter(testedEye);
            PresentationResult presResult = null;

            yield return stimulusPresenter.PresentStimulusRoutine(
                -999, // Special blind spot ID
                blindSpotCenter.x,
                blindSpotCenter.y,
                10.0f, // Bright stimulus into physiological blind spot
                GoldmannSize.SizeIII,
                (res) => presResult = res);

            if (presResult != null)
            {
                reliabilityMonitor.RecordBlindSpotTrial(presResult.userResponded);
                presentationCounter++;
                presentationEvents.Add(new StoredPresentationEvent
                {
                    presentationId = presentationCounter,
                    pointIndex = -999,
                    stimulusDb = 10.0f,
                    stimulusCdm2 = presResult.stimulusCdm2,
                    userResponded = presResult.userResponded,
                    reactionTimeMs = presResult.reactionTimeMs,
                    catchTrialType = "BLIND_SPOT",
                    timestampUnixMs = presResult.timestampUnixMs
                });
            }
        }

        private IEnumerator PresentFalsePositiveCatchTrialRoutine()
        {
            // Shutter interval with zero stimulus
            yield return new WaitForSeconds(0.200f);
            bool responded = false;
            float waitSec = 1.0f;
            float elapsed = 0f;
            PerimetryInputHandler.ResetDebounce();

            while (elapsed < waitSec)
            {
                if (PerimetryInputHandler.CheckResponse(out _))
                {
                    responded = true;
                    break;
                }
                yield return null;
                elapsed += Time.deltaTime;
            }

            reliabilityMonitor.RecordFalsePositiveTrial(responded);
            presentationCounter++;
            presentationEvents.Add(new StoredPresentationEvent
            {
                presentationId = presentationCounter,
                pointIndex = -998,
                stimulusDb = -1.0f,
                stimulusCdm2 = 0f,
                userResponded = responded,
                reactionTimeMs = responded ? elapsed * 1000.0 : -1.0,
                catchTrialType = "FALSE_POSITIVE",
                timestampUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            });
        }

        private bool HasIncompletePoints(List<PatternPoint> points)
        {
            foreach (var pt in points)
            {
                if (!strategyMap[pt.index].IsComplete) return true;
            }
            return false;
        }

        private void OnDestroy()
        {
            if (fixationTarget != null)
            {
                fixationTarget.OnHeadDriftExceeded -= HandleHeadDriftExceeded;
                fixationTarget.OnHeadPoseRestored -= HandleHeadPoseRestored;
            }

            if (sessionCoroutine != null)
            {
                StopCoroutine(sessionCoroutine);
                sessionCoroutine = null;
            }
        }
    }
}
