/*using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

using WindowsFormsApp1.Services;
using AudioCompressor.Models;
using AudioCompressor.Services;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        // ================= SERVICES =================
        private readonly AudioMetadataService metadataService;
        private readonly AudioPlayerService playerService;

        private IAudioCompressionService _compressionService;

        // ================= STATE =================
        private string selectedFilePath;
        private byte[] _originalAudio;
        private byte[] _processedAudio;

        private CompressionSettings _settings = new CompressionSettings();

        // ================= UI =================
        private Button btnSelectFile, btnPlay, btnStop;
        private Button btnCompress, btnReset, btnSave;
        private Button btnDecompress;
        private Button btnCancel;

        private ComboBox cmbAlgorithm;
        private NumericUpDown numQuantization;

        private Label lblOriginalSize, lblProcessedSize, lblCompressionRatio;

        private Panel dropPanel;
        private Label lblDrop;

        private ProgressBar progressBar;
        private System.ComponentModel.BackgroundWorker compressionWorker;

        // ================ METADATA ==============
        private Label lblFileName;
        private Label lblFileSize;
        private Label lblDuration;
        private Label lblSampleRate;
        private Label lblChannels;
        private Label lblBitRate;
        private Label lblCodec;
        // ================= THEME =================
        private bool isDark = true;

        private Color darkBg = Color.FromArgb(18, 18, 30);
        private Color lightBg = Color.FromArgb(240, 240, 240);

        private Color darkPanel = Color.FromArgb(28, 28, 45);
        private Color lightPanel = Color.White;

        private Color accent = Color.FromArgb(0, 120, 215);

        public Form1()
        {
            metadataService = new AudioMetadataService();
            playerService = new AudioPlayerService();

            InitializeForm();
            InitializeControls();
            BindEvents();
        }

        // =====================================================
        // FORM
        // =====================================================
        private void InitializeForm()
        {
            Text = "Audio Compressor Pro (UX Refactored)";
            Width = 1000;
            Height = 750;

            ApplyTheme();
            StartPosition = FormStartPosition.CenterScreen;
            AllowDrop = true;
        }

        // =====================================================
        // THEME SYSTEM (DARK / LIGHT MODE)
        // =====================================================
        private void ApplyTheme()
        {

            Color currentBg = isDark ? darkBg : lightBg;
            Color currentForeColor = isDark ? Color.White : Color.Black;


            BackColor = currentBg;
            ForeColor = currentForeColor;


            foreach (Control ctrl in Controls)
            {
 
                if (ctrl is ComboBox || ctrl is NumericUpDown)
                {
                    ctrl.BackColor = Color.FromArgb(40, 40, 60);
                    ctrl.ForeColor = Color.White;
                    continue; 
                }

                if (ctrl is Label && ctrl != lblDrop)
                {
                    ctrl.ForeColor = currentForeColor;
                }

                else if (ctrl is Panel && ctrl == dropPanel)
                {
                    ctrl.BackColor = darkPanel;

                    lblDrop.ForeColor = Color.Gray;
                }
            }
        }
        private void ToggleTheme()
        {
            isDark = !isDark;
            ApplyTheme();

        }

        // =====================================================
        // UI INIT
        // =====================================================
        private void InitializeControls()
        {
            lblFileName = CreateLabel(20, 170);
            lblFileSize = CreateLabel(20, 195);
            lblDuration = CreateLabel(20, 220);
            lblSampleRate = CreateLabel(20, 245);
            lblChannels = CreateLabel(20, 270);
            lblBitRate = CreateLabel(20, 295);
            lblCodec = CreateLabel(20, 320);

            btnSelectFile = CreateButton("📥 Load", 20, 20);
            btnPlay = CreateButton("▶️ Play", 140, 20);
            btnStop = CreateButton("⏹️ Stop", 260, 20);
            btnCompress = CreateButton("🗜Compress", 380, 20);

            btnReset = CreateButton("↩️ Reset", 500, 20);
            btnSave = CreateButton("💾 Save", 620, 20);

            // THEME SWITCH BUTTON
            var btnTheme = CreateButton("🌗 Theme", 740, 20);
            btnTheme.Click += (s, e) => ToggleTheme();

            Controls.AddRange(new Control[]
            {
                btnSelectFile, btnPlay, btnStop,
                btnCompress, btnReset, btnSave, btnTheme
            });
            btnCancel = CreateButton("🛑 Cancel", 980, 20); 
            btnCancel.Enabled = false; 
            Controls.Add(btnCancel);

            dropPanel = new Panel
            {
                Location = new Point(20, 100),
                Size = new Size(250, 50), 
                BackColor = darkPanel,
                BorderStyle = BorderStyle.FixedSingle,
                AllowDrop = true
            };

            lblDrop = new Label
            {
                Text = "📥 Drag & Drop Audio Here",
                ForeColor = Color.Gray,
                AutoSize = false,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };

            dropPanel.Controls.Add(lblDrop);
            Controls.Add(dropPanel);

            btnDecompress = CreateButton("🔓 Decompress", 860, 20);
            Controls.Add(btnDecompress);

            progressBar = new ProgressBar
            {
                Location = new Point(20, 360), 
                Width = 300,
                Height = 25,
                Minimum = 0,
                Maximum = 100,
                Value = 0,
                Visible = false 
            };
            Controls.Add(progressBar);


            compressionWorker = new System.ComponentModel.BackgroundWorker
            {
                WorkerReportsProgress = true, 
                WorkerSupportsCancellation = true 
            };

            // ================= ALGORITHM =================
            cmbAlgorithm = new ComboBox
            {
                Location = new Point(20, 70),
                Width = 200,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            cmbAlgorithm.DataSource = Enum.GetValues(typeof(CompressionAlgorithm));
            Controls.Add(cmbAlgorithm);

            // ================= QUANTIZATION =================
            numQuantization = new NumericUpDown
            {
                Location = new Point(240, 70),
                Width = 120,
                Minimum = 2,
                Maximum = 1024,
                Value = 256
            };

            Controls.Add(numQuantization);

            

            // ================= METRICS =================
            lblOriginalSize = CreateLabel(350, 190);
            lblProcessedSize = CreateLabel(350, 230);
            lblCompressionRatio = CreateLabel(350, 250);
        }

        // =====================================================
        // EVENTS
        // =====================================================
        private void BindEvents()
        {
            btnSelectFile.Click += SelectFile_Click;
            btnPlay.Click += (s, e) => playerService.Play(selectedFilePath);
            btnStop.Click += (s, e) => playerService.Stop();

            btnCancel.Click += (s, e) => {
                if (compressionWorker.IsBusy)
                {
                    compressionWorker.CancelAsync();
                }
            };

            btnCompress.Click += Compress_Click;
            btnReset.Click += Reset_Click;
            btnSave.Click += Save_Click;

            cmbAlgorithm.SelectedIndexChanged += (s, e) => UpdateSettings();
            numQuantization.ValueChanged += (s, e) => UpdateSettings();


            dropPanel.DragEnter += DropPanel_DragEnter;
            dropPanel.DragLeave += DropPanel_DragLeave;
            dropPanel.DragDrop += DropPanel_DragDrop;
            btnDecompress.Click += Decompress_Click;

            compressionWorker.DoWork += CompressionWorker_DoWork;
            compressionWorker.ProgressChanged += CompressionWorker_ProgressChanged;
            compressionWorker.RunWorkerCompleted += CompressionWorker_RunWorkerCompleted;
        }

        // =====================================================
        // SETTINGS
        // =====================================================
        private void UpdateSettings()
        {
            if (cmbAlgorithm.SelectedItem == null) return;

            _settings.Algorithm = (CompressionAlgorithm)cmbAlgorithm.SelectedItem;
            _settings.QuantizationLevels = (int)numQuantization.Value;
        }

        // =====================================================
        // LOAD
        // =====================================================
        private void SelectFile_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog d = new OpenFileDialog())
            {
                d.Filter = "Audio|*.wav;*.mp3;*.aac";

                if (d.ShowDialog() == DialogResult.OK)
                    LoadFile(d.FileName);
            }
        }

        private void LoadFile(string path)
        {
            selectedFilePath = path;

            _originalAudio = File.ReadAllBytes(path);
            _processedAudio = null;

            ResetMetrics();

            // ================= METADATA (RESTORED FEATURE) =================
            var info = metadataService.GetAudioInfo(path);

            lblFileName.Text = $"File: {info.FileName}";
            lblFileSize.Text = $"Size: {info.FileSize}";
            lblDuration.Text = $"Duration: {info.Duration}";
            lblSampleRate.Text = $"Sample Rate: {info.SampleRate} Hz";
            lblChannels.Text = $"Channels: {info.Channels}";
            lblBitRate.Text = $"Bit Rate: {info.BitRate}";
            lblCodec.Text = $"Codec: {info.CodecType}";
        }

        // =====================================================
        // COMPRESS
        // =====================================================
        private void Compress_Click(object sender, EventArgs e)
        {
            if (_originalAudio == null) return;

            UpdateSettings();
            SelectService();


            btnCompress.Enabled = false;
            progressBar.Value = 0;
            progressBar.Visible = true;
            btnCancel.Enabled = true;

            compressionWorker.RunWorkerAsync(_originalAudio);
        }

        // =====================================================
        // RESET (REPLACES DECOMPRESS BUTTON)
        // =====================================================
        private void Reset_Click(object sender, EventArgs e)
        {
            if (compressionWorker.IsBusy)
            {
                compressionWorker.CancelAsync();
            }

            _processedAudio = null;
            _compressionService = null;

            UpdateMetrics();

            MessageBox.Show("Reset completed. You can select a new algorithm now.");
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        // =====================================================
        // SAVE COMPRESSED FILE (NEW FEATURE)
        // =====================================================
        private void Save_Click(object sender, EventArgs e)
        {
            if (_processedAudio == null)
            {
                MessageBox.Show("Nothing to save.");
                return;
            }

            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "Binary File|*.bin|Audio File|*.wav";

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    File.WriteAllBytes(sfd.FileName, _processedAudio);
                }
            }
        }

        // =====================================================
        // ALGORITHM SELECTOR
        // =====================================================
        private void SelectService()
        {
            var algo = (CompressionAlgorithm)cmbAlgorithm.SelectedItem;

            switch (algo)
            {
                case CompressionAlgorithm.DPCM:
                    _compressionService = new DpcmCompressionService();
                    break;

                case CompressionAlgorithm.DeltaModulation:
                    _compressionService = new DeltaCompressionService();
                    break;

                case CompressionAlgorithm.NonLinearQuantization:
                    _compressionService = new NonlinearQuantizationService();
                    break;
            }
        }

        // =====================================================
        // METRICS
        // =====================================================
        private void UpdateMetrics()
        {
            if (_originalAudio == null) return;

            double original = _originalAudio.Length;
            double processed = _processedAudio?.Length ?? 0;

            lblOriginalSize.Text = $"Original: {original} bytes";
            lblProcessedSize.Text = $"Processed: {processed} bytes";

            lblCompressionRatio.Text =
                processed > 0 ? $"Ratio: {(original / processed):F2}" : "Ratio: N/A";

            btnDecompress.Enabled = _processedAudio != null;
        }

        private void ResetMetrics()
        {
            lblOriginalSize.Text = "";
            lblProcessedSize.Text = "";
            lblCompressionRatio.Text = "";
        }

        // =====================================================
        // HELPERS
        // =====================================================
        private Button CreateButton(string text, int x, int y)
        {
            var btn = new Button
            {
                Text = text,
                Location = new Point(x, y),
                Width = 120,
                Height = 35,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(40, 40, 60),
                ForeColor = Color.White
            };

            btn.MouseEnter += (s, e) => btn.BackColor = accent;
            btn.MouseLeave += (s, e) => btn.BackColor = Color.FromArgb(40, 40, 60);

            return btn;
        }

        private Label CreateLabel(int x, int y)
        {
            var lbl = new Label
            {
                Location = new Point(x, y),
                AutoSize = true,
                ForeColor = this.ForeColor
            };

            Controls.Add(lbl);
            return lbl;
        }
        private void DropPanel_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
                dropPanel.BackColor = accent; 
            }
        }

        private void DropPanel_DragLeave(object sender, EventArgs e)
        {
            dropPanel.BackColor = darkPanel;
        }

        private void DropPanel_DragDrop(object sender, DragEventArgs e)
        {
            dropPanel.BackColor = darkPanel;

            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

            if (files.Length > 0)
            {
                string file = files[0];
                string ext = Path.GetExtension(file).ToLower();

                if (ext == ".wav" || ext == ".mp3" || ext == ".aac")
                {
                    LoadFile(file);
                }
                else
                {
                    MessageBox.Show("Unsupported file type.");
                }
            }
        }

        private void Decompress_Click(object sender, EventArgs e)
        {
            if (_processedAudio == null)
            {
                MessageBox.Show("No compressed audio to decompress.");
                return;
            }

            if (_compressionService == null)
            {
                MessageBox.Show("Select algorithm first.");
                return;
            }

            _processedAudio = _compressionService.Decompress(_processedAudio, _settings);

            UpdateMetrics();

            MessageBox.Show("Decompression completed.");
        }
        private void CompressionWorker_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            byte[] inputData = (byte[])e.Argument;

            for (int i = 1; i <= 100; i++)
            {
                if (compressionWorker.CancellationPending)
                {
                    e.Cancel = true;
                    return;
                }

                System.Threading.Thread.Sleep(10);
                compressionWorker.ReportProgress(i);
            }

            e.Result = _compressionService.Compress(inputData, _settings);
        }

        private void CompressionWorker_ProgressChanged(object sender, System.ComponentModel.ProgressChangedEventArgs e)
        {
            progressBar.Value = e.ProgressPercentage;
        }

        private void CompressionWorker_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                MessageBox.Show("Compression process was cancelled");
                progressBar.Visible = false;
            }
            else if (e.Error != null)
            {
                MessageBox.Show($"An error occurred during compression: {e.Error.Message}");
            }
            else
            {
                _processedAudio = (byte[])e.Result;
                UpdateMetrics();
                MessageBox.Show("Compression completed successfully");
            }

            btnCompress.Enabled = true;
            progressBar.Visible = false;
            btnCancel.Enabled = false;
        }

    }
}
*/
/*

using System;
using System.ComponentModel;
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
    ///   1. Instantiate services (AudioMetadataService, AudioPlayerService)
    ///   2. Instantiate the four UI sections
    ///   3. Add section panels to the form
    ///   4. Wire cross-section events
    ///   5. Hold application state (_originalAudio, _processedAudio, _compressionService)
    ///   6. Coordinate BackgroundWorker DoWork / RunWorkerCompleted
    ///
    /// No UI construction code lives here.
    /// </summary>
    public partial class Form1 : Form
    {
        // =====================================================
        // SERVICES
        // =====================================================
        private readonly AudioMetadataService _metadataService;
        private readonly AudioPlayerService _playerService;

        // =====================================================
        // APPLICATION STATE
        // =====================================================
        private string _selectedFilePath;
        private byte[] _originalAudio;
        private byte[] _processedAudio;
        private IAudioCompressionService _compressionService;
        private CompressionSettings _lastSettings;

        // =====================================================
        // UI SECTIONS
        // =====================================================
        private FileSection _fileSection;
        private CompressionSection _compressionSection;
        private MetadataSection _metadataSection;

        // =====================================================
        // CONSTRUCTOR
        // =====================================================
        public Form1()
        {
            _metadataService = new AudioMetadataService();
            _playerService = new AudioPlayerService();

            InitializeComponent();       // designer-generated (empty partial)
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
            Width = 1020;
            Height = 760;
            MinimumSize = new Size(1020, 780);
            StartPosition = FormStartPosition.CenterScreen;
            AllowDrop = true;
            DoubleBuffered = true;

            DesignSystem.ApplyDarkTheme(this);
        }

        // =====================================================
        // SECTION CONSTRUCTION
        // =====================================================
        private void BuildSections()
        {
            int margin = 5;
            int formWidth = ClientSize.Width;
            int usableW = formWidth - margin * 2;

            // ── Top toolbar bar (theme toggle lives here) ──────────────
            BuildTopBar(margin);

            // ── Section vertical positions ────────────────────────────
            int fileY = 60;
            int comprY = fileY + 170 + DesignSystem.SectionGap;
            int metaY = comprY + 210 + DesignSystem.SectionGap;

            // ── File Section ──────────────────────────────────────────
            _fileSection = new FileSection(margin, fileY, usableW);
            Controls.Add(_fileSection.Panel);

            // ── Compression Section ───────────────────────────────────
            _compressionSection = new CompressionSection(margin, comprY, usableW);
            Controls.Add(_compressionSection.Panel);

            // ── Metadata + Results ────────────────────────────────────
            int metaW = (int)(usableW * 0.58);
            int resultsW = usableW - metaW - DesignSystem.SectionGap;
            int metaH = ClientSize.Height - metaY - margin;

            _metadataSection = new MetadataSection(
                margin, metaY, metaW, resultsW, metaH);

            Controls.Add(_metadataSection.MetadataPanel);
            Controls.Add(_metadataSection.ResultsPanel);
        }

        private void BuildTopBar(int margin)
        {
            // App title
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

            // Theme toggle button (top-right)
            var btnTheme = DesignSystem.CreateButton("🌗  Theme", ButtonStyle.Ghost);
            btnTheme.Width = 110;
            btnTheme.SetBounds(ClientSize.Width - margin - 110, 12, 110, 32);
            btnTheme.Click += (s, e) => ToggleTheme();
            Controls.Add(btnTheme);

            // Divider line
            var div = DesignSystem.CreateSeparator(ClientSize.Width - margin * 2);
            div.Location = new Point(margin, 52);
            Controls.Add(div);
        }

        // =====================================================
        // EVENT WIRING
        // =====================================================
        private void WireEvents()
        {
            // File section events
            _fileSection.FileRequested += (s, path) => LoadFile(path);
            _fileSection.PlayRequested += (s, e) => _playerService.Play(_selectedFilePath);
            _fileSection.StopRequested += (s, e) => _playerService.Stop();

            // Compression section events
            _compressionSection.CompressRequested += OnCompressRequested;
            _compressionSection.DecompressRequested += OnDecompressRequested;
            _compressionSection.ResetRequested += OnResetRequested;

            // Metadata / Results section events
            _metadataSection.SaveRequested += OnSaveRequested;

            // BackgroundWorker — DoWork and RunWorkerCompleted stay in Form1
            // because they touch application state (_originalAudio, _processedAudio,
            // _compressionService, _lastSettings).
            _compressionSection.Worker.DoWork += Worker_DoWork;
            _compressionSection.Worker.RunWorkerCompleted += Worker_RunWorkerCompleted;
        }

        // =====================================================
        // LOAD FILE
        // =====================================================
        private void LoadFile(string path)
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

        // =====================================================
        // COMPRESS
        // =====================================================
        private void OnCompressRequested(object sender, CompressionRequestArgs e)
        {
            if (_originalAudio == null)
            {
                MessageBox.Show("Please load an audio file first.",
                                "No File Loaded",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
                return;
            }

            _lastSettings = e.Settings;
            _compressionService = _compressionSection.BuildService();

            _compressionSection.SetBusy();
            _compressionSection.Worker.RunWorkerAsync(_originalAudio);
        }

        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            byte[] inputData = (byte[])e.Argument;
            var worker = (BackgroundWorker)sender;

            // Simulate progress reporting (matches original behaviour)
            for (int i = 1; i <= 100; i++)
            {
                if (worker.CancellationPending)
                {
                    e.Cancel = true;
                    return;
                }

                System.Threading.Thread.Sleep(10);
                worker.ReportProgress(i);
            }

            e.Result = _compressionService.Compress(inputData, _lastSettings);
        }

        private void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                MessageBox.Show("Compression was cancelled.",
                                "Cancelled",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);

                _compressionSection.SetIdle(decompressEnabled: false);
            }
            else if (e.Error != null)
            {
                MessageBox.Show($"An error occurred during compression:\n{e.Error.Message}",
                                "Error",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);

                _compressionSection.SetIdle(decompressEnabled: false);
            }
            else
            {
                _processedAudio = (byte[])e.Result;

                _metadataSection.UpdateResults(
                    _originalAudio.Length,
                    _processedAudio.Length);

                _compressionSection.SetIdle(decompressEnabled: true);

                MessageBox.Show("Compression completed successfully.",
                                "Done",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
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
                                "Nothing to Decompress",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
                return;
            }

            if (_compressionService == null)
            {
                MessageBox.Show("Please select an algorithm and compress first.",
                                "No Service",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
                return;
            }

            _processedAudio = _compressionService.Decompress(_processedAudio, _lastSettings);

            _metadataSection.UpdateResults(
                _originalAudio?.Length ?? 0,
                _processedAudio.Length);

            MessageBox.Show("Decompression completed.",
                            "Done",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
        }

        // =====================================================
        // RESET
        // =====================================================
        private void OnResetRequested(object sender, EventArgs e)
        {
            _processedAudio = null;
            _compressionService = null;
            _lastSettings = null;

            _metadataSection.ClearResults();

            if (_originalAudio != null)
                _metadataSection.UpdateResults(_originalAudio.Length, 0);

            _compressionSection.SetIdle(decompressEnabled: false);

            MessageBox.Show("Reset completed. You can select a new algorithm.",
                            "Reset",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
        }

        // =====================================================
        // SAVE
        // =====================================================
        private void OnSaveRequested(object sender, EventArgs e)
        {
            if (_processedAudio == null)
            {
                MessageBox.Show("Nothing to save.",
                                "No Processed Audio",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
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
        // THEME TOGGLE
        // =====================================================
        private void ToggleTheme()
        {
            // Currently full dark; extend DesignSystem if you add a light palette.
            DesignSystem.ApplyDarkTheme(this);
        }

        // =====================================================
        // DESIGNER-REQUIRED HANDLER
        // Form1.Designer.cs registers:
        //   this.Load += new System.EventHandler(this.Form1_Load);
        // The method must exist here to satisfy that wiring.
        // =====================================================
        private void Form1_Load(object sender, EventArgs e) { }
    }
}

*/

using System;
using System.ComponentModel;
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
    ///   1. Instantiate services (AudioMetadataService, AudioPlayerService)
    ///   2. Instantiate the four UI sections
    ///   3. Add section panels to the form
    ///   4. Wire cross-section events
    ///   5. Hold application state (_originalAudio, _processedAudio, _compressionService)
    ///   6. Coordinate BackgroundWorker DoWork / RunWorkerCompleted
    ///
    /// No UI construction code lives here.
    /// </summary>
    public partial class Form1 : Form
    {
        // =====================================================
        // SERVICES
        // =====================================================
        private readonly AudioMetadataService _metadataService;
        private readonly AudioPlayerService _playerService;

        // =====================================================
        // THEME
        // =====================================================
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
        // UI SECTIONS
        // =====================================================
        private FileSection _fileSection;
        private CompressionSection _compressionSection;
        private MetadataSection _metadataSection;

        // =====================================================
        // CONSTRUCTOR
        // =====================================================
        public Form1()
        {
            _metadataService = new AudioMetadataService();
            _playerService = new AudioPlayerService();

            InitializeComponent();       // designer-generated (empty partial)
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
            Width = 1020;
            Height = 780;
            MinimumSize = new Size(1020, 780);
            StartPosition = FormStartPosition.CenterScreen;
            AllowDrop = true;
            DoubleBuffered = true;

            DesignSystem.ApplyDarkTheme(this);
        }

        // =====================================================
        // SECTION CONSTRUCTION
        // =====================================================
        private void BuildSections()
        {
            int margin = 20;
            int formWidth = ClientSize.Width;
            int usableW = formWidth - margin * 2;

            // ── Top toolbar bar (theme toggle lives here) ──────────────
            BuildTopBar(margin);

            // ── Section vertical positions ────────────────────────────
            int fileY = 60;
            int comprY = fileY + 170 + DesignSystem.SectionGap;
            int metaY = comprY + 210 + DesignSystem.SectionGap;

            // ── File Section ──────────────────────────────────────────
            _fileSection = new FileSection(margin, fileY, usableW);
            Controls.Add(_fileSection.Panel);

            // ── Compression Section ───────────────────────────────────
            _compressionSection = new CompressionSection(margin, comprY, usableW);
            Controls.Add(_compressionSection.Panel);

            // ── Metadata + Results ────────────────────────────────────
            int metaW = (int)(usableW * 0.58);
            int resultsW = usableW - metaW - DesignSystem.SectionGap;
            int metaH = ClientSize.Height - metaY - margin;

            _metadataSection = new MetadataSection(
                margin, metaY, metaW, resultsW, metaH);

            Controls.Add(_metadataSection.MetadataPanel);
            Controls.Add(_metadataSection.ResultsPanel);
        }

        private void BuildTopBar(int margin)
        {
            // App title
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

            // Theme toggle button (top-right)
            var btnTheme = DesignSystem.CreateButton("🌗  Theme", ButtonStyle.Ghost);
            btnTheme.Width = 110;
            btnTheme.SetBounds(ClientSize.Width - margin - 110, 12, 110, 32);
            btnTheme.Click += (s, e) => ToggleTheme();
            Controls.Add(btnTheme);

            // Divider line
            var div = DesignSystem.CreateSeparator(ClientSize.Width - margin * 2);
            div.Location = new Point(margin, 52);
            Controls.Add(div);
        }

        // =====================================================
        // EVENT WIRING
        // =====================================================
        private void WireEvents()
        {
            // File section events
            _fileSection.FileRequested += (s, path) => LoadFile(path);
            _fileSection.PlayRequested += (s, e) => _playerService.Play(_selectedFilePath);
            _fileSection.StopRequested += (s, e) => _playerService.Stop();

            // Compression section events
            _compressionSection.CompressRequested += OnCompressRequested;
            _compressionSection.DecompressRequested += OnDecompressRequested;
            _compressionSection.ResetRequested += OnResetRequested;

            // Metadata / Results section events
            _metadataSection.SaveRequested += OnSaveRequested;

            // BackgroundWorker — DoWork and RunWorkerCompleted stay in Form1
            // because they touch application state (_originalAudio, _processedAudio,
            // _compressionService, _lastSettings).
            _compressionSection.Worker.DoWork += Worker_DoWork;
            _compressionSection.Worker.RunWorkerCompleted += Worker_RunWorkerCompleted;
        }

        // =====================================================
        // LOAD FILE
        // =====================================================
        private void LoadFile(string path)
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

        // =====================================================
        // COMPRESS
        // =====================================================
        private void OnCompressRequested(object sender, CompressionRequestArgs e)
        {
            if (_originalAudio == null)
            {
                MessageBox.Show("Please load an audio file first.",
                                "No File Loaded",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
                return;
            }

            _lastSettings = e.Settings;
            _compressionService = _compressionSection.BuildService();

            _compressionSection.SetBusy();
            _compressionSection.Worker.RunWorkerAsync(_originalAudio);
        }

        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            byte[] inputData = (byte[])e.Argument;
            var worker = (BackgroundWorker)sender;

            // Simulate progress reporting (matches original behaviour)
            for (int i = 1; i <= 100; i++)
            {
                if (worker.CancellationPending)
                {
                    e.Cancel = true;
                    return;
                }

                System.Threading.Thread.Sleep(10);
                worker.ReportProgress(i);
            }

            e.Result = _compressionService.Compress(inputData, _lastSettings);
        }

        private void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                MessageBox.Show("Compression was cancelled.",
                                "Cancelled",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);

                _compressionSection.SetIdle(decompressEnabled: false);
            }
            else if (e.Error != null)
            {
                MessageBox.Show($"An error occurred during compression:\n{e.Error.Message}",
                                "Error",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);

                _compressionSection.SetIdle(decompressEnabled: false);
            }
            else
            {
                _processedAudio = (byte[])e.Result;

                _metadataSection.UpdateResults(
                    _originalAudio.Length,
                    _processedAudio.Length);

                _compressionSection.SetIdle(decompressEnabled: true);

                MessageBox.Show("Compression completed successfully.",
                                "Done",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
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
                                "Nothing to Decompress",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
                return;
            }

            if (_compressionService == null)
            {
                MessageBox.Show("Please select an algorithm and compress first.",
                                "No Service",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
                return;
            }

            _processedAudio = _compressionService.Decompress(_processedAudio, _lastSettings);

            _metadataSection.UpdateResults(
                _originalAudio?.Length ?? 0,
                _processedAudio.Length);

            MessageBox.Show("Decompression completed.",
                            "Done",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
        }

        // =====================================================
        // RESET
        // =====================================================
        private void OnResetRequested(object sender, EventArgs e)
        {
            _processedAudio = null;
            _compressionService = null;
            _lastSettings = null;

            _metadataSection.ClearResults();

            if (_originalAudio != null)
                _metadataSection.UpdateResults(_originalAudio.Length, 0);

            _compressionSection.SetIdle(decompressEnabled: false);

            MessageBox.Show("Reset completed. You can select a new algorithm.",
                            "Reset",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
        }

        // =====================================================
        // SAVE
        // =====================================================
        private void OnSaveRequested(object sender, EventArgs e)
        {
            if (_processedAudio == null)
            {
                MessageBox.Show("Nothing to save.",
                                "No Processed Audio",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
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
        // THEME TOGGLE
        // =====================================================
        private void ToggleTheme()
        {
            _themeManager.ToggleTheme(this);
        }

        // =====================================================
        // DESIGNER-REQUIRED HANDLER
        // Form1.Designer.cs registers:
        //   this.Load += new System.EventHandler(this.Form1_Load);
        // The method must exist here to satisfy that wiring.
        // =====================================================
        private void Form1_Load(object sender, EventArgs e) { }
    }
}