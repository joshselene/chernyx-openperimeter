#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace OpenPerimeter.UI.DesignSystem.Editor
{
    /// <summary>
    /// Editor helpers to wire PanelSettings + gallery scene for Phase 1 design system.
    /// </summary>
    public static class OpDesignSystemMenu
    {
        private const string GalleryScenePath = "Assets/OpenPerimeter/UI/Gallery/ComponentGallery.unity";
        private const string PanelSettingsPath = "Assets/OpenPerimeter/UI/Gallery/OpPanelSettings.asset";

        [MenuItem("OpenPerimeter/Design System/Open Component Gallery")]
        public static void OpenGallery()
        {
            EnsurePanelSettings();
            EnsureGallerySceneWired();
            var scene = EditorSceneManager.OpenScene(GalleryScenePath, OpenSceneMode.Single);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            if (!Application.isPlaying)
                EditorApplication.EnterPlaymode();
        }

        [MenuItem("OpenPerimeter/Design System/Ensure Gallery Assets")]
        public static void EnsureGalleryAssets()
        {
            EnsurePanelSettings();
            EnsureGallerySceneWired();
            AssetDatabase.SaveAssets();
            Debug.Log("OpenPerimeter design system gallery assets ensured.");
        }

        private static PanelSettings EnsurePanelSettings()
        {
            var existing = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
            if (existing != null) return existing;

            var settings = ScriptableObject.CreateInstance<PanelSettings>();
            settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            settings.referenceResolution = new Vector2Int(390, 844);
            settings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            settings.match = 0.5f;
            settings.referenceDpi = 160;
            settings.fallbackDpi = 160;

            Directory.CreateDirectory(Path.GetDirectoryName(PanelSettingsPath) ?? "Assets/OpenPerimeter/UI/Gallery");
            AssetDatabase.CreateAsset(settings, PanelSettingsPath);
            AssetDatabase.SaveAssets();
            return settings;
        }

        private static void EnsureGallerySceneWired()
        {
            if (!File.Exists(GalleryScenePath))
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene, GalleryScenePath);
            }

            var open = EditorSceneManager.OpenScene(GalleryScenePath, OpenSceneMode.Single);
            var go = GameObject.Find("ComponentGallery");
            if (go == null)
            {
                go = new GameObject("ComponentGallery");
            }

            var doc = go.GetComponent<UIDocument>();
            if (doc == null) doc = go.AddComponent<UIDocument>();
            doc.panelSettings = EnsurePanelSettings();

            if (go.GetComponent<OpThemeController>() == null)
                go.AddComponent<OpThemeController>();
            if (go.GetComponent<ComponentGalleryController>() == null)
                go.AddComponent<ComponentGalleryController>();

            // Assign style sheets if present
            var theme = go.GetComponent<OpThemeController>();
            var gallery = go.GetComponent<ComponentGalleryController>();
            var sheets = new[]
            {
                "Assets/OpenPerimeter/UI/Tokens/OpTokens.uss",
                "Assets/OpenPerimeter/UI/Tokens/OpTypography.uss",
                "Assets/OpenPerimeter/UI/Themes/OpTheme.Dark.uss",
                "Assets/OpenPerimeter/UI/Themes/OpTheme.Light.uss",
                "Assets/OpenPerimeter/UI/Components/OpComponents.uss",
                "Assets/OpenPerimeter/UI/Gallery/ComponentGallery.uss"
            };

            var soTheme = new SerializedObject(theme);
            var sheetsProp = soTheme.FindProperty("styleSheets");
            if (sheetsProp != null && sheetsProp.isArray)
            {
                sheetsProp.arraySize = sheets.Length;
                for (int i = 0; i < sheets.Length; i++)
                {
                    var sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(sheets[i]);
                    sheetsProp.GetArrayElementAtIndex(i).objectReferenceValue = sheet;
                }
                soTheme.ApplyModifiedPropertiesWithoutUndo();
            }

            var soGallery = new SerializedObject(gallery);
            var gSheets = soGallery.FindProperty("styleSheets");
            if (gSheets != null && gSheets.isArray)
            {
                gSheets.arraySize = sheets.Length;
                for (int i = 0; i < sheets.Length; i++)
                {
                    var sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(sheets[i]);
                    gSheets.GetArrayElementAtIndex(i).objectReferenceValue = sheet;
                }

                var themeRef = soGallery.FindProperty("themeController");
                if (themeRef != null) themeRef.objectReferenceValue = theme;
                soGallery.ApplyModifiedPropertiesWithoutUndo();
            }

            // Fonts
            var sans = AssetDatabase.LoadAssetAtPath<Font>("Assets/OpenPerimeter/UI/Fonts/IBMPlexSans-Regular.otf");
            var sansSb = AssetDatabase.LoadAssetAtPath<Font>("Assets/OpenPerimeter/UI/Fonts/IBMPlexSans-SemiBold.otf");
            var mono = AssetDatabase.LoadAssetAtPath<Font>("Assets/OpenPerimeter/UI/Fonts/IBMPlexMono-Regular.otf");
            soTheme.FindProperty("plexSansRegular").objectReferenceValue = sans;
            soTheme.FindProperty("plexSansSemiBold").objectReferenceValue = sansSb;
            soTheme.FindProperty("plexMonoRegular").objectReferenceValue = mono;
            soTheme.FindProperty("uiDocument").objectReferenceValue = doc;
            soTheme.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(open);
            EditorSceneManager.SaveScene(open);
        }
    }
}
#endif
