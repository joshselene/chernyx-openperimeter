using System.IO;
using UnityEngine;

namespace OpenPerimeter.Storage
{
    /// <summary>
    /// Loads packaged research JSON (norms, sector map, default calibration) on both
    /// Android and iOS. Files live under Resources/OpenPerimeterData so they are
    /// readable via Resources.Load on every player platform (StreamingAssets File I/O
    /// is not portable on Android APK jars).
    /// </summary>
    public static class PackagedResearchData
    {
        public const string ResourceRoot = "OpenPerimeterData";

        /// <summary>
        /// Loads a TextAsset by relative path under OpenPerimeterData (e.g. "norms.json"
        /// or "devices/default_calibration.json"). Returns null if missing.
        /// </summary>
        public static string LoadTextOrNull(string relativePathWithExtension)
        {
            if (string.IsNullOrEmpty(relativePathWithExtension))
                return null;

            string normalized = relativePathWithExtension.Replace('\\', '/').TrimStart('/');
            string resourcePath = Path.ChangeExtension($"{ResourceRoot}/{normalized}", null);
            TextAsset asset = Resources.Load<TextAsset>(resourcePath);
            if (asset != null)
                return asset.text;

            // Optional operator override dropped into persistent storage (both OSes).
            string persistentPath = Path.Combine(Application.persistentDataPath, "OpenPerimeterData", normalized);
            if (File.Exists(persistentPath))
                return File.ReadAllText(persistentPath);

            return null;
        }
    }
}
