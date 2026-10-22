using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace OpenPerimeter.UI.DesignSystem
{
    /// <summary>
    /// Applies dark/light theme classes, font scale, IBM Plex fonts, and shared style sheets
    /// to a UITK root. UI-layer only — does not touch stimulus/calibration/threshold logic.
    /// </summary>
    public class OpThemeController : MonoBehaviour
    {
        public const string RootClass = "op-root";
        public const string DarkClass = "op-theme-dark";
        public const string LightClass = "op-theme-light";

        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private OpThemeMode initialTheme = OpThemeMode.Dark;
        [SerializeField] private OpFontScale initialFontScale = OpFontScale.Scale100;
        [SerializeField] private List<StyleSheet> styleSheets = new List<StyleSheet>();
        [SerializeField] private Font plexSansRegular;
        [SerializeField] private Font plexSansSemiBold;
        [SerializeField] private Font plexMonoRegular;

        private VisualElement root;
        private OpThemeMode theme;
        private OpFontScale fontScale;

        public OpThemeMode Theme => theme;
        public OpFontScale FontScale => fontScale;
        public event Action<OpThemeMode> ThemeChanged;

        private void Awake()
        {
            if (uiDocument == null)
                uiDocument = GetComponent<UIDocument>();

            theme = initialTheme;
            fontScale = initialFontScale;
        }

        private void OnEnable()
        {
            if (uiDocument == null) return;
            root = uiDocument.rootVisualElement;
            if (root == null) return;

            EnsureStyleSheets(root);
            ApplyTheme(theme);
            ApplyFontScale(fontScale);
            ApplyFonts(root);
        }

        public void Bind(VisualElement galleryRoot)
        {
            root = galleryRoot;
            EnsureStyleSheets(root);
            ApplyTheme(theme);
            ApplyFontScale(fontScale);
            ApplyFonts(root);
        }

        public void SetTheme(OpThemeMode mode)
        {
            theme = mode;
            ApplyTheme(mode);
            ThemeChanged?.Invoke(mode);
        }

        public void ToggleTheme()
        {
            SetTheme(theme == OpThemeMode.Dark ? OpThemeMode.Light : OpThemeMode.Dark);
        }

        public void SetFontScale(OpFontScale scale)
        {
            fontScale = scale;
            ApplyFontScale(scale);
        }

        private void ApplyTheme(OpThemeMode mode)
        {
            if (root == null) return;
            root.AddToClassList(RootClass);
            root.EnableInClassList(DarkClass, mode == OpThemeMode.Dark);
            root.EnableInClassList(LightClass, mode == OpThemeMode.Light);
        }

        private void ApplyFontScale(OpFontScale scale)
        {
            if (root == null) return;
            root.RemoveFromClassList("op-font-scale-100");
            root.RemoveFromClassList("op-font-scale-115");
            root.RemoveFromClassList("op-font-scale-130");
            switch (scale)
            {
                case OpFontScale.Scale115:
                    root.AddToClassList("op-font-scale-115");
                    break;
                case OpFontScale.Scale130:
                    root.AddToClassList("op-font-scale-130");
                    break;
                default:
                    root.AddToClassList("op-font-scale-100");
                    break;
            }
        }

        private void EnsureStyleSheets(VisualElement target)
        {
            if (styleSheets == null) return;
            foreach (var sheet in styleSheets)
            {
                if (sheet == null) continue;
                if (!target.styleSheets.Contains(sheet))
                    target.styleSheets.Add(sheet);
            }
        }

        private void ApplyFonts(VisualElement target)
        {
            if (plexSansRegular != null)
                target.style.unityFont = plexSansRegular;

            ApplyMonoRecursive(target);
        }

        private void ApplyMonoRecursive(VisualElement ve)
        {
            if (ve.ClassListContains("op-mono") || ve.ClassListContains("op-text--mono") ||
                ve.ClassListContains("op-progress__value") || ve.ClassListContains("op-stepper__value"))
            {
                if (plexMonoRegular != null)
                    ve.style.unityFont = plexMonoRegular;
            }

            if (ve.ClassListContains("op-text--xl") || ve.ClassListContains("op-text--display") ||
                ve.ClassListContains("op-section__title") || ve.ClassListContains("op-card__title") ||
                ve.ClassListContains("op-button--primary") || ve.ClassListContains("op-dialog__title"))
            {
                if (plexSansSemiBold != null)
                    ve.style.unityFont = plexSansSemiBold;
            }

            int count = ve.hierarchy.childCount;
            for (int i = 0; i < count; i++)
                ApplyMonoRecursive(ve.hierarchy.ElementAt(i));
        }

        /// <summary>
        /// Loads fonts from Resources/OpenPerimeter/Fonts when serialized refs are empty.
        /// </summary>
        public void TryLoadFontsFromResources()
        {
            if (plexSansRegular == null)
                plexSansRegular = Resources.Load<Font>("OpenPerimeter/Fonts/IBMPlexSans-Regular");
            if (plexSansSemiBold == null)
                plexSansSemiBold = Resources.Load<Font>("OpenPerimeter/Fonts/IBMPlexSans-SemiBold");
            if (plexMonoRegular == null)
                plexMonoRegular = Resources.Load<Font>("OpenPerimeter/Fonts/IBMPlexMono-Regular");
        }
    }
}
