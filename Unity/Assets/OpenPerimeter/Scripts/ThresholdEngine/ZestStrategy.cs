using System;
using UnityEngine;
using OpenPerimeter.Calibration;
using OpenPerimeter.Core;

namespace OpenPerimeter.ThresholdEngine
{
    public class ZestStrategy : IThresholdStrategy
    {
        // Sensitivity domain: -5 dB to 40 dB in 0.5 dB steps
        private const float DOMAIN_MIN_DB = -5.0f;
        private const float DOMAIN_MAX_DB = 40.0f;
        private const float DOMAIN_STEP_DB = 0.5f;

        private readonly float[] domain;
        private readonly float[] pdf;

        // Psychometric parameters
        private readonly float gamma; // False positive guess rate (default 0.03)
        private readonly float lambda; // False negative lapse rate (default 0.03)
        private readonly float psychometricSlope; // Spread S in dB (default 1.2)

        // Stopping criteria
        private readonly float stopSdDb; // Stop if posterior SD <= 1.5 dB
        private readonly int minPresentations; // 2
        private readonly int maxPresentations; // 5

        private int pointIndex;
        private float xDeg;
        private float yDeg;
        private int presentationCount;
        private bool isComplete;
        private float lastStimulusDb;
        private int consecutiveMissesAtZeroDb;

        public int PointIndex => pointIndex;
        public float XDeg => xDeg;
        public float YDeg => yDeg;
        public bool IsComplete => isComplete;
        public int PresentationCount => presentationCount;

        public ZestStrategy(
            float gamma = 0.03f,
            float lambda = 0.03f,
            float psychometricSlope = 1.2f,
            float stopSdDb = 1.5f,
            int minPresentations = 2,
            int maxPresentations = 5)
        {
            this.gamma = gamma;
            this.lambda = lambda;
            this.psychometricSlope = psychometricSlope;
            this.stopSdDb = stopSdDb;
            this.minPresentations = minPresentations;
            this.maxPresentations = maxPresentations;

            int stepCount = Mathf.RoundToInt((DOMAIN_MAX_DB - DOMAIN_MIN_DB) / DOMAIN_STEP_DB) + 1;
            domain = new float[stepCount];
            pdf = new float[stepCount];

            for (int i = 0; i < stepCount; i++)
            {
                domain[i] = DOMAIN_MIN_DB + i * DOMAIN_STEP_DB;
            }
        }

        private int consecutiveMisses = 0;

        public void Initialize(int pointIndex, float xDeg, float yDeg, float priorMeanDb, float priorSdDb)
        {
            this.pointIndex = pointIndex;
            this.xDeg = xDeg;
            this.yDeg = yDeg;
            this.presentationCount = 0;
            this.isComplete = false;
            this.consecutiveMisses = 0;
            this.consecutiveMissesAtZeroDb = 0;

            // Initialize prior PDF with Gaussian
            float sum = 0f;
            float twoSigmaSq = 2.0f * priorSdDb * priorSdDb;

            for (int i = 0; i < domain.Length; i++)
            {
                float diff = domain[i] - priorMeanDb;
                pdf[i] = Mathf.Exp(-(diff * diff) / twoSigmaSq);
                sum += pdf[i];
            }

            // Normalize
            if (sum > 1e-6f)
            {
                for (int i = 0; i < pdf.Length; i++) pdf[i] /= sum;
            }
            else
            {
                float uniform = 1.0f / pdf.Length;
                for (int i = 0; i < pdf.Length; i++) pdf[i] = uniform;
            }
        }

        /// <summary>
        /// Next stimulus level is chosen as the mean of the current posterior PDF.
        /// If 3 consecutive misses occur, probes immediately at 0 dB (floor scotoma probe).
        /// </summary>
        public float GetNextStimulusDb()
        {
            if (consecutiveMisses >= 3)
            {
                lastStimulusDb = 0.0f;
                return 0.0f;
            }

            float mean = ComputeMean();
            lastStimulusDb = Mathf.Clamp(Mathf.Round(mean * 2.0f) / 2.0f, 0.0f, 35.0f);
            return lastStimulusDb;
        }

        /// <summary>
        /// Updates the posterior PDF using Bayes' theorem with the cumulative Gaussian psychometric function.
        /// </summary>
        public void ProcessResponse(bool userResponded, double reactionTimeMs)
        {
            presentationCount++;

            if (userResponded)
            {
                consecutiveMisses = 0;
            }
            else
            {
                consecutiveMisses++;
                if (lastStimulusDb <= 0.5f)
                {
                    consecutiveMissesAtZeroDb++;
                }
            }

            float sum = 0f;
            for (int i = 0; i < domain.Length; i++)
            {
                float t = domain[i];
                float pSeen = PsychometricFunction(lastStimulusDb, t);
                float likelihood = userResponded ? pSeen : (1.0f - pSeen);

                pdf[i] *= likelihood;
                sum += pdf[i];
            }

            // Normalize posterior
            if (sum > 1e-9f)
            {
                for (int i = 0; i < pdf.Length; i++) pdf[i] /= sum;
            }

            // Evaluate stopping rules
            float currentSd = ComputeStandardDeviation();
            if (presentationCount >= minPresentations)
            {
                if (currentSd <= stopSdDb || presentationCount >= maxPresentations)
                {
                    isComplete = true;
                }
                // Fast floor termination: if patient misses 0 dB, scotoma is at or below device floor
                if (consecutiveMissesAtZeroDb >= 1 && presentationCount >= 3)
                {
                    isComplete = true;
                }
            }
        }

        public ThresholdPointResult GetFinalResult()
        {
            float finalMean = ComputeMean();
            float finalSd = ComputeStandardDeviation();

            CensoringStatus censoring = CensoringStatus.Measured;
            if (consecutiveMissesAtZeroDb >= 1 || finalMean < 0.5f)
            {
                censoring = CensoringStatus.FloorCensored;
                finalMean = 0.0f; // Documented floor clamp with explicit flag
            }
            else if (finalMean > 35.0f)
            {
                censoring = CensoringStatus.CeilingCensored;
            }

            return new ThresholdPointResult
            {
                pointIndex = pointIndex,
                xDeg = xDeg,
                yDeg = yDeg,
                estimatedThresholdDb = finalMean,
                posteriorSdDb = finalSd,
                censoringStatus = censoring,
                presentationCount = presentationCount,
                isComplete = isComplete,
                stimulusSize = GoldmannSize.SizeIII
            };
        }

        private float ComputeMean()
        {
            float mean = 0f;
            for (int i = 0; i < domain.Length; i++)
            {
                mean += domain[i] * pdf[i];
            }
            return mean;
        }

        private float ComputeStandardDeviation()
        {
            float mean = ComputeMean();
            float variance = 0f;
            for (int i = 0; i < domain.Length; i++)
            {
                float diff = domain[i] - mean;
                variance += (diff * diff) * pdf[i];
            }
            return Mathf.Sqrt(Mathf.Max(0f, variance));
        }

        /// <summary>
        /// Cumulative standard normal frequency-of-seeing function:
        /// Stimulus is seen when it is brighter than threshold (thresholdDb >= stimulusDb).
        /// P(Seen | x, T) = gamma + (1 - gamma - lambda) * Phi((T - x) / S)
        /// </summary>
        private float PsychometricFunction(float stimulusDb, float thresholdDb)
        {
            float z = (thresholdDb - stimulusDb) / psychometricSlope;
            float phi = CumulativeNormal(z);
            return gamma + (1.0f - gamma - lambda) * phi;
        }

        private static float CumulativeNormal(float z)
        {
            // Numerical approximation of standard normal CDF: 0.5 * (1 + erf(z / sqrt(2)))
            return 0.5f * (1.0f + Erf(z / 1.41421356f));
        }

        private static float Erf(float x)
        {
            // Abramowitz and Stegun formula 7.1.26 (max error 1.5e-7)
            float sign = x >= 0 ? 1.0f : -1.0f;
            x = Mathf.Abs(x);
            float p = 0.3275911f;
            float a1 = 0.254829592f;
            float a2 = -0.284496736f;
            float a3 = 1.421413741f;
            float a4 = -1.453152027f;
            float a5 = 1.061405429f;

            float t = 1.0f / (1.0f + p * x);
            float poly = ((((a5 * t + a4) * t + a3) * t + a2) * t + a1) * t;
            return sign * (1.0f - poly * Mathf.Exp(-x * x));
        }
    }
}
