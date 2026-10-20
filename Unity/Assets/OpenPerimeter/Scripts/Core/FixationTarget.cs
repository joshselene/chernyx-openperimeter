using System;
using UnityEngine;

namespace OpenPerimeter.Core
{
    public enum FixationTargetType
    {
        CentralCross = 0,   // Standard central crosshair (0.5 deg arms)
        CentralDot = 1,     // Small central point (0.2 deg)
        DiamondFourPoint = 2 // 4-point diamond for subjects with central scotoma
    }

    public class FixationTarget : MonoBehaviour
    {
        [Header("Fixation Appearance")]
        [SerializeField] private FixationTargetType targetType = FixationTargetType.CentralCross;
        [SerializeField] private Color targetColor = new Color(0.85f, 0.70f, 0.10f, 1.0f); // Warm amber / low-photopic yellow
        [SerializeField] private float sizeDegrees = 0.5f;

        [Header("Head Drift Monitoring")]
        [SerializeField] private bool enableHeadDriftMonitoring = true;
        [SerializeField] private float maxAllowedDriftDegrees = 3.0f;
        [SerializeField] private float reacquireToleranceDegrees = 1.5f;

        private Quaternion baselineOrientation = Quaternion.identity;
        private bool isBaselineSet = false;
        private bool isDriftExceeded = false;
        private float currentDriftDegrees = 0f;

        public event Action<float> OnHeadDriftExceeded;
        public event Action OnHeadPoseRestored;

        public float CurrentDriftDegrees => currentDriftDegrees;
        public bool IsDriftExceeded => isDriftExceeded;
        public FixationTargetType TargetType { get => targetType; set => targetType = value; }
        public float MaxAllowedDriftDegrees { get => maxAllowedDriftDegrees; set => maxAllowedDriftDegrees = value; }

        private void Awake()
        {
            if (SystemInfo.supportsGyroscope)
            {
                Input.gyro.enabled = true;
                Input.gyro.updateInterval = 0.016f; // 60 Hz updates
            }
        }

        private void Start()
        {
            ResetBaselinePose();
        }

        private void Update()
        {
            if (!enableHeadDriftMonitoring) return;

            UpdateHeadPoseMonitoring();
        }

        /// <summary>
        /// Calibrates the current head orientation as the zero-drift reference pose.
        /// </summary>
        public void ResetBaselinePose()
        {
            if (SystemInfo.supportsGyroscope)
            {
                baselineOrientation = GyroAttitudeToUnity(Input.gyro.attitude);
            }
            else
            {
                baselineOrientation = transform.rotation;
            }
            isBaselineSet = true;
            isDriftExceeded = false;
            currentDriftDegrees = 0f;
        }

        private void UpdateHeadPoseMonitoring()
        {
            if (!isBaselineSet) return;

            Quaternion currentAttitude = SystemInfo.supportsGyroscope
                ? GyroAttitudeToUnity(Input.gyro.attitude)
                : transform.rotation;

            // Angular distance between baseline and current attitude
            currentDriftDegrees = Quaternion.Angle(baselineOrientation, currentAttitude);

            if (!isDriftExceeded && currentDriftDegrees > maxAllowedDriftDegrees)
            {
                isDriftExceeded = true;
                OnHeadDriftExceeded?.Invoke(currentDriftDegrees);
            }
            else if (isDriftExceeded && currentDriftDegrees <= reacquireToleranceDegrees)
            {
                isDriftExceeded = false;
                OnHeadPoseRestored?.Invoke();
            }
        }

        /// <summary>
        /// Renders procedural fixation mark on display texture or mesh.
        /// </summary>
        public void DrawFixationMark(Texture2D targetTexture, Vector2 centerPixel, float pixelsPerDegree)
        {
            float halfSizePx = (sizeDegrees * pixelsPerDegree) * 0.5f;
            int cx = Mathf.RoundToInt(centerPixel.x);
            int cy = Mathf.RoundToInt(centerPixel.y);

            switch (targetType)
            {
                case FixationTargetType.CentralCross:
                    int armPx = Mathf.RoundToInt(halfSizePx);
                    for (int x = cx - armPx; x <= cx + armPx; x++)
                    {
                        if (x >= 0 && x < targetTexture.width && cy >= 0 && cy < targetTexture.height)
                            targetTexture.SetPixel(x, cy, targetColor);
                    }
                    for (int y = cy - armPx; y <= cy + armPx; y++)
                    {
                        if (cx >= 0 && cx < targetTexture.width && y >= 0 && y < targetTexture.height)
                            targetTexture.SetPixel(cx, y, targetColor);
                    }
                    break;

                case FixationTargetType.CentralDot:
                    int radiusPx = Mathf.Max(1, Mathf.RoundToInt(halfSizePx * 0.4f));
                    for (int x = -radiusPx; x <= radiusPx; x++)
                    {
                        for (int y = -radiusPx; y <= radiusPx; y++)
                        {
                            if (x * x + y * y <= radiusPx * radiusPx)
                            {
                                int px = cx + x;
                                int py = cy + y;
                                if (px >= 0 && px < targetTexture.width && py >= 0 && py < targetTexture.height)
                                    targetTexture.SetPixel(px, py, targetColor);
                            }
                        }
                    }
                    break;

                case FixationTargetType.DiamondFourPoint:
                    float diamondOffset = halfSizePx * 1.5f;
                    int dotRadius = Mathf.Max(1, Mathf.RoundToInt(halfSizePx * 0.25f));
                    Vector2[] dots = new Vector2[]
                    {
                        new Vector2(cx, cy + diamondOffset), // top
                        new Vector2(cx, cy - diamondOffset), // bottom
                        new Vector2(cx - diamondOffset, cy), // left
                        new Vector2(cx + diamondOffset, cy)  // right
                    };
                    foreach (var dot in dots)
                    {
                        int dx = Mathf.RoundToInt(dot.x);
                        int dy = Mathf.RoundToInt(dot.y);
                        for (int x = -dotRadius; x <= dotRadius; x++)
                        {
                            for (int y = -dotRadius; y <= dotRadius; y++)
                            {
                                if (x * x + y * y <= dotRadius * dotRadius)
                                {
                                    int px = dx + x;
                                    int py = dy + y;
                                    if (px >= 0 && px < targetTexture.width && py >= 0 && py < targetTexture.height)
                                        targetTexture.SetPixel(px, py, targetColor);
                                }
                            }
                        }
                    }
                    break;
            }
        }

        /// <summary>
        /// Converts device gyroscope attitude (right-handed) into Unity's left-handed space.
        /// Required for correct head-drift angles on both Android and iOS.
        /// </summary>
        private static Quaternion GyroAttitudeToUnity(Quaternion q)
        {
            return new Quaternion(q.x, q.y, -q.z, -q.w);
        }
    }
}
