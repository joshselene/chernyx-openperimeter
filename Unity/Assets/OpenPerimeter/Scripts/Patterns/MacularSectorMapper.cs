using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace OpenPerimeter.Patterns
{
    [Serializable]
    public class SectorCoordinate
    {
        public float x;
        public float y;
    }

    [Serializable]
    public class MacularSectorItem
    {
        public string sector_id;
        public string sector_name;
        public string corresponds_to_oct;
        public List<SectorCoordinate> points = new List<SectorCoordinate>();
    }

    [Serializable]
    public class MacularSectorMapFile
    {
        public string map_name;
        public string version;
        public string description;
        public string grid_type;
        public List<MacularSectorItem> sectors = new List<MacularSectorItem>();
    }

    public class MacularSectorMapper
    {
        private MacularSectorMapFile activeMap;
        private readonly Dictionary<string, string> pointToSectorLookup = new Dictionary<string, string>();

        public bool IsLoaded => activeMap != null;
        public string MapName => activeMap != null ? activeMap.map_name : "None";

        public void LoadFromFile(string jsonPath)
        {
            if (!File.Exists(jsonPath))
            {
                UnityEngine.Debug.LogWarning($"[OpenPerimeter] sector_map.json not found at {jsonPath}. Macular GCC mapping disabled.");
                return;
            }

            string json = File.ReadAllText(jsonPath);
            LoadFromJson(json);
        }

        public void LoadFromJson(string json)
        {
            activeMap = JsonUtility.FromJson<MacularSectorMapFile>(json);
            pointToSectorLookup.Clear();

            if (activeMap != null && activeMap.sectors != null)
            {
                foreach (var sector in activeMap.sectors)
                {
                    foreach (var pt in sector.points)
                    {
                        string key = FormatCoordKey(pt.x, pt.y);
                        pointToSectorLookup[key] = sector.sector_id;
                    }
                }
            }
        }

        /// <summary>
        /// Looks up corresponding GCC sector for a visual field coordinate.
        /// Returns empty string if point is not mapped.
        /// </summary>
        public string GetSectorForPoint(float xDeg, float yDeg)
        {
            string key = FormatCoordKey(xDeg, yDeg);
            if (pointToSectorLookup.TryGetValue(key, out string sectorId))
            {
                return sectorId;
            }
            return "";
        }

        private static string FormatCoordKey(float x, float y)
        {
            return $"{x:F1}_{y:F1}";
        }
    }
}
