using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

using AudioCompressor.Models;
using AudioCompressor.Services;
using WindowsFormsApp1.Services;
using WindowsFormsApp1.UI;

namespace WindowsFormsApp1
{
    /// <summary>
    /// Form1 — thin orchestrator.
    ///
    /// Responsibilities:
    ///   1. Instantiate services (metadata, player) and UI sections
    ///   2. Lay out the sections on the form
    ///   3. Wire cross-section events
    ///   4. Hold application state (original / processed audio, service, settings)
    ///   5. Drive the BackgroundWorker: real compression with live progress,
    ///      cancellation, performance charts, and a post-run report
    ///
    /// No UI construction code lives here — that is in the UI/*Section classes.
    /// </summary>
    public partial class Form1 : Form
    {
        // =====================================================
        // SERVICES
        // =====================================================
        private readonly AudioMetadataService _metadataService;
        private readonly AudioPlayerService _playerService;
        private readonly ThemeManager _themeManager = new ThemeManager();

        // =====================================================
        // APPLICATION STATE
        // =====================================================
        private string _selectedFilePath;
        private byte[] _originalAudio;
        private byte[] _processedAudio;
        private IAudioCompressionService _compressionService;
        private CompressionSettings _lastSettings;

        // =====================================================
        // REPORT STATE (Requirement 10) — snapshot of the last run
        // =====================================================
        private readonly Stopwatch _stopwatch = new Stopwatch();
        private bool _hasReport;
        private string _reportAlgorithm;
        private int _reportSampleRate;
        private int _reportQuantLevels;
        private long _reportOriginalBytes;
        private long _reportCompressedBytes;
        private double _reportElapsedMs;

        // =====================================================
        // UI SECTIONS
        // =====================================================
        private FileSection _fileSection;
        private CompressionSection _compressionSection;
        private MetadataSection _metadataSection;
        private MonitorSection _monitorSection;

        // =====================================================
        // CONSTRUCTOR
        // =====================================================
        public Form1()
        {
            _metadataService = new AudioMetadataService();
            _playerService = new AudioPlayerService();

            InitializeComponent();       // designer-generated (near-empty partial)
            ConfigureForm();
            BuildSections();
            WireEvents();
        }

        // =====================================================
        // FORM CONFIGURATION
        // =====================================================
        private void ConfigureForm()
        {
            Text = "Audio Compressor Pro";
            ClientSize = new Size(1020, 900);
            MinimumSize = new Size(900, 600);
            StartPosition = FormStartPosition.CenterScreen;
            AllowDrop = true;
            DoubleBuffered = true;
            AutoScroll = true; // safety net: scroll if the window is shorter than the content

            DesignSystem.ApplyDarkTheme(this);
        }

        // =====================================================
        // SECTION CONSTRUCTION
        // =====================================================
        private void BuildSections()
        {
            int margin = 20;
            int usableW = ClientSize.Width - margin * 2;

            BuildTopBar(margin);

            int fileY = 60;
            int fileH = 160;

            int comprY = fileY + fileH + DesignSystem.SectionGap;
            int comprH = 200;

            int metaY = comprY + comprH + DesignSystem.SectionGap;
            int metaH = 240;

            // ── File Section ──────────────────────────────────────────
            _fileSection = new FileSection(margin, fileY, usableW);
            Controls.Add(_fileSection.Panel);

            // ── Compression Section ───────────────────────────────────
            _compressionSection = new CompressionSection(margin, comprY, usableW);
            Controls.Add(_compressionSection.Panel);

            // ── Metadata + Results ────────────────────────────────────
            int metaW = (int)(usableW * 0.58);
            int resultsW = usableW - metaW - DesignSystem.SectionGap;

            _metadataSection = new MetadataSection(margin, metaY, metaW, resultsW, metaH);
            Controls.Add(_metadataSection.MetadataPanel);
            Controls.Add(_metadataSection.ResultsPanel);

            // ── Live Performance Monitor (Requirement 7) ──────────────
            int monitorY = metaY + metaH + DesignSystem.SectionGap;
            int monitorH = 260;

            _monitorSection = new MonitorSection(margin, monitorY, usableW, monitorH);
            Controls.Add(_monitorSection.Panel);
        }

        private void BuildTopBar(int margin)
        {
            var title = new Label
            {
                Text = "AUDIO COMPRESSOR PRO",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = DesignSystem.TextPrimary,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(margin, 18)
            };
            Controls.Add(title);

            var btnTheme = DesignSystem.CreateButton("🌗  Theme", ButtonStyle.Ghost);
            btnTheme.Width = 110;
            btnTheme.SetBounds(ClientSize.Width - margin - 110, 12, 110, 32);
            btnTheme.Click += (s, e) => ToggleTheme();
            Controls.Add(btnTheme);

            var div = DesignSystem.CreateSeparator(ClientSize.Width - margin * 2);
            div.Location = new Point(margin, 52);
            Controls.Add(div);
        }

        // =====================================================
        // EVENT WIRING
        // =====================================================
        private void WireEvents()
        {
            // File section
            _fileSection.FileRequested += (s, path) => LoadFile(path);
            _fileSection.PlayRequested += (s, e) => _playerService.Play(_selectedFilePath);
            _fileSection.StopRequested += (s, e) => _playerService.Stop();

            // Compression section
            _compressionSection.CompressRequested += OnCompressRequested;
            _compressionSection.DecompressRequested += OnDecompressRequested;
            _compressionSection.ResetRequested += OnResetRequested;
            _compressionSection.ViewReportRequested += (s, e) => ShowReport();

            // Results section
            _metadataSection.SaveRequested += OnSaveRequested;

            // BackgroundWorker — owned by CompressionSection, driven here
            _compressionSection.Worker.DoWork += Worker_DoWork;
            _compressionSection.Worker.ProgressChanged += Worker_ProgressChanged;
            _compressionSection.Worker.RunWorkerCompleted += Worker_RunWorkerCompleted;
        }

        // =====================================================
        // LOAD FILE
        // =====================================================
        private void LoadFile(string path)
        {
            try
            {
                _selectedFilePath = path;
                _originalAudio = File.ReadAllBytes(path);
                _processedAudio = null;

                _fileSection.SetCurrentFile(path);
                _metadataSection.ClearResults();

                var info = _metadataService.GetAudioInfo(path);
                _metadataSection.UpdateMetadata(info);
                _metadataSection.UpdateResults(_originalAudio.Length, 0);

                _compressionSection.SetDecompressEnabled(false);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not load the file:\n{ex.Message}",
                                "Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // =====================================================
        // COMPRESS
        // =====================================================
        private void OnCompressRequested(object sender, CompressionRequestArgs e)
        {
            if (_originalAudio == null)
            {
                MessageBox.Show("Please load an audio file first.",
                                "No File Loaded", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _lastSettings = e.Settings;
            _compressionService = _compressionSection.BuildService();

            _monitorSection.Reset();
            _compressionSection.SetBusy();
            _compressionSection.Worker.RunWorkerAsync(_originalAudio);
        }

        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            byte[] inputData = (byte[])e.Argument;
            var worker = (BackgroundWorker)sender;

            _stopwatch.Restart();

            // Real compression that reports live progress and honours cancellation.
            byte[] result = _compressionService.Compress(
                inputData, _lastSettings,
                p => worker.ReportProgress(p.Percent, p),
                () => worker.CancellationPending);

            _stopwatch.Stop();

            if (result == null || worker.CancellationPending)
            {
                e.Cancel = true;
                return;
            }

            e.Result = result;
        }

        private void Worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (e.UserState is CompressionProgress p)
                _monitorSection.AddPoint(p);
        }

        private void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                _monitorSection.SetFinal("Compression cancelled.");
                _compressionSection.SetIdle(decompressEnabled: false);

                MessageBox.Show("Compression was cancelled.",
                                "Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else if (e.Error != null)
            {
                _monitorSection.SetFinal("Error: " + e.Error.Message);
                _compressionSection.SetIdle(decompressEnabled: false);

                MessageBox.Show($"An error occurred during compression:\n{e.Error.Message}",
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                _processedAudio = (byte[])e.Result;

                _metadataSection.UpdateResults(_originalAudio.Length, _processedAudio.Length);
                _compressionSection.SetIdle(decompressEnabled: true);

                // Build + present the report (Requirement 10)
                CaptureReport();
                _compressionSection.SetReportEnabled(true);

                double savings = _reportOriginalBytes > 0
                    ? (1.0 - (double)_reportCompressedBytes / _reportOriginalBytes) * 100.0
                    : 0;
                _monitorSection.SetFinal(
                    $"Done — {_reportElapsedMs:F0} ms  •  saved {savings:F1}%  •  " +
                    $"ratio {Ratio():F2}×");

                ShowReport();
            }
        }

        // =====================================================
        // REPORT (Requirement 10)
        // =====================================================
        private void CaptureReport()
        {
            _reportAlgorithm = _lastSettings.Algorithm.ToString();
            _reportSampleRate = _lastSettings.SampleRate;
            _reportQuantLevels = _lastSettings.QuantizationLevels;
            _reportOriginalBytes = _originalAudio?.Length ?? 0;
            _reportCompressedBytes = _processedAudio?.Length ?? 0;
            _reportElapsedMs = _stopwatch.Elapsed.TotalMilliseconds;
            _hasReport = true;
        }

        private double Ratio()
        {
            return _reportCompressedBytes > 0
                ? (double)_reportOriginalBytes / _reportCompressedBytes
                : 0;
        }

        private void ShowReport()
        {
            if (!_hasReport)
            {
                MessageBox.Show("No report yet — run a compression first.",
                                "No Report", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var dlg = new ReportForm(
                _reportAlgorithm, _reportSampleRate, _reportQuantLevels,
                _reportOriginalBytes, _reportCompressedBytes, _reportElapsedMs))
            {
                dlg.ShowDialog(this);
            }
        }

        // =====================================================
        // DECOMPRESS
        // =====================================================
        private void OnDecompressRequested(object sender, EventArgs e)
        {
            if (_processedAudio == null)
            {
                MessageBox.Show("No compressed audio to decompress.",
                                "Nothing to Decompress", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (_compressionService == null)
            {
                MessageBox.Show("Please select an algorithm and compress first.",
                                "No Service", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _processedAudio = _compressionService.Decompress(_processedAudio, _lastSettings);

            _metadataSection.UpdateResults(_originalAudio?.Length ?? 0, _processedAudio.Length);

            MessageBox.Show("Decompression completed.",
                            "Done", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // =====================================================
        // RESET (Requirement 9)
        // =====================================================
        private void OnResetRequested(object sender, EventArgs e)
        {
            _processedAudio = null;
            _compressionService = null;
            _lastSettings = null;
            _hasReport = false;

            _metadataSection.ClearResults();
            if (_originalAudio != null)
                _metadataSection.UpdateResults(_originalAudio.Length, 0);

            _monitorSection.Reset();
            _monitorSection.SetFinal("Idle — load a file and press Compress.");

            _compressionSection.SetIdle(decompressEnabled: false);
            _compressionSection.SetReportEnabled(false);

            MessageBox.Show("Reset completed. You can select a new algorithm.",
                            "Reset", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // =====================================================
        // SAVE (Requirement 11)
        // =====================================================
        private void OnSaveRequested(object sender, EventArgs e)
        {
            if (_processedAudio == null)
            {
                MessageBox.Show("Nothing to save.",
                                "No Processed Audio", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var sfd = new SaveFileDialog
            {
                Filter = "Binary File|*.bin|Audio File|*.wav",
                DefaultExt = "bin",
                Title = "Save Compressed Audio"
            })
            {
                if (sfd.ShowDialog() == DialogResult.OK)
                    File.WriteAllBytes(sfd.FileName, _processedAudio);
            }
        }

        // =====================================================
        // THEME
        // =====================================================
        private void ToggleTheme()
        {
            _themeManager.ToggleTheme(this);
        }

        // =====================================================
        // DESIGNER-REQUIRED HANDLER (Form1.Designer.cs wires this.Load)
        // =====================================================
        private void Form1_Load(object sender, EventArgs e) { }
    }
}
