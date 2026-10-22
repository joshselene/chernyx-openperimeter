using UnityEngine;
using UnityEngine.UIElements;

namespace OpenPerimeter.UI.DesignSystem
{
    /// <summary>
    /// Phase 1 component gallery — demos every design-system control in dark/light with theme switcher.
    /// Does not interact with perimetry physics, calibration, or storage.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class ComponentGalleryController : MonoBehaviour
    {
        [SerializeField] private OpThemeController themeController;
        [SerializeField] private StyleSheet[] styleSheets;

        private UIDocument document;
        private VisualElement root;
        private VisualElement toastHost;
        private VisualElement dialogLayer;
        private Label themeStateLabel;

        private void Awake()
        {
            document = GetComponent<UIDocument>();
            if (themeController == null)
                themeController = GetComponent<OpThemeController>();
        }

        private void OnEnable()
        {
            root = document.rootVisualElement;
            root.Clear();
            root.AddToClassList("op-root");

            if (styleSheets != null)
            {
                foreach (var sheet in styleSheets)
                {
                    if (sheet != null && !root.styleSheets.Contains(sheet))
                        root.styleSheets.Add(sheet);
                }
            }

            if (themeController != null)
            {
                themeController.TryLoadFontsFromResources();
                themeController.Bind(root);
            }
            else
            {
                root.AddToClassList("op-theme-dark");
                root.AddToClassList("op-font-scale-100");
            }

            BuildChrome();
            BuildContent();
        }

        private void BuildChrome()
        {
            var header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.alignItems = Align.Center;
            header.style.justifyContent = Justify.SpaceBetween;
            header.style.paddingLeft = 16;
            header.style.paddingRight = 16;
            header.style.paddingTop = 16;
            header.style.paddingBottom = 12;
            header.style.borderBottomWidth = 1;

            var titles = new VisualElement();
            var brand = new Label("OpenPerimeter");
            brand.AddToClassList("op-text--xl");
            var sub = new Label("Phase 1 · Design System Gallery · RUO");
            sub.AddToClassList("op-text--secondary");
            titles.Add(brand);
            titles.Add(sub);
            header.Add(titles);

            var controls = new VisualElement();
            controls.AddToClassList("op-row");

            themeStateLabel = new Label(ThemeLabel());
            themeStateLabel.AddToClassList("op-text--sm");
            themeStateLabel.AddToClassList("op-mono");
            controls.Add(themeStateLabel);

            controls.Add(OpComponents.Button("Theme", "primary", OpIconKind.Theme, () =>
            {
                if (themeController != null)
                    themeController.ToggleTheme();
                else
                {
                    bool dark = root.ClassListContains("op-theme-dark");
                    root.EnableInClassList("op-theme-dark", !dark);
                    root.EnableInClassList("op-theme-light", dark);
                }
                themeStateLabel.text = ThemeLabel();
            }));

            controls.Add(OpComponents.SegmentedControl(
                new[] { "100%", "115%", "130%" },
                0,
                index =>
                {
                    var scale = index == 1 ? OpFontScale.Scale115 :
                        index == 2 ? OpFontScale.Scale130 : OpFontScale.Scale100;
                    if (themeController != null)
                        themeController.SetFontScale(scale);
                    else
                    {
                        root.RemoveFromClassList("op-font-scale-100");
                        root.RemoveFromClassList("op-font-scale-115");
                        root.RemoveFromClassList("op-font-scale-130");
                        root.AddToClassList(scale == OpFontScale.Scale115 ? "op-font-scale-115" :
                            scale == OpFontScale.Scale130 ? "op-font-scale-130" : "op-font-scale-100");
                    }
                }));

            header.Add(controls);
            root.Add(header);

            dialogLayer = new VisualElement();
            dialogLayer.style.position = Position.Absolute;
            dialogLayer.style.left = 0;
            dialogLayer.style.right = 0;
            dialogLayer.style.top = 0;
            dialogLayer.style.bottom = 0;
            dialogLayer.pickingMode = PickingMode.Ignore;

            toastHost = new VisualElement();
            toastHost.AddToClassList("op-toast-host");
            toastHost.pickingMode = PickingMode.Ignore;
        }

        private void BuildContent()
        {
            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.AddToClassList("op-scroll");
            scroll.style.flexGrow = 1;

            scroll.Add(Section("Buttons", BuildButtons()));
            scroll.Add(Section("Segmented control", BuildSegmented()));
            scroll.Add(Section("Cards", BuildCards()));
            scroll.Add(Section("Chips & status", BuildChips()));
            scroll.Add(Section("List rows", BuildListRows()));
            scroll.Add(Section("Toggle & stepper", BuildToggleStepper()));
            scroll.Add(Section("Banner", BuildBanners()));
            scroll.Add(Section("Progress", BuildProgress()));
            scroll.Add(Section("Dialog & toast", BuildDialogToast()));
            scroll.Add(Section("Typography & numbers", BuildType()));
            scroll.Add(Section("Icons (stroke)", BuildIcons()));

            var disclaimer = new Label("RESEARCH USE ONLY — NOT A MEDICAL DEVICE — FOR INVESTIGATIONAL BENCH TESTING ONLY");
            disclaimer.AddToClassList("op-text--xs");
            disclaimer.AddToClassList("op-text--muted");
            disclaimer.style.marginLeft = 16;
            disclaimer.style.marginRight = 16;
            disclaimer.style.marginBottom = 32;
            disclaimer.style.whiteSpace = WhiteSpace.Normal;
            scroll.Add(disclaimer);

            root.Add(scroll);
            root.Add(dialogLayer);
            root.Add(toastHost);
        }

        private static VisualElement Section(string title, VisualElement body)
        {
            var section = new VisualElement();
            section.AddToClassList("op-section");
            var t = new Label(title);
            t.AddToClassList("op-section__title");
            section.Add(t);
            section.Add(body);
            return section;
        }

        private VisualElement BuildButtons()
        {
            var row = new VisualElement();
            row.AddToClassList("op-row");
            row.Add(OpComponents.Button("Primary", "primary", OpIconKind.Play));
            row.Add(OpComponents.Button("Secondary", null, OpIconKind.Settings));
            row.Add(OpComponents.Button("Ghost", "ghost"));
            row.Add(OpComponents.Button("Abort", "danger", OpIconKind.Abort));
            return row;
        }

        private VisualElement BuildSegmented()
        {
            return OpComponents.SegmentedControl(new[] { "OD", "OS", "OU" }, 0, _ => { });
        }

        private VisualElement BuildCards()
        {
            var stack = new VisualElement();
            stack.AddToClassList("op-stack");
            stack.Add(OpComponents.Card(
                "Session setup",
                "Reusable surface for grouping controls. Cards are interaction containers — not decorative chrome.",
                elevated: false));
            stack.Add(OpComponents.Card(
                "Elevated card",
                "Slightly stronger surface for nested emphasis.",
                elevated: true));
            return stack;
        }

        private VisualElement BuildChips()
        {
            var row = new VisualElement();
            row.AddToClassList("op-row");
            row.Add(OpComponents.Chip("Calibrated", OpChipTone.Success, OpIconKind.Check));
            row.Add(OpComponents.Chip("Uncalibrated", OpChipTone.Warning, OpIconKind.Warning));
            row.Add(OpComponents.Chip("Accent", OpChipTone.Accent, OpIconKind.Info));
            row.Add(OpComponents.Chip("Abort risk", OpChipTone.Danger, OpIconKind.Error));
            row.Add(OpComponents.Chip("Neutral", OpChipTone.Neutral));
            return row;
        }

        private VisualElement BuildListRows()
        {
            var list = new VisualElement();
            list.Add(OpComponents.ListRow("24-2 pattern", "54 points · 6° grid", "54", OpIconKind.Eye));
            list.Add(OpComponents.ListRow("10-2 pattern", "68 points · 2° grid", "68", OpIconKind.Eye));
            list.Add(OpComponents.ListRow("ZEST strategy", "Bayesian threshold", "ON", OpIconKind.Settings));
            return list;
        }

        private VisualElement BuildToggleStepper()
        {
            var stack = new VisualElement();
            stack.AddToClassList("op-stack");
            stack.Add(OpComponents.Toggle("Sound prompts", true, _ => { }));
            stack.Add(OpComponents.Stepper(12f, 1f, 0f, 40f, _ => { }));
            return stack;
        }

        private VisualElement BuildBanners()
        {
            var stack = new VisualElement();
            stack.AddToClassList("op-stack");
            stack.Add(OpComponents.Banner(
                "Research calibrated",
                "Photometer profile loaded. Stimulus luminance follows the device LUT.",
                OpBannerTone.Success,
                OpIconKind.Check));
            stack.Add(OpComponents.Banner(
                "Head drift warning",
                "Keep gaze on the amber fixation cross. Amber is reserved for warnings.",
                OpBannerTone.Warning,
                OpIconKind.Warning));
            stack.Add(OpComponents.Banner(
                "Session interrupted",
                "Thermal guard paused the examination. Red is reserved for destructive/critical states.",
                OpBannerTone.Danger,
                OpIconKind.Error));
            return stack;
        }

        private VisualElement BuildProgress()
        {
            return OpComponents.ProgressBar("Examination progress", 0.62f);
        }

        private VisualElement BuildDialogToast()
        {
            var row = new VisualElement();
            row.AddToClassList("op-row");

            row.Add(OpComponents.Button("Show dialog", "primary", OpIconKind.None, () =>
            {
                dialogLayer.pickingMode = PickingMode.Position;
                var dialog = OpComponents.Dialog(
                    "Abort session?",
                    "This ends the examination and discards in-progress threshold estimates. Red confirms destructive actions only.",
                    "Abort",
                    "Cancel",
                    () =>
                    {
                        dialogLayer.pickingMode = PickingMode.Ignore;
                        ShowToast("Session aborted", OpToastTone.Danger, OpIconKind.Abort);
                    },
                    () => { dialogLayer.pickingMode = PickingMode.Ignore; },
                    destructive: true);
                dialogLayer.Clear();
                dialogLayer.Add(dialog);
            }));

            row.Add(OpComponents.Button("Show toast", "ghost", OpIconKind.Check, () =>
            {
                ShowToast("Profile saved", OpToastTone.Success, OpIconKind.Check);
            }));

            return row;
        }

        private VisualElement BuildType()
        {
            var stack = new VisualElement();
            stack.AddToClassList("op-stack");
            var display = new Label("IBM Plex Sans — Display");
            display.AddToClassList("op-text--display");
            var body = new Label("Body text uses IBM Plex Sans. Numbers and dB values use IBM Plex Mono for tabular alignment.");
            body.AddToClassList("op-text");
            body.style.whiteSpace = WhiteSpace.Normal;
            var mono = new Label("MD −6.42 dB · PSD 4.18 dB · FL 12%");
            mono.AddToClassList("op-mono");
            mono.AddToClassList("op-text--mono");
            stack.Add(display);
            stack.Add(body);
            stack.Add(mono);
            return stack;
        }

        private VisualElement BuildIcons()
        {
            var row = new VisualElement();
            row.AddToClassList("op-row");
            foreach (OpIconKind kind in System.Enum.GetValues(typeof(OpIconKind)))
            {
                if (kind == OpIconKind.None) continue;
                var cell = new VisualElement();
                cell.style.alignItems = Align.Center;
                cell.style.marginRight = 12;
                cell.style.marginBottom = 8;
                var icon = new OpIconElement(kind);
                icon.style.width = 24;
                icon.style.height = 24;
                var label = new Label(kind.ToString());
                label.AddToClassList("op-text--xs");
                label.AddToClassList("op-text--muted");
                cell.Add(icon);
                cell.Add(label);
                row.Add(cell);
            }
            return row;
        }

        private void ShowToast(string message, OpToastTone tone, OpIconKind icon)
        {
            toastHost.Clear();
            toastHost.Add(OpComponents.Toast(message, tone, icon));
            CancelInvoke(nameof(ClearToast));
            Invoke(nameof(ClearToast), 2.5f);
        }

        private void ClearToast()
        {
            toastHost.Clear();
        }

        private string ThemeLabel()
        {
            if (themeController != null)
                return themeController.Theme == OpThemeMode.Dark ? "DARK" : "LIGHT";
            return root != null && root.ClassListContains("op-theme-light") ? "LIGHT" : "DARK";
        }
    }
}
