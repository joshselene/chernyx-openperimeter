using System;
using UnityEngine;
using OpenPerimeter.Calibration;
using OpenPerimeter.Core;

namespace OpenPerimeter.ThresholdEngine
{
    public class Staircase42Strategy : IThresholdStrategy
    {
        private int pointIndex;
        private float xDeg;
        private float yDeg;
        private int presentationCount;
        private bool isComplete;

        private float currentStimulusDb;
        private float currentStepSizeDb = 4.0f;
        private int reversalCount = 0;
        private int lastResponseDirection = 0; // +1 if seen (went dimmer), -1 if not seen (went brighter), 0 uninitialized
        private float lastSeenDb = -1.0f;
        private float lastUnseenDb = -1.0f;
        private int consecutiveMissesAtZeroDb = 0;
        private readonly int maxPresentations = 8;

        public int PointIndex => pointIndex;
        public float XDeg => xDeg;
        public float YDeg => yDeg;
        public bool IsComplete => isComplete;
        public int PresentationCount => presentationCount;

        public void Initialize(int pointIndex, float xDeg, float yDeg, float priorMeanDb, float priorSdDb)
        {
            this.pointIndex = pointIndex;
            this.xDeg = xDeg;
            this.yDeg = yDeg;
            this.presentationCount = 0;
            this.isComplete = false;
            this.reversalCount = 0;
            this.currentStepSizeDb = 4.0f;
            this.lastResponseDirection = 0;
            this.lastSeenDb = -1.0f;
            this.lastUnseenDb = -1.0f;
            this.consecutiveMissesAtZeroDb = 0;

            // Start at closest even dB integer to expected normal
            this.currentStimulusDb = Mathf.Clamp(Mathf.Round(priorMeanDb / 2.0f) * 2.0f, 4.0f, 30.0f);
        }

        public float GetNextStimulusDb()
        {
            return currentStimulusDb;
        }

        public void ProcessResponse(bool userResponded, double reactionTimeMs)
        {
            presentationCount++;

            int responseDirection;
            if (userResponded)
            {
                lastSeenDb = currentStimulusDb;
                responseDirection = +1; // Step up (dimmer)
            }
            else
            {
                lastUnseenDb = currentStimulusDb;
                responseDirection = -1; // Step down (brighter)
                if (currentStimulusDb <= 0.0f)
                {
                    consecutiveMissesAtZeroDb++;
                }
            }

            // Check for reversal
            if (lastResponseDirection != 0 && responseDirection != lastResponseDirection)
            {
                reversalCount++;
                currentStepSizeDb = 2.0f; // Switch from 4 dB to 2 dB step
            }
            lastResponseDirection = responseDirection;

            // Check termination conditions:
            // 1. Two reversals achieved
            if (reversalCount >= 2)
            {
                isComplete = true;
                return;
            }

            // 2. Scotoma floor: missed 0 dB twice
            if (consecutiveMissesAtZeroDb >= 2)
            {
                isComplete = true;
                return;
            }

            // 3. Max presentations reached
            if (presentationCount >= maxPresentations)
            {
                isComplete = true;
                return;
            }

            // Calculate next presentation level
            float nextDb = currentStimulusDb + (responseDirection * currentStepSizeDb);
            currentStimulusDb = Mathf.Clamp(nextDb, 0.0f, 36.0f);

            // If clamped at 0 or 36 and direction continues, we terminate
            if (currentStimulusDb <= 0.0f && responseDirection == -1 && presentationCount >= 3)
            {
                isComplete = true;
            }
            else if (currentStimulusDb >= 36.0f && responseDirection == +1 && presentationCount >= 3)
            {
                isComplete = true;
            }
        }

        public ThresholdPointResult GetFinalResult()
        {
            float finalThreshold;
            if (lastSeenDb >= 0.0f && lastUnseenDb >= 0.0f)
            {
                finalThreshold = (lastSeenDb + lastUnseenDb) * 0.5f;
            }
            else if (lastSeenDb >= 0.0f)
            {
                finalThreshold = lastSeenDb;
            }
            else
            {
                finalThreshold = 0.0f;
            }

            CensoringStatus censoring = CensoringStatus.Measured;

            if (consecutiveMissesAtZeroDb >= 2 || lastSeenDb < 0.0f)
            {
                censoring = CensoringStatus.FloorCensored;
                finalThreshold = 0.0f;
            }
            else if (finalThreshold >= 34.0f)
            {
                censoring = CensoringStatus.CeilingCensored;
            }

            return new ThresholdPointResult
            {
                pointIndex = pointIndex,
                xDeg = xDeg,
                yDeg = yDeg,
                estimatedThresholdDb = finalThreshold,
                posteriorSdDb = (reversalCount >= 2) ? 1.41f : 2.50f, // Empirical staircase precision
                censoringStatus = censoring,
                presentationCount = presentationCount,
                isComplete = isComplete,
                stimulusSize = GoldmannSize.SizeIII
            };
        }
    }
}
