using System;
using UnityEngine;
using OpenPerimeter.Patterns;

namespace OpenPerimeter.Reliability
{
    public enum CatchTrialType
    {
        None = 0,
        BlindSpotFixation = 1,
        FalsePositive = 2,
        FalseNegative = 3
    }

    [Serializable]
    public class ReliabilitySummary
    {
        public int fixationLossCount;
        public int fixationLossTotal;
        public float fixationLossPercent;

        public int falsePositiveCount;
        public int falsePositiveTotal;
        public float falsePositivePercent;

        public int falseNegativeCount;
        public int falseNegativeTotal;
        public float falseNegativePercent;

        public bool isReliable;
        public string failureReason = "";
    }

    public class ReliabilityMonitor
    {
        // Study criteria limits
        public const float MAX_ALLOWED_FIXATION_LOSS_PCT = 20.0f;
        public const float MAX_ALLOWED_FALSE_POSITIVE_PCT = 15.0f;
        public const float MAX_ALLOWED_FALSE_NEGATIVE_PCT = 15.0f;

        private int flCount = 0;
        private int flTotal = 0;

        private int fpCount = 0;
        private int fpTotal = 0;

        private int fnCount = 0;
        private int fnTotal = 0;

        private bool isPaused = false;

        public int FixationLossCount => flCount;
        public int FixationLossTotal => flTotal;
        public int FalsePositiveCount => fpCount;
        public int FalsePositiveTotal => fpTotal;
        public int FalseNegativeCount => fnCount;
        public int FalseNegativeTotal => fnTotal;
        public bool IsPaused { get => isPaused; set => isPaused = value; }

        public void Reset()
        {
            flCount = 0;
            flTotal = 0;
            fpCount = 0;
            fpTotal = 0;
            fnCount = 0;
            fnTotal = 0;
            isPaused = false;
        }

        /// <summary>
        /// Records result of Heijl-Krakau blind spot trial.
        /// A click inside the blind spot indicates fixation loss.
        /// </summary>
        public void RecordBlindSpotTrial(bool subjectResponded)
        {
            flTotal++;
            if (subjectResponded)
            {
                flCount++;
            }
        }

        /// <summary>
        /// Records result of False Positive catch trial (zero stimulus presented).
        /// A click indicates false positive guessing.
        /// </summary>
        public void RecordFalsePositiveTrial(bool subjectResponded)
        {
            fpTotal++;
            if (subjectResponded)
            {
                fpCount++;
            }
        }

        /// <summary>
        /// Records result of False Negative catch trial (+9 dB supra-threshold stimulus).
        /// Failure to click at a known seeing location indicates false negative inattention.
        /// </summary>
        public void RecordFalseNegativeTrial(bool subjectResponded)
        {
            fnTotal++;
            if (!subjectResponded)
            {
                fnCount++;
            }
        }

        public float GetFixationLossPercent()
        {
            if (flTotal == 0) return 0.0f;
            return (flCount / (float)flTotal) * 100.0f;
        }

        public float GetFalsePositivePercent()
        {
            if (fpTotal == 0) return 0.0f;
            return (fpCount / (float)fpTotal) * 100.0f;
        }

        public float GetFalseNegativePercent()
        {
            if (fnTotal == 0) return 0.0f;
            return (fnCount / (float)fnTotal) * 100.0f;
        }

        /// <summary>
        /// Evaluates pass/fail status against strict study protocol criteria.
        /// Passes if FL < 20%, FP < 15%, FN < 15%.
        /// </summary>
        public bool IsTestReliable(out string failureReason)
        {
            float flPct = GetFixationLossPercent();
            float fpPct = GetFalsePositivePercent();
            float fnPct = GetFalseNegativePercent();

            failureReason = "";
            bool pass = true;

            if (flTotal >= 3 && flPct >= MAX_ALLOWED_FIXATION_LOSS_PCT)
            {
                pass = false;
                failureReason += $"Fixation Loss {flPct:F1}% >= {MAX_ALLOWED_FIXATION_LOSS_PCT}%. ";
            }

            if (fpTotal >= 3 && fpPct >= MAX_ALLOWED_FALSE_POSITIVE_PCT)
            {
                pass = false;
                failureReason += $"False Positive {fpPct:F1}% >= {MAX_ALLOWED_FALSE_POSITIVE_PCT}%. ";
            }

            if (fnTotal >= 3 && fnPct >= MAX_ALLOWED_FALSE_NEGATIVE_PCT)
            {
                pass = false;
                failureReason += $"False Negative {fnPct:F1}% >= {MAX_ALLOWED_FALSE_NEGATIVE_PCT}%. ";
            }

            return pass;
        }

        public ReliabilitySummary GetSummary()
        {
            bool reliable = IsTestReliable(out string reason);
            return new ReliabilitySummary
            {
                fixationLossCount = flCount,
                fixationLossTotal = flTotal,
                fixationLossPercent = GetFixationLossPercent(),

                falsePositiveCount = fpCount,
                falsePositiveTotal = fpTotal,
                falsePositivePercent = GetFalsePositivePercent(),

                falseNegativeCount = fnCount,
                falseNegativeTotal = fnTotal,
                falseNegativePercent = GetFalseNegativePercent(),

                isReliable = reliable,
                failureReason = reason
            };
        }
    }
}
