using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using OpenPerimeter.ThresholdEngine;

namespace OpenPerimeter.Core
{
    [Serializable]
    public class NormativePoint
    {
        public float x;
        public float y;
        public float expected_db;
        public float sd_db;
    }

    [Serializable]
    public class NormativeAgeGroup
    {
        public int min_age;
        public int max_age;
        public List<NormativePoint> points = new List<NormativePoint>();
    }

    [Serializable]
    public class NormativeDatabaseFile
    {
        public string database_name = "Placeholder";
        public string version = "0.1.0";
        public bool is_validated = false;
        public string disclaimer = "UNVALIDATED NORMS";
        public string pattern = "24-2";
        public List<NormativeAgeGroup> age_groups = new List<NormativeAgeGroup>();
    }

    public class GlobalIndicesResult
    {
        public float meanDeviationDb;
        public float patternStandardDeviationDb;
        public bool isNormsValidated;
        public string normsDisclaimer;
        public Dictionary<int, float> totalDeviations = new Dictionary<int, float>();
        public Dictionary<int, float> patternDeviations = new Dictionary<int, float>();
    }

    public class NormativeDeviationEngine
    {
        public const string UNVALIDATED_DISCLAIMER = "UNVALIDATED NORMS - FOR TESTING PIPELINE ONLY - NOT FOR CLINICAL USE";

        private NormativeDatabaseFile loadedNorms;

        public bool IsLoaded => loadedNorms != null;
        public bool IsValidated => loadedNorms != null && loadedNorms.is_validated;

        public void LoadNormsFromFile(string jsonPath)
        {
            if (File.Exists(jsonPath))
            {
                string json = File.ReadAllText(jsonPath);
                loadedNorms = JsonUtility.FromJson<NormativeDatabaseFile>(json);
            }
            else
            {
                loadedNorms = new NormativeDatabaseFile
                {
                    database_name = "Missing_Norms",
                    is_validated = false,
                    disclaimer = UNVALIDATED_DISCLAIMER
                };
            }
        }

        public void SetNorms(NormativeDatabaseFile norms)
        {
            loadedNorms = norms;
        }

        /// <summary>
        /// Calculates Mean Deviation (MD) and Pattern Standard Deviation (PSD).
        /// Enforces "UNVALIDATED NORMS" disclaimer on all deviation outputs if not validated.
        /// </summary>
        public GlobalIndicesResult CalculateGlobalIndices(
            List<ThresholdPointResult> measuredPoints,
            int subjectAge = 50)
        {
            var result = new GlobalIndicesResult
            {
                isNormsValidated = IsValidated,
                normsDisclaimer = IsValidated ? "VALIDATED RESEARCH NORMATIVE DATABASE" : UNVALIDATED_DISCLAIMER
            };

            if (measuredPoints == null || measuredPoints.Count == 0)
            {
                return result;
            }

            // Find matching age group in norms
            NormativeAgeGroup ageGroup = null;
            if (loadedNorms != null && loadedNorms.age_groups != null)
            {
                foreach (var ag in loadedNorms.age_groups)
                {
                    if (subjectAge >= ag.min_age && subjectAge <= ag.max_age)
                    {
                        ageGroup = ag;
                        break;
                    }
                }
            }

            var tdList = new List<float>();

            foreach (var pt in measuredPoints)
            {
                float expectedNormal = 28.0f; // Baseline default if point-by-point norms unavailable

                if (ageGroup != null)
                {
                    var normPt = ageGroup.points.Find(p => Mathf.Approximately(p.x, pt.xDeg) && Mathf.Approximately(p.y, pt.yDeg));
                    if (normPt != null)
                    {
                        expectedNormal = normPt.expected_db;
                    }
                }

                // Total Deviation: Measured sensitivity - Normal sensitivity
                float td = pt.estimatedThresholdDb - expectedNormal;
                result.totalDeviations[pt.pointIndex] = td;
                tdList.Add(td);
            }

            // Mean Deviation (MD) = arithmetic average of Total Deviations
            float sumTd = 0f;
            foreach (float td in tdList) sumTd += td;
            result.meanDeviationDb = tdList.Count > 0 ? (sumTd / tdList.Count) : 0f;

            // Pattern Deviation: Correct for generalized depression / elevation (General Height)
            // GH is estimated as the 85th percentile of TD values (standard in perimetric literature)
            var sortedTd = new List<float>(tdList);
            sortedTd.Sort();
            int ghIndex = Mathf.Clamp(Mathf.RoundToInt(sortedTd.Count * 0.85f), 0, sortedTd.Count - 1);
            float generalHeight = (sortedTd.Count > 0) ? sortedTd[ghIndex] : 0f;

            var pdList = new List<float>();
            foreach (var pt in measuredPoints)
            {
                float td = result.totalDeviations[pt.pointIndex];
                float pd = td - generalHeight;
                result.patternDeviations[pt.pointIndex] = pd;
                pdList.Add(pd);
            }

            // Pattern Standard Deviation (PSD)
            float sumPd = 0f;
            foreach (float pd in pdList) sumPd += pd;
            float meanPd = pdList.Count > 0 ? (sumPd / pdList.Count) : 0f;

            float sumSqDiff = 0f;
            foreach (float pd in pdList)
            {
                float diff = pd - meanPd;
                sumSqDiff += diff * diff;
            }

            result.patternStandardDeviationDb = (pdList.Count > 1) 
                ? Mathf.Sqrt(sumSqDiff / (pdList.Count - 1)) 
                : 0f;

            return result;
        }
    }
}
