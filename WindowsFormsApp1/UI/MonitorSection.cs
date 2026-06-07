using System;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

using AudioCompressor.Services;

namespace WindowsFormsApp1.UI
{
    /// <summary>
    /// Live performance monitor (Requirement 7).
    /// Hosts two real-time charts driven by <see cref="CompressionProgress"/>:
    ///   • Compression ratio vs. progress
    ///   • Processing speed (MB/s) vs. progress
    /// Plus a single-line live readout.
    ///
    /// All update methods must be called on the UI thread (the BackgroundWorker's
    /// ProgressChanged handler runs there already).
    /// </summary>
    public sealed class MonitorSection
    {
        public Panel Panel { get; }

        private readonly Chart _ratioChart;
        private readonly Chart _speedChart;
        private readonly Series _ratioSeries;
        private readonly Series _speedSeries;
        private readonly Label _lblLive;

        public MonitorSection(int x, int y, int width, int height)
        {
            Panel = DesignSystem.CreateCard(x, y, width, height);
            int pad = DesignSystem.SectionPad;

            var header = DesignSystem.CreateSectionHeader("📈  Live Performance Monitor");
            header.SetBounds(pad, pad, width - pad * 2, 20);
            Panel.Controls.Add(header);

            var sep = DesignSystem.CreateSeparator(width - pad * 2);
            sep.Location = new Point(pad, 36);
            Panel.Controls.Add(sep);

            int innerW = width - pad * 2;
            int gap = DesignSystem.ControlGap;
            int chartW = (innerW - gap) / 2;
            int chartY = 46;
            int chartH = height - chartY - 34;

            _ratioChart = BuildChart("Compression Ratio (×)", DesignSystem.Accent, out _ratioSeries);
            _ratioChart.SetBounds(pad, chartY, chartW, chartH);
            Panel.Controls.Add(_ratioChart);

            _speedChart = BuildChart("Processing Speed (MB/s)", DesignSystem.Success, out _speedSeries);
            _speedChart.SetBounds(pad + chartW + gap, chartY, chartW, chartH);
            Panel.Controls.Add(_speedChart);

            _lblLive = DesignSystem.CreateLabel("Idle — load a file and press Compress.", LabelStyle.Caption);
            _lblLive.SetBounds(pad, height - 26, innerW, DesignSystem.LabelHeight);
            _lblLive.AutoSize = false;
            Panel.Controls.Add(_lblLive);
        }

        // =====================================================
        // PUBLIC UPDATE API
        // =====================================================
        public void Reset()
        {
            _ratioSeries.Points.Clear();
            _speedSeries.Points.Clear();
            _lblLive.Text = "Compressing…";
        }

        public void AddPoint(CompressionProgress p)
        {
            if (p == null) return;

            _ratioSeries.Points.AddXY(p.Percent, p.Ratio);
            _speedSeries.Points.AddXY(p.Percent, p.SpeedMBPerSec);

            _lblLive.Text =
                $"Progress {p.Percent}%   •   Ratio {p.Ratio:F2}×   •   " +
                $"Speed {p.SpeedMBPerSec:F2} MB/s   •   {p.ElapsedSeconds:F2}s";
        }

        public void SetFinal(string text)
        {
            _lblLive.Text = text;
        }

        // =====================================================
        // CHART BUILDER
        // =====================================================
        private static Chart BuildChart(string yTitle, Color lineColor, out Series series)
        {
            var chart = new Chart
            {
                BackColor = DesignSystem.BgElevated,
                Palette = ChartColorPalette.None
            };

            var area = new ChartArea("main")
            {
                BackColor = DesignSystem.BgElevated
            };

            // X axis — progress percentage
            area.AxisX.Title = "Progress (%)";
            area.AxisX.Minimum = 0;
            area.AxisX.Maximum = 100;
            area.AxisX.Interval = 20;
            StyleAxis(area.AxisX);

            // Y axis — value
            area.AxisY.Title = yTitle;
            area.AxisY.Minimum = 0;
            StyleAxis(area.AxisY);

            chart.ChartAreas.Add(area);

            series = new Series("data")
            {
                ChartType = SeriesChartType.FastLine,
                Color = lineColor,
                BorderWidth = 2,
                XValueType = ChartValueType.Double,
                YValueType = ChartValueType.Double
            };
            chart.Series.Add(series);

            return chart;
        }

        private static void StyleAxis(Axis axis)
        {
            axis.LineColor = DesignSystem.BorderSubtle;
            axis.TitleForeColor = DesignSystem.TextSecondary;
            axis.LabelStyle.ForeColor = DesignSystem.TextMuted;
            axis.MajorGrid.LineColor = DesignSystem.BorderSubtle;
            axis.MajorTickMark.LineColor = DesignSystem.BorderSubtle;
            axis.TitleFont = new Font("Segoe UI", 7.5f, FontStyle.Bold);
            axis.LabelStyle.Font = new Font("Segoe UI", 7f);
        }
    }
}
