using System;
using System.Drawing;
using System.Windows.Forms;

using WindowsFormsApp1.Models;

namespace WindowsFormsApp1.UI
{
    public sealed class MetadataSection
    {
        // =====================================================
        // PUBLIC SURFACE
        // =====================================================
        public Panel MetadataPanel { get; }
        public Panel ResultsPanel { get; }

        public event EventHandler SaveRequested;

        // =====================================================
        // METADATA LABELS
        // =====================================================
        private readonly Label _lblFileName;
        private readonly Label _lblFileSize;
        private readonly Label _lblDuration;
        private readonly Label _lblSampleRate;
        private readonly Label _lblChannels;
        private readonly Label _lblBitRate;
        private readonly Label _lblCodec;

        // =====================================================
        // RESULTS LABELS + SAVE BUTTON
        // =====================================================
        private readonly Label _lblOriginalSize;
        private readonly Label _lblProcessedSize;
        private readonly Label _lblRatioValue;
        private readonly Button _btnSave;

        // =====================================================
        // CONSTRUCTOR
        // =====================================================
        public MetadataSection(int x, int y, int metaWidth, int resultsWidth, int height)
        {
            MetadataPanel = BuildMetadataCard(x, y, metaWidth, height,
                out _lblFileName, out _lblFileSize, out _lblDuration,
                out _lblSampleRate, out _lblChannels, out _lblBitRate,
                out _lblCodec);

            int resultsX = x + metaWidth + DesignSystem.SectionGap;
            ResultsPanel = BuildResultsCard(resultsX, y, resultsWidth, height,
                out _lblOriginalSize, out _lblProcessedSize,
                out _lblRatioValue, out _btnSave);

            _btnSave.Click += (s, e) => SaveRequested?.Invoke(this, EventArgs.Empty);
        }

        // =====================================================
        // PUBLIC UPDATE METHODS
        // =====================================================
        public void UpdateMetadata(AudioFileInfo info)
        {
            if (info == null)
            {
                ClearMetadata();
                return;
            }

            _lblFileName.Text = info.FileName;
            _lblFileSize.Text = info.FileSize;
            _lblDuration.Text = info.Duration;
            _lblSampleRate.Text = $"{info.SampleRate} Hz";
            _lblChannels.Text = info.Channels.ToString();
            _lblBitRate.Text = info.BitRate.ToString();
            _lblCodec.Text = info.CodecType;
        }

        public void UpdateResults(long originalBytes, long processedBytes)
        {
            _lblOriginalSize.Text = FormatBytes(originalBytes);
            _lblProcessedSize.Text = processedBytes > 0
                ? FormatBytes(processedBytes)
                : "—";

            if (processedBytes > 0 && originalBytes > 0)
            {
                double ratio = (double)originalBytes / processedBytes;
                _lblRatioValue.Text = $"{ratio:F2}×";
                _lblRatioValue.ForeColor = ratio >= 1.0
                    ? DesignSystem.Success
                    : DesignSystem.Warning;
            }
            else
            {
                _lblRatioValue.Text = "—";
                _lblRatioValue.ForeColor = DesignSystem.TextMuted;
            }

            _btnSave.Enabled = processedBytes > 0;
        }

        public void ClearResults()
        {
            _lblOriginalSize.Text = "—";
            _lblProcessedSize.Text = "—";
            _lblRatioValue.Text = "—";
            _lblRatioValue.ForeColor = DesignSystem.TextMuted;
            _btnSave.Enabled = false;
        }

        // =====================================================
        // PRIVATE BUILDERS
        // =====================================================
        private static Panel BuildMetadataCard(
            int x, int y, int width, int height,
            out Label lblFileName, out Label lblFileSize, out Label lblDuration,
            out Label lblSampleRate, out Label lblChannels,
            out Label lblBitRate, out Label lblCodec)
        {
            var card = DesignSystem.CreateCard(x, y, width, height);
            int pad = DesignSystem.SectionPad;
            int gap = 4;

            // Header
            var hdr = DesignSystem.CreateSectionHeader("📋  File Metadata");
            hdr.SetBounds(pad, pad, width - pad * 2, 20);
            card.Controls.Add(hdr);

            var sep = DesignSystem.CreateSeparator(width - pad * 2);
            sep.Location = new Point(pad, 36);
            card.Controls.Add(sep);

            // Rows: caption on left, value on right
            int rowY = 46;
            int colW = (width - pad * 2) / 2 - 4;

            lblFileName = AddMetaRow(card, pad, ref rowY, gap, colW, "File Name", "—");
            lblFileSize = AddMetaRow(card, pad, ref rowY, gap, colW, "Size", "—");
            lblDuration = AddMetaRow(card, pad, ref rowY, gap, colW, "Duration", "—");
            lblSampleRate = AddMetaRow(card, pad, ref rowY, gap, colW, "Sample Rate", "—");
            lblChannels = AddMetaRow(card, pad, ref rowY, gap, colW, "Channels", "—");
            lblBitRate = AddMetaRow(card, pad, ref rowY, gap, colW, "Bit Rate", "—");
            lblCodec = AddMetaRow(card, pad, ref rowY, gap, colW, "Codec", "—");

            return card;
        }

        private static Label AddMetaRow(
            Panel card, int pad, ref int y, int gap, int colW,
            string caption, string initialValue)
        {
            int rowH = DesignSystem.LabelHeight + gap;

            var lblCaption = DesignSystem.CreateLabel(caption, LabelStyle.Caption);
            lblCaption.SetBounds(pad, y, colW, DesignSystem.LabelHeight);
            lblCaption.AutoSize = false;
            card.Controls.Add(lblCaption);

            var lblValue = DesignSystem.CreateLabel(initialValue, LabelStyle.Value);
            lblValue.SetBounds(pad + colW + 8, y, colW, DesignSystem.LabelHeight);
            lblValue.AutoSize = false;
            card.Controls.Add(lblValue);

            y += rowH;
            return lblValue;
        }

        private static Panel BuildResultsCard(
            int x, int y, int width, int height,
            out Label lblOriginalSize, out Label lblProcessedSize,
            out Label lblRatioValue, out Button btnSave)
        {
            var card = DesignSystem.CreateCard(x, y, width, height);
            int pad = DesignSystem.SectionPad;

            // Header
            var hdr = DesignSystem.CreateSectionHeader("📊  Results");
            hdr.SetBounds(pad, pad, width - pad * 2, 20);
            card.Controls.Add(hdr);

            var sep = DesignSystem.CreateSeparator(width - pad * 2);
            sep.Location = new Point(pad, 36);
            card.Controls.Add(sep);

            // — Original Size stat block —
            var capOrig = DesignSystem.CreateLabel("Original Size", LabelStyle.Caption);
            capOrig.SetBounds(pad, 50, 120, DesignSystem.LabelHeight);
            capOrig.AutoSize = false;
            card.Controls.Add(capOrig);

            lblOriginalSize = DesignSystem.CreateLabel("—", LabelStyle.Value);
            lblOriginalSize.SetBounds(pad, 74, width - pad * 2, DesignSystem.LabelHeight);
            lblOriginalSize.AutoSize = false;
            card.Controls.Add(lblOriginalSize);

            // — Processed Size stat block —
            var capProc = DesignSystem.CreateLabel("Processed Size", LabelStyle.Caption);
            capProc.SetBounds(pad, 100, 120, DesignSystem.LabelHeight);
            capProc.AutoSize = false;
            card.Controls.Add(capProc);

            lblProcessedSize = DesignSystem.CreateLabel("—", LabelStyle.Value);
            lblProcessedSize.SetBounds(pad, 126, width - pad * 2, DesignSystem.LabelHeight);
            lblProcessedSize.AutoSize = false;
            card.Controls.Add(lblProcessedSize);

            // — Compression Ratio (large stat) —
            var capRatio = DesignSystem.CreateLabel("Compression Ratio", LabelStyle.Caption);
            capRatio.SetBounds(pad, 150, 140, DesignSystem.LabelHeight);
            capRatio.AutoSize = false;
            card.Controls.Add(capRatio);

            lblRatioValue = DesignSystem.CreateLabel("—", LabelStyle.Stat);
            lblRatioValue.SetBounds(pad, 168, width - pad * 2, 34);
            lblRatioValue.AutoSize = false;
            lblRatioValue.ForeColor = DesignSystem.TextMuted;
            card.Controls.Add(lblRatioValue);

            // — Save button —
            btnSave = DesignSystem.CreateButton("💾  Save File", ButtonStyle.Success);
            btnSave.SetBounds(pad, height - DesignSystem.ButtonHeight - pad,
                              width - pad * 2, DesignSystem.ButtonHeight);
            btnSave.Enabled = false;
            card.Controls.Add(btnSave);

            return card;
        }

        // =====================================================
        // PRIVATE HELPERS
        // =====================================================
        private static string FormatBytes(long bytes)
        {
            if (bytes <= 0) return "—";
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
            return $"{bytes / (1024.0 * 1024):F2} MB";
        }

        private void ClearMetadata()
        {
            _lblFileName.Text = "—";
            _lblFileSize.Text = "—";
            _lblDuration.Text = "—";
            _lblSampleRate.Text = "—";
            _lblChannels.Text = "—";
            _lblBitRate.Text = "—";
            _lblCodec.Text = "—";
        }
    }
}