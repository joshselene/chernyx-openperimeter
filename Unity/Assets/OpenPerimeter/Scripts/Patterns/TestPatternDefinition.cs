using System;
using System.Collections.Generic;
using UnityEngine;

namespace OpenPerimeter.Patterns
{
    public enum EyeSide
    {
        OD = 0, // Right Eye
        OS = 1  // Left Eye
    }

    public enum TestPatternType
    {
        Pattern24_2 = 0,
        Pattern10_2 = 1
    }

    [Serializable]
    public class PatternPoint
    {
        public int index;
        public float xDeg;
        public float yDeg;
        public bool isBlindSpotLocation;
        public string gccSector = "";
    }

    public static class TestPatternDefinition
    {
        /// <summary>
        /// Generates the standard Humphrey 24-2 test grid (54 active test locations).
        /// 6-degree Cartesian spacing with 3-degree offset from horizontal and vertical meridians.
        /// Accounts for eye side (OD vs OS) by mirroring the nasal step and physiological blind spot.
        /// </summary>
        public static List<PatternPoint> Generate24_2(EyeSide eye)
        {
            var points = new List<PatternPoint>();

            // Baseline coordinates for Right Eye (OD):
            // Temporal is positive x (+), Nasal is negative x (-)
            // Physiological blind spot for OD is located at (+15, -3) and (+15, +3).
            (int x, int y)[] rawCoordsOD = new (int, int)[]
            {
                // y = +21
                (-9, 21), (-3, 21), (3, 21), (9, 21),
                // y = +15
                (-15, 15), (-9, 15), (-3, 15), (3, 15), (9, 15), (15, 15),
                // y = +9
                (-21, 9), (-15, 9), (-9, 9), (-3, 9), (3, 9), (9, 9), (15, 9), (21, 9),
                // y = +3
                (-27, 3), (-21, 3), (-15, 3), (-9, 3), (-3, 3), (3, 3), (9, 3), (15, 3), (21, 3),
                // y = -3
                (-27, -3), (-21, -3), (-15, -3), (-9, -3), (-3, -3), (3, -3), (9, -3), (15, -3), (21, -3),
                // y = -9
                (-21, -9), (-15, -9), (-9, -9), (-3, -9), (3, -9), (9, -9), (15, -9), (21, -9),
                // y = -15
                (-15, -15), (-9, -15), (-3, -15), (3, -15), (9, -15), (15, -15),
                // y = -21
                (-9, -21), (-3, -21), (3, -21), (9, -21)
            };

            int idx = 0;
            foreach (var (x, y) in rawCoordsOD)
            {
                float finalX = (eye == EyeSide.OD) ? (float)x : -(float)x; // OS mirrors horizontal axis
                float finalY = (float)y;

                // Check if this point falls on the physiological blind spot:
                // For OD: (+15, -3) or (+15, +3). For OS: (-15, -3) or (-15, +3).
                bool isBlindSpot = (eye == EyeSide.OD && (x == 15 && (y == -3 || y == 3))) ||
                                   (eye == EyeSide.OS && (x == 15 && (y == -3 || y == 3)));

                points.Add(new PatternPoint
                {
                    index = idx++,
                    xDeg = finalX,
                    yDeg = finalY,
                    isBlindSpotLocation = isBlindSpot,
                    gccSector = ""
                });
            }

            return points;
        }

        /// <summary>
        /// Generates the standard 10-2 test grid (68 points).
        /// 2-degree Cartesian spacing with 1-degree offset from horizontal and vertical meridians across central ±10 degrees.
        /// </summary>
        public static List<PatternPoint> Generate10_2(EyeSide eye)
        {
            var points = new List<PatternPoint>();
            int idx = 0;

            // 10-2 covers x and y from -9 to +9 in steps of 2:
            // Offsets: -9, -7, -5, -3, -1, +1, +3, +5, +7, +9
            int[] grid1D = new int[] { -9, -7, -5, -3, -1, 1, 3, 5, 7, 9 };

            foreach (int y in grid1D)
            {
                foreach (int x in grid1D)
                {
                    // 10-2 excludes outer corner points to approximate a circular 10-degree radius boundary
                    float r2 = x * x + y * y;
                    if (r2 <= 100.0f) // r <= 10.0 degrees
                    {
                        float finalX = (eye == EyeSide.OD) ? (float)x : -(float)x;
                        points.Add(new PatternPoint
                        {
                            index = idx++,
                            xDeg = finalX,
                            yDeg = (float)y,
                            isBlindSpotLocation = false,
                            gccSector = ""
                        });
                    }
                }
            }

            return points;
        }

        /// <summary>
        /// Returns the standard Heijl-Krakau physiological blind spot presentation coordinate for fixation catch trials.
        /// </summary>
        public static Vector2 GetPhysiologicalBlindSpotCenter(EyeSide eye)
        {
            // Center of physiological blind spot is approximately 15.0 deg temporal, 1.5 - 2.0 deg inferior
            float x = (eye == EyeSide.OD) ? 15.0f : -15.0f;
            float y = -2.0f;
            return new Vector2(x, y);
        }
    }
}
