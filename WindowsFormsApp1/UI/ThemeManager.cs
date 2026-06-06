using System;
using System.Drawing;
using System.Windows.Forms;

namespace WindowsFormsApp1.UI
{
    /// <summary>
    /// ThemeManager — مسؤول عن تبديل الثيم بين Dark و Light.
    ///
    /// يعمل عبر:
    ///   1. تحديث الـ static color fields في DesignSystem عند التبديل.
    ///   2. المرور بشكل recursive على كل Control وتطبيق الألوان الصحيحة.
    ///   3. إجبار كل RoundedPanel على إعادة الرسم لأنها تعتمد على GDI+.
    /// </summary>
    public sealed class ThemeManager
    {
        // =====================================================
        // STATE
        // =====================================================
        public bool IsDarkTheme { get; private set; } = true;

        // =====================================================
        // DARK PALETTE
        // =====================================================
        private static class Dark
        {
            public static readonly Color BgDeep = Color.FromArgb(10, 12, 20);
            public static readonly Color BgSurface = Color.FromArgb(18, 20, 34);
            public static readonly Color BgElevated = Color.FromArgb(26, 29, 50);
            public static readonly Color BgControl = Color.FromArgb(34, 38, 64);
            public static readonly Color BorderSubtle = Color.FromArgb(50, 55, 90);
            public static readonly Color BorderBright = Color.FromArgb(80, 90, 140);
            public static readonly Color TextPrimary = Color.FromArgb(236, 237, 255);
            public static readonly Color TextSecondary = Color.FromArgb(148, 150, 200);
            public static readonly Color TextMuted = Color.FromArgb(90, 93, 140);
        }

        // =====================================================
        // LIGHT PALETTE
        // =====================================================
        private static class Light
        {
            public static readonly Color BgDeep = Color.FromArgb(243, 244, 250);
            public static readonly Color BgSurface = Color.FromArgb(255, 255, 255);
            public static readonly Color BgElevated = Color.FromArgb(235, 237, 248);
            public static readonly Color BgControl = Color.FromArgb(245, 246, 252);
            public static readonly Color BorderSubtle = Color.FromArgb(210, 213, 230);
            public static readonly Color BorderBright = Color.FromArgb(150, 155, 200);
            public static readonly Color TextPrimary = Color.FromArgb(20, 22, 40);
            public static readonly Color TextSecondary = Color.FromArgb(70, 75, 120);
            public static readonly Color TextMuted = Color.FromArgb(140, 145, 180);
        }

        // =====================================================
        // TOGGLE
        // =====================================================
        /// <summary>
        /// يبدّل الثيم ويطبقه على الـ root control (عادةً الـ Form).
        /// </summary>
        public void ToggleTheme(Control root)
        {
            IsDarkTheme = !IsDarkTheme;
            ApplyTheme(root);
        }

        // =====================================================
        // APPLY
        // =====================================================
        /// <summary>
        /// يحدّث الـ DesignSystem static fields ثم يمشي على كل control.
        /// </summary>
        public void ApplyTheme(Control root)
        {
            // ── Step 1: حدّث DesignSystem ليعكس الثيم الجديد ──────────
            PushPaletteToDesignSystem();

            // ── Step 2: طبّق على كل control بشكل recursive ────────────
            ApplyToControl(root);

            // ── Step 3: أجبر الـ Form على إعادة الرسم ─────────────────
            root.Refresh();
        }

        // =====================================================
        // PUSH PALETTE → DesignSystem
        // =====================================================
        private void PushPaletteToDesignSystem()
        {
            if (IsDarkTheme)
            {
                DesignSystem.BgDeep = Dark.BgDeep;
                DesignSystem.BgSurface = Dark.BgSurface;
                DesignSystem.BgElevated = Dark.BgElevated;
                DesignSystem.BgControl = Dark.BgControl;
                DesignSystem.BorderSubtle = Dark.BorderSubtle;
                DesignSystem.BorderBright = Dark.BorderBright;
                DesignSystem.TextPrimary = Dark.TextPrimary;
                DesignSystem.TextSecondary = Dark.TextSecondary;
                DesignSystem.TextMuted = Dark.TextMuted;
            }
            else
            {
                DesignSystem.BgDeep = Light.BgDeep;
                DesignSystem.BgSurface = Light.BgSurface;
                DesignSystem.BgElevated = Light.BgElevated;
                DesignSystem.BgControl = Light.BgControl;
                DesignSystem.BorderSubtle = Light.BorderSubtle;
                DesignSystem.BorderBright = Light.BorderBright;
                DesignSystem.TextPrimary = Light.TextPrimary;
                DesignSystem.TextSecondary = Light.TextSecondary;
                DesignSystem.TextMuted = Light.TextMuted;
            }
        }

        // =====================================================
        // RECURSIVE CONTROL WALKER
        // =====================================================
        private void ApplyToControl(Control ctrl)
        {
            // طبّق حسب نوع الـ control
            if (ctrl is Form form)
                StyleForm(form);

            else if (ctrl is RoundedPanel rp)
                StyleRoundedPanel(rp);

            else if (ctrl is Panel panel && IsSeparator(panel))
                StyleSeparator(panel);

            else if (ctrl is Panel plainPanel)
                StylePanel(plainPanel);

            else if (ctrl is Button btn)
                StyleButton(btn);

            else if (ctrl is Label lbl)
                StyleLabel(lbl);

            else if (ctrl is ComboBox cmb)
                StyleComboBox(cmb);

            else if (ctrl is NumericUpDown num)
                StyleNumericUpDown(num);

            else if (ctrl is ProgressBar pb)
                StyleProgressBar(pb);

            else if (ctrl is GroupBox gb)
                StyleGroupBox(gb);

            // ── امشِ على الأبناء ──────────────────────────────────────
            foreach (Control child in ctrl.Controls)
                ApplyToControl(child);
        }

        // =====================================================
        // PER-TYPE STYLERS
        // =====================================================
        private void StyleForm(Form f)
        {
            f.BackColor = DesignSystem.BgDeep;
            f.ForeColor = DesignSystem.TextPrimary;
        }

        private void StyleRoundedPanel(RoundedPanel rp)
        {
            // الـ separator panels لها height == 1، ما نغيرها هنا
            rp.BackColor = DesignSystem.BgSurface;
            rp.ForeColor = DesignSystem.TextPrimary;
            rp.Invalidate(); // أجبره على إعادة رسم الـ GDI+ border
        }

        private void StylePanel(Panel p)
        {
            // الـ drop zone داخل FileSection هو Panel عادي مرفوع
            p.BackColor = DesignSystem.BgElevated;
            p.ForeColor = DesignSystem.TextPrimary;
        }

        private static bool IsSeparator(Panel p)
            => p.Height == 1;

        private void StyleSeparator(Panel sep)
        {
            sep.BackColor = DesignSystem.BorderSubtle;
        }

        private void StyleButton(Button btn)
        {
            // الأزرار الـ Danger و Success تحتفظ بألوانها الدلالية
            // نغير فقط الأزرار Primary و Ghost
            Color bg = btn.BackColor;

            // Ghost buttons: خلفية شفافة أو BgElevated
            bool isGhost = bg == Color.Transparent
                          || bg == Dark.BgElevated
                          || bg == Light.BgElevated;

            // Primary buttons: AccentDim
            bool isPrimary = bg == DesignSystem.AccentDim
                          || bg == Dark.BgElevated  // حالة hover السابقة
                          || bg == Light.BgElevated;

            // Danger: لا نغير
            bool isDanger = bg == Color.FromArgb(80, 30, 30)
                         || bg == DesignSystem.Danger;

            // Success: لا نغير
            bool isSuccess = bg == Color.FromArgb(20, 70, 55)
                          || bg == DesignSystem.Success;

            if (isDanger || isSuccess)
                return; // احتفظ بالألوان الدلالية

            if (isGhost)
            {
                btn.BackColor = Color.Transparent;
                btn.ForeColor = DesignSystem.TextSecondary;
            }
            else
            {
                // Primary
                btn.BackColor = DesignSystem.AccentDim;
                btn.ForeColor = DesignSystem.TextPrimary;
            }

            btn.FlatAppearance.BorderColor = DesignSystem.BorderSubtle;
        }

        private void StyleLabel(Label lbl)
        {
            // لا نلمس الـ BackColor لأن معظم الـ labels تستخدم Transparent
            // نحدد الـ ForeColor بناءً على الـ Font size كمؤشر على الـ LabelStyle

            if (lbl.BackColor != Color.Transparent)
                lbl.BackColor = Color.Transparent;

            float fontSize = lbl.Font.Size;

            if (fontSize >= 13f)
            {
                // Stat label — يبقى بلون الـ Accent
                lbl.ForeColor = DesignSystem.Accent;
            }
            else if (fontSize <= 8f)
            {
                // Caption label
                lbl.ForeColor = DesignSystem.TextMuted;
            }
            else if (lbl.Font.Bold)
            {
                // Value label
                lbl.ForeColor = DesignSystem.TextPrimary;
            }
            else
            {
                // Body / section header
                lbl.ForeColor = DesignSystem.TextSecondary;
            }
        }

        private void StyleComboBox(ComboBox cmb)
        {
            cmb.BackColor = DesignSystem.BgControl;
            cmb.ForeColor = DesignSystem.TextPrimary;
        }

        private void StyleNumericUpDown(NumericUpDown num)
        {
            num.BackColor = DesignSystem.BgControl;
            num.ForeColor = DesignSystem.TextPrimary;
        }

        private void StyleProgressBar(ProgressBar pb)
        {
            // ProgressBar لا يدعم تغيير الألوان بشكل كامل على Windows
            // لكن نغير الـ BackColor على الأقل
            pb.BackColor = DesignSystem.BgElevated;
        }

        private void StyleGroupBox(GroupBox gb)
        {
            gb.BackColor = DesignSystem.BgSurface;
            gb.ForeColor = DesignSystem.TextPrimary;
        }
    }
}