using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace OpenPerimeter.UI.DesignSystem
{
    /// <summary>
    /// Factory helpers for reusable OpenPerimeter UITK components.
    /// Styles come from OpComponents.uss + theme tokens.
    /// </summary>
    public static class OpComponents
    {
        public static Button Button(string label, string variant = null, OpIconKind icon = OpIconKind.None, Action onClick = null)
        {
            var btn = new Button();
            btn.AddToClassList("op-button");
            if (!string.IsNullOrEmpty(variant))
                btn.AddToClassList("op-button--" + variant);

            if (icon != OpIconKind.None)
            {
                var iconEl = new OpIconElement(icon);
                iconEl.AddToClassList("op-button__icon");
                btn.Add(iconEl);
            }

            var text = new Label(label);
            text.AddToClassList("op-button__label");
            btn.Add(text);

            if (onClick != null)
                btn.clicked += onClick;

            return btn;
        }

        public static VisualElement SegmentedControl(string[] options, int selectedIndex, Action<int> onChanged)
        {
            var root = new VisualElement();
            root.AddToClassList("op-segmented");

            void Select(int index)
            {
                for (int i = 0; i < root.childCount; i++)
                    root[i].EnableInClassList("op-segmented__item--selected", i == index);
                onChanged?.Invoke(index);
            }

            for (int i = 0; i < options.Length; i++)
            {
                int captured = i;
                var item = new Button(() => Select(captured));
                item.text = options[i];
                item.AddToClassList("op-segmented__item");
                if (i == selectedIndex)
                    item.AddToClassList("op-segmented__item--selected");
                root.Add(item);
            }

            return root;
        }

        public static VisualElement Card(string title, string body, bool elevated = false)
        {
            var card = new VisualElement();
            card.AddToClassList("op-card");
            if (elevated) card.AddToClassList("op-card--elevated");

            if (!string.IsNullOrEmpty(title))
            {
                var t = new Label(title);
                t.AddToClassList("op-card__title");
                card.Add(t);
            }

            if (!string.IsNullOrEmpty(body))
            {
                var b = new Label(body);
                b.AddToClassList("op-card__body");
                card.Add(b);
            }

            return card;
        }

        public static VisualElement Chip(string label, OpChipTone tone = OpChipTone.Neutral, OpIconKind icon = OpIconKind.None)
        {
            var chip = new VisualElement();
            chip.AddToClassList("op-chip");
            switch (tone)
            {
                case OpChipTone.Accent: chip.AddToClassList("op-chip--accent"); break;
                case OpChipTone.Warning: chip.AddToClassList("op-chip--warning"); break;
                case OpChipTone.Danger: chip.AddToClassList("op-chip--danger"); break;
                case OpChipTone.Success: chip.AddToClassList("op-chip--success"); break;
            }

            if (icon != OpIconKind.None)
            {
                var iconEl = new OpIconElement(icon);
                iconEl.AddToClassList("op-chip__icon");
                chip.Add(iconEl);
            }

            var text = new Label(label);
            text.AddToClassList("op-chip__label");
            chip.Add(text);
            return chip;
        }

        public static VisualElement ListRow(string title, string subtitle, string trailing, OpIconKind leading, Action onClick = null)
        {
            var row = new VisualElement();
            row.AddToClassList("op-list-row");

            if (leading != OpIconKind.None)
            {
                var icon = new OpIconElement(leading);
                icon.AddToClassList("op-list-row__leading");
                row.Add(icon);
            }

            var content = new VisualElement();
            content.AddToClassList("op-list-row__content");
            var t = new Label(title);
            t.AddToClassList("op-list-row__title");
            content.Add(t);
            if (!string.IsNullOrEmpty(subtitle))
            {
                var s = new Label(subtitle);
                s.AddToClassList("op-list-row__subtitle");
                content.Add(s);
            }
            row.Add(content);

            if (!string.IsNullOrEmpty(trailing))
            {
                var tr = new Label(trailing);
                tr.AddToClassList("op-list-row__trailing");
                tr.AddToClassList("op-mono");
                row.Add(tr);
            }

            if (onClick != null)
                row.RegisterCallback<ClickEvent>(_ => onClick());

            return row;
        }

        public static VisualElement Toggle(string label, bool isOn, Action<bool> onChanged)
        {
            var root = new VisualElement();
            root.AddToClassList("op-toggle");
            if (isOn) root.AddToClassList("op-toggle--on");

            var track = new VisualElement();
            track.AddToClassList("op-toggle__track");
            var thumb = new VisualElement();
            thumb.AddToClassList("op-toggle__thumb");
            track.Add(thumb);
            root.Add(track);

            var text = new Label(label);
            text.AddToClassList("op-toggle__label");
            root.Add(text);

            bool state = isOn;
            root.RegisterCallback<ClickEvent>(_ =>
            {
                state = !state;
                root.EnableInClassList("op-toggle--on", state);
                onChanged?.Invoke(state);
            });

            return root;
        }

        public static VisualElement Stepper(float value, float step, float min, float max, Action<float> onChanged)
        {
            var root = new VisualElement();
            root.AddToClassList("op-stepper");

            float current = value;
            var valueLabel = new Label(FormatMono(current));
            valueLabel.AddToClassList("op-stepper__value");
            valueLabel.AddToClassList("op-mono");

            var minus = new Button(() =>
            {
                current = Mathf.Max(min, current - step);
                valueLabel.text = FormatMono(current);
                onChanged?.Invoke(current);
            });
            minus.text = "−";
            minus.AddToClassList("op-stepper__btn");

            var plus = new Button(() =>
            {
                current = Mathf.Min(max, current + step);
                valueLabel.text = FormatMono(current);
                onChanged?.Invoke(current);
            });
            plus.text = "+";
            plus.AddToClassList("op-stepper__btn");

            root.Add(minus);
            root.Add(valueLabel);
            root.Add(plus);
            return root;
        }

        public static VisualElement Banner(string title, string message, OpBannerTone tone, OpIconKind icon)
        {
            var banner = new VisualElement();
            banner.AddToClassList("op-banner");
            switch (tone)
            {
                case OpBannerTone.Warning: banner.AddToClassList("op-banner--warning"); break;
                case OpBannerTone.Success: banner.AddToClassList("op-banner--success"); break;
                case OpBannerTone.Danger: banner.AddToClassList("op-banner--danger"); break;
                default: banner.AddToClassList("op-banner--info"); break;
            }

            var iconEl = new OpIconElement(icon);
            iconEl.AddToClassList("op-banner__icon");
            banner.Add(iconEl);

            var body = new VisualElement();
            body.AddToClassList("op-banner__body");
            var t = new Label(title);
            t.AddToClassList("op-banner__title");
            var m = new Label(message);
            m.AddToClassList("op-banner__message");
            body.Add(t);
            body.Add(m);
            banner.Add(body);
            return banner;
        }

        public static VisualElement Dialog(string title, string message, string confirmLabel, string cancelLabel,
            Action onConfirm, Action onCancel, bool destructive = false)
        {
            var overlay = new VisualElement();
            overlay.AddToClassList("op-dialog-overlay");
            overlay.pickingMode = PickingMode.Position;

            var dialog = new VisualElement();
            dialog.AddToClassList("op-dialog");

            var t = new Label(title);
            t.AddToClassList("op-dialog__title");
            dialog.Add(t);

            var m = new Label(message);
            m.AddToClassList("op-dialog__message");
            dialog.Add(m);

            var actions = new VisualElement();
            actions.AddToClassList("op-dialog__actions");

            var cancel = Button(cancelLabel, "ghost", OpIconKind.None, () =>
            {
                overlay.RemoveFromHierarchy();
                onCancel?.Invoke();
            });
            actions.Add(cancel);

            var confirm = Button(confirmLabel, destructive ? "danger" : "primary", OpIconKind.None, () =>
            {
                overlay.RemoveFromHierarchy();
                onConfirm?.Invoke();
            });
            actions.Add(confirm);

            dialog.Add(actions);
            overlay.Add(dialog);
            return overlay;
        }

        public static VisualElement Toast(string message, OpToastTone tone, OpIconKind icon)
        {
            var toast = new VisualElement();
            toast.AddToClassList("op-toast");
            switch (tone)
            {
                case OpToastTone.Success: toast.AddToClassList("op-toast--success"); break;
                case OpToastTone.Warning: toast.AddToClassList("op-toast--warning"); break;
                case OpToastTone.Danger: toast.AddToClassList("op-toast--danger"); break;
            }

            if (icon != OpIconKind.None)
            {
                var iconEl = new OpIconElement(icon);
                iconEl.AddToClassList("op-toast__icon");
                toast.Add(iconEl);
            }

            var label = new Label(message);
            label.AddToClassList("op-toast__message");
            toast.Add(label);
            return toast;
        }

        public static VisualElement ProgressBar(string label, float progress01)
        {
            var root = new VisualElement();
            root.AddToClassList("op-progress");

            var row = new VisualElement();
            row.AddToClassList("op-progress__label-row");
            var name = new Label(label);
            name.AddToClassList("op-progress__label");
            var value = new Label($"{Mathf.RoundToInt(Mathf.Clamp01(progress01) * 100f)}%");
            value.AddToClassList("op-progress__value");
            value.AddToClassList("op-mono");
            row.Add(name);
            row.Add(value);
            root.Add(row);

            var track = new VisualElement();
            track.AddToClassList("op-progress__track");
            var fill = new VisualElement();
            fill.AddToClassList("op-progress__fill");
            fill.style.width = Length.Percent(Mathf.Clamp01(progress01) * 100f);
            track.Add(fill);
            root.Add(track);

            root.userData = new ProgressRefs { Value = value, Fill = fill };
            return root;
        }

        public static void SetProgress(VisualElement progressRoot, float progress01)
        {
            if (progressRoot?.userData is ProgressRefs refs)
            {
                float p = Mathf.Clamp01(progress01);
                refs.Value.text = $"{Mathf.RoundToInt(p * 100f)}%";
                refs.Fill.style.width = Length.Percent(p * 100f);
            }
        }

        private static string FormatMono(float v)
        {
            return Mathf.Approximately(v, Mathf.Round(v)) ? ((int)v).ToString() : v.ToString("0.0");
        }

        private class ProgressRefs
        {
            public Label Value;
            public VisualElement Fill;
        }
    }
}
