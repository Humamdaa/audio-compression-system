using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace WindowsFormsApp1.UI
{
    /// <summary>
    /// Owns the File Management card:
    ///   • Load button
    ///   • Drag-and-drop zone
    ///   • Play / Stop buttons
    ///   • Current file name display
    ///
    /// Exposes:
    ///   • Event FileRequested  → Form1 calls LoadFile
    ///   • Event PlayRequested  → Form1 calls playerService.Play
    ///   • Event StopRequested  → Form1 calls playerService.Stop
    ///   • Property Panel       → the card panel to add to the form
    /// </summary>
    public sealed class FileSection
    {
        // =====================================================
        // PUBLIC SURFACE
        // =====================================================
        public Panel Panel { get; }

        public event EventHandler<string> FileRequested;   // arg = file path
        public event EventHandler PlayRequested;
        public event EventHandler StopRequested;

        // =====================================================
        // PRIVATE CONTROLS
        // =====================================================
        private readonly Button _btnLoad;
        private readonly Button _btnPlay;
        private readonly Button _btnStop;
        private readonly Panel _dropZone;
        private readonly Label _lblDropHint;
        private readonly Label _lblCurrentFile;

        // =====================================================
        // CONSTRUCTOR
        // =====================================================
        public FileSection(int x, int y, int width)
        {
            Panel = DesignSystem.CreateCard(x, y, width, 160);

            // --- Section header ---
            var header = DesignSystem.CreateSectionHeader("📂  File Management");
            header.SetBounds(DesignSystem.SectionPad, DesignSystem.SectionPad, width - DesignSystem.SectionPad * 2, 20);
            Panel.Controls.Add(header);

            // --- Separator ---
            var sep = DesignSystem.CreateSeparator(width - DesignSystem.SectionPad * 2);
            sep.Location = new Point(DesignSystem.SectionPad, 36);
            Panel.Controls.Add(sep);

            // --- Buttons row ---
            int btnY = 48;
            int btnX = DesignSystem.SectionPad;

            _btnLoad = DesignSystem.CreateButton("📥  Load File");
            _btnLoad.SetBounds(btnX, btnY, DesignSystem.ButtonWidth, DesignSystem.ButtonHeight);
            Panel.Controls.Add(_btnLoad);

            btnX += DesignSystem.ButtonWidth + DesignSystem.ControlGap;
            _btnPlay = DesignSystem.CreateButton("▶  Play", ButtonStyle.Success);
            _btnPlay.SetBounds(btnX, btnY, DesignSystem.ButtonWidth, DesignSystem.ButtonHeight);
            Panel.Controls.Add(_btnPlay);

            btnX += DesignSystem.ButtonWidth + DesignSystem.ControlGap;
            _btnStop = DesignSystem.CreateButton("■  Stop", ButtonStyle.Ghost);
            _btnStop.SetBounds(btnX, btnY, DesignSystem.ButtonWidth, DesignSystem.ButtonHeight);
            Panel.Controls.Add(_btnStop);

            // --- Current file label ---
            _lblCurrentFile = DesignSystem.CreateLabel("No file loaded", LabelStyle.Caption);
            _lblCurrentFile.SetBounds(DesignSystem.SectionPad, btnY + DesignSystem.ButtonHeight + 8,
                                      width - DesignSystem.SectionPad * 2, DesignSystem.LabelHeight);
            _lblCurrentFile.AutoSize = true;
            _lblCurrentFile.ForeColor = DesignSystem.TextMuted;
            Panel.Controls.Add(_lblCurrentFile);

            // --- Drag & Drop zone ---
            int dzX = btnX + DesignSystem.ButtonWidth + DesignSystem.ControlGap;
            _dropZone = BuildDropZone(dzX, btnY,
                                      width - dzX - DesignSystem.SectionPad,
                                      DesignSystem.ButtonHeight * 2 + 8);
            _lblDropHint = (Label)_dropZone.Controls[0];
            Panel.Controls.Add(_dropZone);

            // --- Wire events ---
            _btnLoad.Click += (s, e) => OnLoadClick();
            _btnPlay.Click += (s, e) => PlayRequested?.Invoke(this, EventArgs.Empty);
            _btnStop.Click += (s, e) => StopRequested?.Invoke(this, EventArgs.Empty);
        }

        // =====================================================
        // PUBLIC METHODS
        // =====================================================
        /// <summary>Update the "current file" label after a file loads.</summary>
        public void SetCurrentFile(string filePath)
        {
            _lblCurrentFile.Text = string.IsNullOrEmpty(filePath)
                ? "No file loaded"
                : $"Loaded: {Path.GetFileName(filePath)}";
            _lblCurrentFile.ForeColor = string.IsNullOrEmpty(filePath)
                ? DesignSystem.TextMuted
                : DesignSystem.Success;
        }

        // =====================================================
        // PRIVATE — LOAD
        // =====================================================
        private void OnLoadClick()
        {
            using (var dlg = new OpenFileDialog { Filter = "Audio|*.wav;*.mp3;*.aac" })
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                    FileRequested?.Invoke(this, dlg.FileName);
            }
        }

        // =====================================================
        // PRIVATE — DROP ZONE BUILDER
        // =====================================================
        private Panel BuildDropZone(int x, int y, int w, int h)
        {
            
            var zone = new RoundedPanel(6)
            {
                Location = new Point(x, y),
                Size = new Size(w, h),
                BackColor = DesignSystem.BgElevated,
                AllowDrop = true,
            };

            var hint = new Label
            {
                Text = "⬇  Drop audio here",
                AutoSize = false,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = DesignSystem.TextMuted,
                Font = new Font("Segoe UI", 8.5f),
                BackColor = Color.Transparent
            };

            zone.Controls.Add(hint);

            zone.DragEnter += (s, e) =>
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    e.Effect = DragDropEffects.Copy;
                    zone.BackColor = DesignSystem.AccentDim;
                    hint.ForeColor = DesignSystem.TextPrimary;
                }
            };

            zone.DragLeave += (s, e) =>
            {
                zone.BackColor = DesignSystem.BgElevated;
                hint.ForeColor = DesignSystem.TextMuted;
            };

            zone.DragDrop += (s, e) =>
            {
                zone.BackColor = DesignSystem.BgElevated;
                hint.ForeColor = DesignSystem.TextMuted;

                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files == null || files.Length == 0) return;

                string file = files[0];
                string ext = Path.GetExtension(file).ToLower();

                if (ext == ".wav" || ext == ".mp3" || ext == ".aac")
                    FileRequested?.Invoke(this, file);
                else
                    MessageBox.Show("Unsupported file type. Please use WAV, MP3, or AAC.",
                                    "Invalid File", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            };

            return zone;
        }
    }
}