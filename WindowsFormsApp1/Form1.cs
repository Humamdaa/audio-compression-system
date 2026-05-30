using System;
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

        private ComboBox cmbAlgorithm;
        private NumericUpDown numQuantization;

        private Label lblOriginalSize, lblProcessedSize, lblCompressionRatio;

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
        }

        // =====================================================
        // THEME SYSTEM (DARK / LIGHT MODE)
        // =====================================================
        private void ApplyTheme()
        {
            BackColor = isDark ? darkBg : lightBg;
            ForeColor = isDark ? Color.White : Color.Black;
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

            btnSelectFile = CreateButton("Load", 20, 20);
            btnPlay = CreateButton("Play", 140, 20);
            btnStop = CreateButton("Stop", 260, 20);
            btnCompress = CreateButton("Compress", 380, 20);

            btnReset = CreateButton("Reset", 520, 20);
            btnSave = CreateButton("Save", 660, 20);

            // THEME SWITCH BUTTON
            var btnTheme = CreateButton("🌗 Theme", 800, 20);
            btnTheme.Click += (s, e) => ToggleTheme();

            Controls.AddRange(new Control[]
            {
                btnSelectFile, btnPlay, btnStop,
                btnCompress, btnReset, btnSave, btnTheme
            });

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

            btnCompress.Click += Compress_Click;
            btnReset.Click += Reset_Click;
            btnSave.Click += Save_Click;

            cmbAlgorithm.SelectedIndexChanged += (s, e) => UpdateSettings();
            numQuantization.ValueChanged += (s, e) => UpdateSettings();
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

            _processedAudio = _compressionService.Compress(_originalAudio, _settings);

            btnCompress.Enabled = true;

            UpdateMetrics();
        }

        // =====================================================
        // RESET (REPLACES DECOMPRESS BUTTON)
        // =====================================================
        private void Reset_Click(object sender, EventArgs e)
        {
            _processedAudio = null;
            _compressionService = null;

            UpdateMetrics();

            MessageBox.Show("Reset completed. You can select new algorithm now.");
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
                ForeColor = Color.White
            };

            Controls.Add(lbl);
            return lbl;
        }
    }
}