using UnityEngine;
using UnityEngine.UIElements;

namespace OpenPerimeter.UI.DesignSystem
{
    /// <summary>
    /// Stroke icon drawn with Painter2D — no emoji, no bitmap dependency.
    /// </summary>
    public class OpIconElement : VisualElement
    {
        private OpIconKind kind = OpIconKind.Info;
        private Color strokeColor = new Color(0.91f, 0.93f, 0.96f, 1f);
        private float strokeWidth = 1.75f;

        public OpIconKind Kind
        {
            get => kind;
            set
            {
                kind = value;
                MarkDirtyRepaint();
            }
        }

        public Color StrokeColor
        {
            get => strokeColor;
            set
            {
                strokeColor = value;
                MarkDirtyRepaint();
            }
        }

        public OpIconElement()
        {
            AddToClassList("op-icon");
            generateVisualContent += OnGenerateVisualContent;
            style.width = 20;
            style.height = 20;
        }

        public OpIconElement(OpIconKind iconKind) : this()
        {
            kind = iconKind;
        }

        private void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            if (kind == OpIconKind.None) return;

            var painter = mgc.painter2D;
            painter.strokeColor = strokeColor;
            painter.lineWidth = strokeWidth;
            painter.lineCap = LineCap.Round;
            painter.lineJoin = LineJoin.Round;

            Rect r = contentRect;
            if (r.width < 1f || r.height < 1f) return;

            float pad = strokeWidth + 1f;
            float x0 = r.xMin + pad;
            float y0 = r.yMin + pad;
            float x1 = r.xMax - pad;
            float y1 = r.yMax - pad;
            float cx = (x0 + x1) * 0.5f;
            float cy = (y0 + y1) * 0.5f;
            float w = x1 - x0;
            float h = y1 - y0;

            switch (kind)
            {
                case OpIconKind.Check:
                    painter.BeginPath();
                    painter.MoveTo(new Vector2(x0, cy));
                    painter.LineTo(new Vector2(cx - w * 0.08f, y1 - h * 0.1f));
                    painter.LineTo(new Vector2(x1, y0 + h * 0.15f));
                    painter.Stroke();
                    break;

                case OpIconKind.Warning:
                    painter.BeginPath();
                    painter.MoveTo(new Vector2(cx, y0));
                    painter.LineTo(new Vector2(x1, y1));
                    painter.LineTo(new Vector2(x0, y1));
                    painter.ClosePath();
                    painter.Stroke();
                    painter.BeginPath();
                    painter.MoveTo(new Vector2(cx, y0 + h * 0.35f));
                    painter.LineTo(new Vector2(cx, y0 + h * 0.62f));
                    painter.Stroke();
                    painter.BeginPath();
                    painter.Arc(new Vector2(cx, y0 + h * 0.78f), 0.8f, 0f, 360f);
                    painter.Stroke();
                    break;

                case OpIconKind.Error:
                    painter.BeginPath();
                    painter.Arc(new Vector2(cx, cy), Mathf.Min(w, h) * 0.48f, 0f, 360f);
                    painter.Stroke();
                    painter.BeginPath();
                    painter.MoveTo(new Vector2(cx - w * 0.22f, cy - h * 0.22f));
                    painter.LineTo(new Vector2(cx + w * 0.22f, cy + h * 0.22f));
                    painter.MoveTo(new Vector2(cx + w * 0.22f, cy - h * 0.22f));
                    painter.LineTo(new Vector2(cx - w * 0.22f, cy + h * 0.22f));
                    painter.Stroke();
                    break;

                case OpIconKind.Info:
                    painter.BeginPath();
                    painter.Arc(new Vector2(cx, cy), Mathf.Min(w, h) * 0.48f, 0f, 360f);
                    painter.Stroke();
                    painter.BeginPath();
                    painter.Arc(new Vector2(cx, y0 + h * 0.28f), 0.9f, 0f, 360f);
                    painter.Stroke();
                    painter.BeginPath();
                    painter.MoveTo(new Vector2(cx, y0 + h * 0.45f));
                    painter.LineTo(new Vector2(cx, y1 - h * 0.12f));
                    painter.Stroke();
                    break;

                case OpIconKind.Close:
                    painter.BeginPath();
                    painter.MoveTo(new Vector2(x0, y0));
                    painter.LineTo(new Vector2(x1, y1));
                    painter.MoveTo(new Vector2(x1, y0));
                    painter.LineTo(new Vector2(x0, y1));
                    painter.Stroke();
                    break;

                case OpIconKind.ChevronRight:
                    painter.BeginPath();
                    painter.MoveTo(new Vector2(cx - w * 0.15f, y0));
                    painter.LineTo(new Vector2(cx + w * 0.2f, cy));
                    painter.LineTo(new Vector2(cx - w * 0.15f, y1));
                    painter.Stroke();
                    break;

                case OpIconKind.Plus:
                    painter.BeginPath();
                    painter.MoveTo(new Vector2(cx, y0));
                    painter.LineTo(new Vector2(cx, y1));
                    painter.MoveTo(new Vector2(x0, cy));
                    painter.LineTo(new Vector2(x1, cy));
                    painter.Stroke();
                    break;

                case OpIconKind.Minus:
                    painter.BeginPath();
                    painter.MoveTo(new Vector2(x0, cy));
                    painter.LineTo(new Vector2(x1, cy));
                    painter.Stroke();
                    break;

                case OpIconKind.Play:
                    painter.BeginPath();
                    painter.MoveTo(new Vector2(x0 + w * 0.15f, y0));
                    painter.LineTo(new Vector2(x1 - w * 0.05f, cy));
                    painter.LineTo(new Vector2(x0 + w * 0.15f, y1));
                    painter.ClosePath();
                    painter.Stroke();
                    break;

                case OpIconKind.Pause:
                    float bar = w * 0.18f;
                    painter.BeginPath();
                    painter.MoveTo(new Vector2(cx - w * 0.22f, y0));
                    painter.LineTo(new Vector2(cx - w * 0.22f, y1));
                    painter.MoveTo(new Vector2(cx + w * 0.22f, y0));
                    painter.LineTo(new Vector2(cx + w * 0.22f, y1));
                    painter.Stroke();
                    painter.lineWidth = bar;
                    painter.BeginPath();
                    painter.MoveTo(new Vector2(cx - w * 0.22f, y0));
                    painter.LineTo(new Vector2(cx - w * 0.22f, y1));
                    painter.MoveTo(new Vector2(cx + w * 0.22f, y0));
                    painter.LineTo(new Vector2(cx + w * 0.22f, y1));
                    painter.Stroke();
                    painter.lineWidth = strokeWidth;
                    break;

                case OpIconKind.Abort:
                    painter.BeginPath();
                    painter.Arc(new Vector2(cx, cy), Mathf.Min(w, h) * 0.48f, 0f, 360f);
                    painter.Stroke();
                    painter.BeginPath();
                    painter.MoveTo(new Vector2(x0 + w * 0.2f, cy));
                    painter.LineTo(new Vector2(x1 - w * 0.2f, cy));
                    painter.Stroke();
                    break;

                case OpIconKind.Eye:
                    painter.BeginPath();
                    painter.MoveTo(new Vector2(x0, cy));
                    painter.BezierCurveTo(
                        new Vector2(cx - w * 0.35f, y0),
                        new Vector2(cx + w * 0.35f, y0),
                        new Vector2(x1, cy));
                    painter.BezierCurveTo(
                        new Vector2(cx + w * 0.35f, y1),
                        new Vector2(cx - w * 0.35f, y1),
                        new Vector2(x0, cy));
                    painter.Stroke();
                    painter.BeginPath();
                    painter.Arc(new Vector2(cx, cy), Mathf.Min(w, h) * 0.18f, 0f, 360f);
                    painter.Stroke();
                    break;

                case OpIconKind.Theme:
                    painter.BeginPath();
                    painter.Arc(new Vector2(cx, cy), Mathf.Min(w, h) * 0.45f, 0f, 360f);
                    painter.Stroke();
                    painter.BeginPath();
                    painter.MoveTo(new Vector2(cx, y0));
                    painter.LineTo(new Vector2(cx, y1));
                    painter.Stroke();
                    break;

                case OpIconKind.Settings:
                    painter.BeginPath();
                    painter.Arc(new Vector2(cx, cy), Mathf.Min(w, h) * 0.2f, 0f, 360f);
                    painter.Stroke();
                    painter.BeginPath();
                    painter.Arc(new Vector2(cx, cy), Mathf.Min(w, h) * 0.42f, 0f, 360f);
                    painter.Stroke();
                    break;
            }
        }
    }
}
