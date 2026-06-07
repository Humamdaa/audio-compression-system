using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace WindowsFormsApp1.UI
{
    /// <summary>
    /// Post-compression report (Requirement 10).
    /// Shows file size before/after, savings ratio, time taken, and the
    /// algorithm used together with its settings. Can be saved to a .txt file.
    /// </summary>
    public sealed class ReportForm : Form
    {
        private readonly string _reportText;

        public ReportForm(string algorithm, int sampleRate, int quantizationLevels,
                          long originalBytes, long compressedBytes, double elapsedMs)
        {
            _reportText = BuildReport(algorithm, sampleRate, quantizationLevels,
                                      originalBytes, compressedBytes, elapsedMs);

            // ---- window ----
            Text = "Compression Report";
            Width = 520;
            Height = 460;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = DesignSystem.BgDeep;
            ForeColor = DesignSystem.TextPrimary;

            // ---- title ----
            var title = new Label
            {
                Text = "📊  COMPRESSION REPORT",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = DesignSystem.TextPrimary,
                AutoSize = true,
                Location = new Point(16, 14)
            };
            Controls.Add(title);

            // ---- report body ----
            var box = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Text = _reportText,
                Font = new Font("Consolas", 10f),
                BackColor = DesignSystem.BgSurface,
                ForeColor = DesignSystem.TextPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                Location = new Point(16, 50),
                Size = new Size(472, 310),
                TabStop = false
            };
            Controls.Add(box);

            // ---- buttons ----
            var btnSave = DesignSystem.CreateButton("💾  Save Report", ButtonStyle.Success);
            btnSave.SetBounds(16, 372, 150, DesignSystem.ButtonHeight);
            btnSave.Click += (s, e) => SaveReport();
            Controls.Add(btnSave);

            var btnClose = DesignSystem.CreateButton("Close", ButtonStyle.Ghost);
            btnClose.SetBounds(372, 372, 116, DesignSystem.ButtonHeight);
            btnClose.Click += (s, e) => Close();
            Controls.Add(btnClose);

            AcceptButton = btnClose;
        }

        private void SaveReport()
        {
            using (var sfd = new SaveFileDialog
            {
                Filter = "Text File|*.txt",
                DefaultExt = "txt",
                FileName = "compression_report.txt",
                Title = "Save Compression Report"
            })
            {
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    File.WriteAllText(sfd.FileName, _reportText);
                    MessageBox.Show("Report saved.", "Saved",
                                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        // =====================================================
        // REPORT TEXT
        // =====================================================
        private static string BuildReport(string algorithm, int sampleRate, int quantizationLevels,
                                          long originalBytes, long compressedBytes, double elapsedMs)
        {
            double ratio = compressedBytes > 0 ? (double)originalBytes / compressedBytes : 0;
            double savings = originalBytes > 0
                ? (1.0 - (double)compressedBytes / originalBytes) * 100.0
                : 0;

            var sb = new StringBuilder();
            sb.AppendLine("=======================================");
            sb.AppendLine("        AUDIO COMPRESSION REPORT");
            sb.AppendLine("=======================================");
            sb.AppendLine();
            sb.AppendLine("ALGORITHM & SETTINGS");
            sb.AppendLine("---------------------------------------");
            sb.AppendLine($"  Algorithm          : {algorithm}");
            sb.AppendLine($"  Sample Rate        : {sampleRate} Hz");
            sb.AppendLine($"  Quantization Levels: {quantizationLevels}");
            sb.AppendLine($"  Sample Bit Rate    : {BitsPerLevel(quantizationLevels)} bits/sample");
            sb.AppendLine();
            sb.AppendLine("SIZE");
            sb.AppendLine("---------------------------------------");
            sb.AppendLine($"  Before Compression : {FormatBytes(originalBytes)}  ({originalBytes:N0} bytes)");
            sb.AppendLine($"  After Compression  : {FormatBytes(compressedBytes)}  ({compressedBytes:N0} bytes)");
            sb.AppendLine();
            sb.AppendLine("RESULTS");
            sb.AppendLine("---------------------------------------");
            sb.AppendLine($"  Compression Ratio  : {ratio:F2} ×");
            sb.AppendLine($"  Size Savings       : {savings:F1} %");
            sb.AppendLine($"  Time Taken         : {elapsedMs:F0} ms  ({elapsedMs / 1000.0:F2} s)");
            sb.AppendLine();
            sb.AppendLine($"  Generated          : {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine("=======================================");
            return sb.ToString();
        }

        private static int BitsPerLevel(int levels)
        {
            int bits = 0;
            int v = Math.Max(1, levels - 1);
            while (v > 0) { bits++; v >>= 1; }
            return Math.Max(1, bits);
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes <= 0) return "0 B";
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
            return $"{bytes / (1024.0 * 1024):F2} MB";
        }
    }
}
