/*using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace WindowsFormsApp1.UI
{
    /// <summary>
    /// Single source of truth for all visual tokens, color palettes,
    /// typography constants, and shared control factory methods.
    /// </summary>
    public static class DesignSystem
    {
        // =====================================================
        // COLOR PALETTE — DARK THEME
        // =====================================================
        public static Color BgDeep = Color.FromArgb(10, 12, 20);   // outermost background
        public static Color BgSurface = Color.FromArgb(18, 20, 34);   // card/panel bg
        public static Color BgElevated = Color.FromArgb(26, 29, 50);   // elevated panel
        public static Color BgControl = Color.FromArgb(34, 38, 64);   // combobox / numericUpDown bg
        public static Color BorderSubtle = Color.FromArgb(50, 55, 90);   // panel border
        public static Color BorderBright = Color.FromArgb(80, 90, 140);   // active / hover border

        // Accent — electric violet-blue
        public static Color Accent = Color.FromArgb(99, 102, 241);  // primary CTA
        public static Color AccentHover = Color.FromArgb(118, 121, 255);
        public static Color AccentDim = Color.FromArgb(60, 62, 160);

        // Semantic colours
        public static Color Success = Color.FromArgb(52, 211, 153);
        public static Color Warning = Color.FromArgb(251, 191, 36);
        public static Color Danger = Color.FromArgb(248, 113, 113);

        // Text
        public static Color TextPrimary = Color.FromArgb(236, 237, 255);
        public static Color TextSecondary = Color.FromArgb(148, 150, 200);
        public static Color TextMuted = Color.FromArgb(90, 93, 140);

        // =====================================================
        // LAYOUT CONSTANTS
        // =====================================================
        public const int SectionPad = 16;   // internal section padding
        public const int ControlGap = 10;   // gap between sibling controls
        public const int SectionGap = 14;   // gap between sections
        public const int LabelHeight = 22;
        public const int ButtonHeight = 36;
        public const int ButtonWidth = 118;
        public const int CornerRadius = 8;

        // =====================================================
        // SECTION HEADER LABEL
        // =====================================================
        public static Label CreateSectionHeader(string text)
        {
            return new Label
            {
                Text = text.ToUpper(),
                AutoSize = false,
                Height = 20,
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = TextMuted,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft,
                Dock = DockStyle.None
            };
        }

        // =====================================================
        // CARD PANEL
        // =====================================================
        public static Panel CreateCard(int x, int y, int width, int height)
        {
            var panel = new RoundedPanel(CornerRadius)
            {
                Location = new Point(x, y),
                Size = new Size(width, height),
                BackColor = BgSurface,
                Padding = new Padding(SectionPad)
            };
            return panel;
        }

        // =====================================================
        // BUTTON FACTORY
        // =====================================================
        public static Button CreateButton(string text, ButtonStyle style = ButtonStyle.Primary)
        {
            Color normalBg, hoverBg, textColor;

            switch (style)
            {
                case ButtonStyle.Ghost:
                    normalBg = Color.Transparent;
                    hoverBg = BgElevated;
                    textColor = TextSecondary;
                    break;
                case ButtonStyle.Danger:
                    normalBg = Color.FromArgb(80, 30, 30);
                    hoverBg = Danger;
                    textColor = TextPrimary;
                    break;
                case ButtonStyle.Success:
                    normalBg = Color.FromArgb(20, 70, 55);
                    hoverBg = Success;
                    textColor = TextPrimary;
                    break;
                default: // Primary
                    normalBg = AccentDim;
                    hoverBg = Accent;
                    textColor = TextPrimary;
                    break;
            }

            var btn = new Button
            {
                Text = text,
                Height = ButtonHeight,
                Width = ButtonWidth,
                FlatStyle = FlatStyle.Flat,
                BackColor = normalBg,
                ForeColor = textColor,
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleCenter,
                UseVisualStyleBackColor = false
            };

            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = BorderSubtle;
            btn.FlatAppearance.MouseOverBackColor = hoverBg;
            btn.FlatAppearance.MouseDownBackColor = AccentDim;

            btn.MouseEnter += (s, e) =>
            {
                btn.BackColor = hoverBg;
                btn.FlatAppearance.BorderColor = BorderBright;
            };
            btn.MouseLeave += (s, e) =>
            {
                btn.BackColor = normalBg;
                btn.FlatAppearance.BorderColor = BorderSubtle;
            };

            return btn;
        }

        // =====================================================
        // LABEL FACTORY
        // =====================================================
        public static Label CreateLabel(string text = "",
                                        LabelStyle style = LabelStyle.Body)
        {
            Font font;
            Color color;

            switch (style)
            {
                case LabelStyle.Caption:
                    font = new Font("Segoe UI", 8f);
                    color = TextMuted;
                    break;
                case LabelStyle.Value:
                    font = new Font("Segoe UI", 9f, FontStyle.Bold);
                    color = TextPrimary;
                    break;
                case LabelStyle.Stat:
                    font = new Font("Segoe UI", 14f, FontStyle.Bold);
                    color = Accent;
                    break;
                default: // Body
                    font = new Font("Segoe UI", 9f);
                    color = TextSecondary;
                    break;
            }

            return new Label
            {
                Text = text,
                AutoSize = true,
                Font = font,
                ForeColor = color,
                BackColor = Color.Transparent
            };
        }

        // =====================================================
        // SEPARATOR
        // =====================================================
        public static Panel CreateSeparator(int width)
        {
            return new Panel
            {
                Height = 1,
                Width = width,
                BackColor = BorderSubtle
            };
        }

        // =====================================================
        // COMBO BOX FACTORY
        // =====================================================
        public static ComboBox CreateComboBox(int width = 180)
        {
            var cmb = new ComboBox
            {
                Width = width,
                Height = ButtonHeight,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = BgControl,
                ForeColor = TextPrimary,
                Font = new Font("Segoe UI", 9f),
                FlatStyle = FlatStyle.Flat
            };
            return cmb;
        }

        // =====================================================
        // NUMERIC UPDOWN FACTORY
        // =====================================================
        public static NumericUpDown CreateNumericUpDown(decimal min, decimal max, decimal value, int width = 120)
        {
            var num = new NumericUpDown
            {
                Width = width,
                Height = ButtonHeight,
                Minimum = min,
                Maximum = max,
                Value = value,
                BackColor = BgControl,
                ForeColor = TextPrimary,
                Font = new Font("Segoe UI", 9f),
                BorderStyle = BorderStyle.FixedSingle
            };
            return num;
        }

        // =====================================================
        // PROGRESS BAR FACTORY
        // =====================================================
        public static ProgressBar CreateProgressBar(int width)
        {
            var pb = new ProgressBar
            {
                Width = width,
                Height = 6,
                Minimum = 0,
                Maximum = 100,
                Value = 0,
                Style = ProgressBarStyle.Continuous
            };
            return pb;
        }

        // =====================================================
        // APPLY THEME TO ENTIRE FORM
        // =====================================================
        public static void ApplyDarkTheme(Control root)
        {
            root.BackColor = BgDeep;
            root.ForeColor = TextPrimary;
        }
    }

    // =====================================================
    // ENUMS
    // =====================================================
    public enum ButtonStyle { Primary, Ghost, Danger, Success }
    public enum LabelStyle { Body, Caption, Value, Stat }

    // =====================================================
    // ROUNDED PANEL (GDI+)
    // =====================================================
    public class RoundedPanel : Panel
    {
        private readonly int _radius;

        public RoundedPanel(int radius = 8)
        {
            _radius = radius;
            DoubleBuffered = true;
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            using (var path = GetRoundedRect(ClientRectangle, _radius))
            using (var brush = new SolidBrush(BackColor))
            {
                e.Graphics.FillPath(brush, path);
            }

            // subtle border
            using (var path = GetRoundedRect(
                new Rectangle(0, 0, Width - 1, Height - 1), _radius))
            using (var pen = new Pen(DesignSystem.BorderSubtle, 1))
            {
                e.Graphics.DrawPath(pen, path);
            }
        }

        private static GraphicsPath GetRoundedRect(Rectangle rect, int radius)
        {
            int d = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
*/

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace WindowsFormsApp1.UI
{
    /// <summary>
    /// Single source of truth for all visual tokens, color palettes,
    /// typography constants, and shared control factory methods.
    /// </summary>
    public static class DesignSystem
    {
        // =====================================================
        // COLOR PALETTE — DARK THEME
        // =====================================================
        public static Color BgDeep = Color.FromArgb(10, 12, 20);   // outermost background
        public static Color BgSurface = Color.FromArgb(18, 20, 34);   // card/panel bg
        public static Color BgElevated = Color.FromArgb(26, 29, 50);   // elevated panel
        public static Color BgControl = Color.FromArgb(34, 38, 64);   // combobox / numericUpDown bg
        public static Color BorderSubtle = Color.FromArgb(50, 55, 90);   // panel border
        public static Color BorderBright = Color.FromArgb(80, 90, 140);   // active / hover border

        // Accent — electric violet-blue
        public static Color Accent = Color.FromArgb(99, 102, 241);  // primary CTA
        public static Color AccentHover = Color.FromArgb(118, 121, 255);
        public static Color AccentDim = Color.FromArgb(60, 62, 160);

        // Semantic colours
        public static Color Success = Color.FromArgb(52, 211, 153);
        public static Color Warning = Color.FromArgb(251, 191, 36);
        public static Color Danger = Color.FromArgb(248, 113, 113);

        // Text
        public static Color TextPrimary = Color.FromArgb(236, 237, 255);
        public static Color TextSecondary = Color.FromArgb(148, 150, 200);
        public static Color TextMuted = Color.FromArgb(90, 93, 140);

        // =====================================================
        // LAYOUT CONSTANTS
        // =====================================================
        public const int SectionPad = 16;   // internal section padding
        public const int ControlGap = 10;   // gap between sibling controls
        public const int SectionGap = 14;   // gap between sections
        public const int LabelHeight = 22;
        public const int ButtonHeight = 36;
        public const int ButtonWidth = 118;
        public const int CornerRadius = 8;

        // =====================================================
        // SECTION HEADER LABEL
        // =====================================================
        public static Label CreateSectionHeader(string text)
        {
            return new Label
            {
                Text = text.ToUpper(),
                AutoSize = false,
                Height = 20,
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = TextMuted,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft,
                Dock = DockStyle.None
            };
        }

        // =====================================================
        // CARD PANEL
        // =====================================================
        public static Panel CreateCard(int x, int y, int width, int height)
        {
            var panel = new RoundedPanel(CornerRadius)
            {
                Location = new Point(x, y),
                Size = new Size(width, height),
                BackColor = BgSurface,
                Padding = new Padding(SectionPad)
            };
            return panel;
        }

        // =====================================================
        // BUTTON FACTORY
        // =====================================================
        public static Button CreateButton(string text, ButtonStyle style = ButtonStyle.Primary)
        {
            Color normalBg, hoverBg, textColor;

            switch (style)
            {
                case ButtonStyle.Ghost:
                    normalBg = Color.Transparent;
                    hoverBg = BgElevated;
                    textColor = TextSecondary;
                    break;
                case ButtonStyle.Danger:
                    normalBg = Color.FromArgb(80, 30, 30);
                    hoverBg = Danger;
                    textColor = TextPrimary;
                    break;
                case ButtonStyle.Success:
                    normalBg = Color.FromArgb(20, 70, 55);
                    hoverBg = Success;
                    textColor = TextPrimary;
                    break;
                default: // Primary
                    normalBg = AccentDim;
                    hoverBg = Accent;
                    textColor = TextPrimary;
                    break;
            }

            var btn = new Button
            {
                Text = text,
                Height = ButtonHeight,
                Width = ButtonWidth,
                FlatStyle = FlatStyle.Flat,
                BackColor = normalBg,
                ForeColor = textColor,
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleCenter,
                UseVisualStyleBackColor = false
            };

            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = BorderSubtle;
            btn.FlatAppearance.MouseOverBackColor = hoverBg;
            btn.FlatAppearance.MouseDownBackColor = AccentDim;

            btn.MouseEnter += (s, e) =>
            {
                btn.BackColor = hoverBg;
                btn.FlatAppearance.BorderColor = BorderBright;
            };
            btn.MouseLeave += (s, e) =>
            {
                btn.BackColor = normalBg;
                btn.FlatAppearance.BorderColor = BorderSubtle;
            };

            return btn;
        }

        // =====================================================
        // LABEL FACTORY
        // =====================================================
        public static Label CreateLabel(string text = "",
                                        LabelStyle style = LabelStyle.Body)
        {
            Font font;
            Color color;

            switch (style)
            {
                case LabelStyle.Caption:
                    font = new Font("Segoe UI", 8f);
                    color = TextMuted;
                    break;
                case LabelStyle.Value:
                    font = new Font("Segoe UI", 9f, FontStyle.Bold);
                    color = TextPrimary;
                    break;
                case LabelStyle.Stat:
                    font = new Font("Segoe UI", 14f, FontStyle.Bold);
                    color = Accent;
                    break;
                default: // Body
                    font = new Font("Segoe UI", 9f);
                    color = TextSecondary;
                    break;
            }

            return new Label
            {
                Text = text,
                AutoSize = true,
                Font = font,
                ForeColor = color,
                BackColor = Color.Transparent
            };
        }

        // =====================================================
        // SEPARATOR
        // =====================================================
        public static Panel CreateSeparator(int width)
        {
            return new Panel
            {
                Height = 1,
                Width = width,
                BackColor = BorderSubtle
            };
        }

        // =====================================================
        // COMBO BOX FACTORY
        // =====================================================
        public static ComboBox CreateComboBox(int width = 180)
        {
            var cmb = new ComboBox
            {
                Width = width,
                Height = ButtonHeight,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = BgControl,
                ForeColor = TextPrimary,
                Font = new Font("Segoe UI", 9f),
                FlatStyle = FlatStyle.Flat
            };
            return cmb;
        }

        // =====================================================
        // NUMERIC UPDOWN FACTORY
        // =====================================================
        public static NumericUpDown CreateNumericUpDown(decimal min, decimal max, decimal value, int width = 120)
        {
            var num = new NumericUpDown
            {
                Width = width,
                Height = ButtonHeight,
                Minimum = min,
                Maximum = max,
                Value = value,
                BackColor = BgControl,
                ForeColor = TextPrimary,
                Font = new Font("Segoe UI", 9f),
                BorderStyle = BorderStyle.FixedSingle
            };
            return num;
        }

        // =====================================================
        // PROGRESS BAR FACTORY
        // =====================================================
        public static ProgressBar CreateProgressBar(int width)
        {
            var pb = new ProgressBar
            {
                Width = width,
                Height = 6,
                Minimum = 0,
                Maximum = 100,
                Value = 0,
                Style = ProgressBarStyle.Continuous
            };
            return pb;
        }

        // =====================================================
        // APPLY THEME TO ENTIRE FORM
        // Kept for backwards-compatibility.
        // ThemeManager is the preferred entry point for full
        // recursive theming across all controls.
        // =====================================================
        public static void ApplyDarkTheme(Control root)
        {
            root.BackColor = BgDeep;
            root.ForeColor = TextPrimary;
        }

        public static void ApplyLightTheme(Control root)
        {
            // BgDeep is already swapped to the light value by ThemeManager
            // before this is called, so we just propagate it.
            root.BackColor = BgDeep;
            root.ForeColor = TextPrimary;
        }
    }

    // =====================================================
    // ENUMS
    // =====================================================
    public enum ButtonStyle { Primary, Ghost, Danger, Success }
    public enum LabelStyle { Body, Caption, Value, Stat }

    // =====================================================
    // ROUNDED PANEL (GDI+)
    // =====================================================
    public class RoundedPanel : Panel
    {
        private readonly int _radius;

        public RoundedPanel(int radius = 8)
        {
            _radius = radius;
            DoubleBuffered = true;
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            using (var path = GetRoundedRect(ClientRectangle, _radius))
            using (var brush = new SolidBrush(BackColor))
            {
                e.Graphics.FillPath(brush, path);
            }

            // subtle border
            using (var path = GetRoundedRect(
                new Rectangle(0, 0, Width - 1, Height - 1), _radius))
            using (var pen = new Pen(DesignSystem.BorderSubtle, 1))
            {
                e.Graphics.DrawPath(pen, path);
            }
        }

        private static GraphicsPath GetRoundedRect(Rectangle rect, int radius)
        {
            int d = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}