using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

using AudioCompressor.Models;
using AudioCompressor.Services;
using WindowsFormsApp1.Services;

namespace WindowsFormsApp1.UI
{
    /// <summary>
    /// Owns the Compression card:
    ///   • Algorithm ComboBox
    ///   • Quantization NumericUpDown
    ///   • Compress / Decompress / Cancel buttons
    ///   • Animated progress bar
    ///   • BackgroundWorker lifecycle
    ///
    /// Exposes:
    ///   • Event CompressRequested(byte[] input, CompressionSettings settings)
    ///   • Event DecompressRequested()
    ///   • Event ResetRequested()
    ///   • Property Panel
    ///   • Property CurrentSettings
    ///   • Methods SetBusy / SetIdle / UpdateProgress / SetDecompressEnabled
    ///   • The BackgroundWorker is internal; Form1 wires DoWork/Completed via
    ///     the RegisterWorkerHandlers method.
    /// </summary>
    public sealed class CompressionSection
    {
        // =====================================================
        // PUBLIC SURFACE
        // =====================================================
        public Panel Panel { get; }

        /// <summary>Fired when the user clicks Compress with valid audio loaded.</summary>
        public event EventHandler<CompressionRequestArgs> CompressRequested;

        /// <summary>Fired when the user clicks Decompress.</summary>
        public event EventHandler DecompressRequested;

        /// <summary>Fired when the user clicks Reset.</summary>
        public event EventHandler ResetRequested;

        public CompressionSettings CurrentSettings { get; private set; } = new CompressionSettings();

        // The worker is exposed so Form1 can wire DoWork and RunWorkerCompleted.
        public BackgroundWorker Worker { get; }

        // =====================================================
        // PRIVATE CONTROLS
        // =====================================================
        private readonly ComboBox _cmbAlgorithm;
        private readonly NumericUpDown _numQuantization;
        private readonly Button _btnCompress;
        private readonly Button _btnDecompress;
        private readonly Button _btnCancel;
        private readonly Button _btnReset;
        private readonly ProgressBar _progressBar;
        private readonly Label _lblProgress;
        private readonly Label _lblAlgoCaption;
        private readonly Label _lblQuantCaption;

        // =====================================================
        // CONSTRUCTOR
        // =====================================================
        public CompressionSection(int x, int y, int width)
        {
            Panel = DesignSystem.CreateCard(x, y, width, 200);

            int pad = DesignSystem.SectionPad;
            int gap = DesignSystem.ControlGap;
            int bw = DesignSystem.ButtonWidth;
            int bh = DesignSystem.ButtonHeight;

            // --- Header ---
            var header = DesignSystem.CreateSectionHeader("🗜  Compression Engine");
            header.SetBounds(pad, pad, width - pad * 2, 20);
            Panel.Controls.Add(header);

            var sep = DesignSystem.CreateSeparator(width - pad * 2);
            sep.Location = new Point(pad, 36);
            Panel.Controls.Add(sep);

            // --- Algorithm row ---
            int row1Y = 50;

            _lblAlgoCaption = DesignSystem.CreateLabel("Algorithm", LabelStyle.Caption);
            _lblAlgoCaption.SetBounds(pad, row1Y, 80, DesignSystem.LabelHeight);
            Panel.Controls.Add(_lblAlgoCaption);

            _lblQuantCaption = DesignSystem.CreateLabel("Quantization Levels", LabelStyle.Caption);
            _lblQuantCaption.SetBounds(pad + 200 + gap, row1Y, 140, DesignSystem.LabelHeight);
            Panel.Controls.Add(_lblQuantCaption);

            int row2Y = row1Y + DesignSystem.LabelHeight + 2;

            _cmbAlgorithm = DesignSystem.CreateComboBox(190);
            _cmbAlgorithm.Location = new Point(pad, row2Y);
            _cmbAlgorithm.DataSource = Enum.GetValues(typeof(CompressionAlgorithm));
            Panel.Controls.Add(_cmbAlgorithm);

            _numQuantization = DesignSystem.CreateNumericUpDown(2, 1024, 256, 130);
            _numQuantization.Location = new Point(pad + 200 + gap, row2Y);
            Panel.Controls.Add(_numQuantization);

            // --- Buttons row ---
            int row3Y = row2Y + bh + gap * 2;
            int btnX = pad;

            _btnCompress = DesignSystem.CreateButton("🗜  Compress");
            _btnCompress.SetBounds(btnX, row3Y, bw, bh);
            Panel.Controls.Add(_btnCompress);

            btnX += bw + gap;
            _btnDecompress = DesignSystem.CreateButton("🔓  Decompress", ButtonStyle.Ghost);
            _btnDecompress.SetBounds(btnX, row3Y, bw, bh);
            _btnDecompress.Enabled = false;
            Panel.Controls.Add(_btnDecompress);

            btnX += bw + gap;
            _btnCancel = DesignSystem.CreateButton("✕  Cancel", ButtonStyle.Danger);
            _btnCancel.SetBounds(btnX, row3Y, bw, bh);
            _btnCancel.Enabled = false;
            Panel.Controls.Add(_btnCancel);

            btnX += bw + gap;
            _btnReset = DesignSystem.CreateButton("↩  Reset", ButtonStyle.Ghost);
            _btnReset.SetBounds(btnX, row3Y, bw, bh);
            Panel.Controls.Add(_btnReset);

            // --- Progress row ---
            int row4Y = row3Y + bh + gap + 4;

            _progressBar = DesignSystem.CreateProgressBar(width - pad * 2);
            _progressBar.Location = new Point(pad, row4Y);
            _progressBar.Visible = false;
            Panel.Controls.Add(_progressBar);

            _lblProgress = DesignSystem.CreateLabel("", LabelStyle.Caption);
            _lblProgress.SetBounds(pad, row4Y + 10, width - pad * 2, DesignSystem.LabelHeight);
            _lblProgress.Visible = false;
            Panel.Controls.Add(_lblProgress);

            // --- BackgroundWorker ---
            Worker = new BackgroundWorker
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };

            Worker.ProgressChanged += Worker_ProgressChanged;

            // --- Wire control events ---
            _cmbAlgorithm.SelectedIndexChanged += (s, e) => SyncSettings();
            _numQuantization.ValueChanged += (s, e) => SyncSettings();

            _btnCompress.Click += OnCompressClick;
            _btnDecompress.Click += (s, e) => DecompressRequested?.Invoke(this, EventArgs.Empty);
            _btnCancel.Click += (s, e) =>
            {
                if (Worker.IsBusy) Worker.CancelAsync();
            };
            _btnReset.Click += (s, e) =>
            {
                if (Worker.IsBusy) Worker.CancelAsync();
                ResetRequested?.Invoke(this, EventArgs.Empty);
            };

            SyncSettings();
        }

        // =====================================================
        // PUBLIC METHODS
        // =====================================================

        /// <summary>Call when compression starts to lock UI into busy state.</summary>
        public void SetBusy()
        {
            _btnCompress.Enabled = false;
            _btnDecompress.Enabled = false;
            _btnCancel.Enabled = true;
            _progressBar.Value = 0;
            _progressBar.Visible = true;
            _lblProgress.Text = "Compressing…";
            _lblProgress.Visible = true;
        }

        /// <summary>Call when compression ends (success, cancel, or error).</summary>
        public void SetIdle(bool decompressEnabled = false)
        {
            _btnCompress.Enabled = true;
            _btnDecompress.Enabled = decompressEnabled;
            _btnCancel.Enabled = false;
            _progressBar.Visible = false;
            _lblProgress.Visible = false;
        }

        /// <summary>Enable or disable the Decompress button.</summary>
        public void SetDecompressEnabled(bool enabled)
        {
            _btnDecompress.Enabled = enabled;
        }

        /// <summary>Build and return the current IAudioCompressionService for the selected algorithm.</summary>
        public IAudioCompressionService BuildService()
        {
            SyncSettings();
            var algo = (CompressionAlgorithm)_cmbAlgorithm.SelectedItem;

            switch (algo)
            {
                case CompressionAlgorithm.DPCM:
                    return new DpcmCompressionService();
                case CompressionAlgorithm.DeltaModulation:
                    return new DeltaCompressionService();
                case CompressionAlgorithm.NonLinearQuantization:
                    return new NonlinearQuantizationService();
                default:
                    throw new InvalidOperationException($"Unknown algorithm: {algo}");
            }
        }

        // =====================================================
        // PRIVATE
        // =====================================================
        private void SyncSettings()
        {
            if (_cmbAlgorithm.SelectedItem == null) return;

            CurrentSettings = new CompressionSettings
            {
                Algorithm = (CompressionAlgorithm)_cmbAlgorithm.SelectedItem,
                QuantizationLevels = (int)_numQuantization.Value
            };
        }

        private void OnCompressClick(object sender, EventArgs e)
        {
            SyncSettings();
            CompressRequested?.Invoke(this,
                new CompressionRequestArgs(CurrentSettings));
        }

        private void Worker_ProgressChanged(object sender,
                                             ProgressChangedEventArgs e)
        {
            _progressBar.Value = e.ProgressPercentage;
            _lblProgress.Text = $"Compressing…  {e.ProgressPercentage}%";
        }
    }

    // =====================================================
    // EVENT ARGS
    // =====================================================
    public sealed class CompressionRequestArgs : EventArgs
    {
        public CompressionSettings Settings { get; }

        public CompressionRequestArgs(CompressionSettings settings)
        {
            Settings = settings;
        }
    }
}