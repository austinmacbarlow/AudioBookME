using System.Text.RegularExpressions;

namespace AudiobookMaker;

public class MainForm : Form
{
    TextBox      txtInput = null!, txtOutput = null!, txtTitle = null!,
                 txtAuthor = null!, txtSeries = null!, txtCoverArt = null!;
    Button       btnBrowseIn = null!, btnAddFolder = null!, btnBrowseOut = null!, btnBrowseCover = null!;
    ComboBox     cmbBitrate  = null!;
    RadioButton  rdoName = null!, rdoDate = null!;
    DataGridView dgvFiles    = null!;
    Button       btnUp = null!, btnDown = null!, btnRemove = null!, btnRefresh = null!;
    ProgressBar  prgConvert  = null!;
    Label        lblProgress = null!;
    Button       btnConvert  = null!, btnCancel  = null!, btnFixM4B = null!, btnSplitM4B = null!;
    RichTextBox  txtLog      = null!;

    readonly List<string> fileList = [];
    string primaryFolder  = "";
    bool   outputAutoSet  = false;
    DateTime convStart;
    CancellationTokenSource? cts;
    readonly AppSettings settings = AppSettings.Load();

    public MainForm()
    {
        InitializeComponent();
        LoadSettings();
    }

    void LoadSettings()
    {
        if (cmbBitrate.Items.Contains(settings.LastBitrate))
            cmbBitrate.SelectedItem = settings.LastBitrate;
    }

    void InitializeComponent()
    {
        SuspendLayout();

        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode       = AutoScaleMode.Dpi;

        Text        = "Audiobook Maker";
        AutoScroll  = true;
        ClientSize  = new Size(720, 600);
        MinimumSize = new Size(720, 400);
        StartPosition   = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.Sizable;
        BackColor       = Palette.Base;
        Font            = new Font("Segoe UI", 9.5f);
        AllowDrop       = true;
        Icon            = CreateAppIcon();

        // ── Header ───────────────────────────────────────────────────────────
        var header = new Panel { Location = new Point(0, 0), Size = new Size(720, 54), BackColor = Palette.Pine };
        header.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        header.Controls.Add(new Label
        {
            Text = "Audiobook Maker", Location = new Point(16, 10), Size = new Size(400, 28),
            ForeColor = Color.White, BackColor = Color.Transparent,
            Font = new Font("Segoe UI", 16f, FontStyle.Bold)
        });
        header.Controls.Add(new Label
        {
            Text = "Convert audio files to M4B", Location = new Point(20, 36), Size = new Size(400, 14),
            ForeColor = Palette.PaleMint, BackColor = Color.Transparent,
            Font = new Font("Segoe UI", 8f)
        });
        Controls.Add(header);

        // ── Input / Output ────────────────────────────────────────────────────
        int y = 62;
        RowLabel("Input Folder:", y);
        txtInput     = Input(120, y, 322); txtInput.ReadOnly = true;
        btnBrowseIn  = UtilBtn("Browse", 450, y - 1, 76);
        btnAddFolder = UtilBtn("+ Add",  532, y - 1, 66);
        y += 26;

        RowLabel("Output File:", y);
        txtOutput    = Input(120, y, 522);
        btnBrowseOut = UtilBtn("Browse", 648, y - 1, 64);
        y += 26;

        // ── Metadata ──────────────────────────────────────────────────────────
        y += 6; Divider(y); y += 4; SecLabel("Metadata", y); y += 14;

        RowLabel("Book Title:", y);  txtTitle  = Input(120, y, 572); y += 26;
        RowLabel("Author:",     y);  txtAuthor = Input(120, y, 572); y += 26;
        RowLabel("Series:",     y);  txtSeries = Input(120, y, 572); y += 26;

        RowLabel("Cover Art:", y);
        txtCoverArt    = Input(120, y, 458); txtCoverArt.ReadOnly = true;
        btnBrowseCover = UtilBtn("Browse", 586, y - 1, 106);
        y += 26;

        // ── Options ───────────────────────────────────────────────────────────
        y += 6; Divider(y); y += 4; SecLabel("Options", y); y += 14;

        RowLabel("Bitrate:", y);
        cmbBitrate = new ComboBox
        {
            Location = new Point(120, y - 1), Size = new Size(80, 26),
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = Palette.Input, ForeColor = Palette.Text,
            FlatStyle = FlatStyle.Flat, Font = Font
        };
        foreach (string b in (string[])["32k", "48k", "64k", "96k", "128k", "Copy"])
            cmbBitrate.Items.Add(b);
        cmbBitrate.SelectedItem = "64k";
        Controls.Add(cmbBitrate);

        Controls.Add(new Label
        {
            Text = "\"Copy\" = stream copy, instant (AAC sources only)",
            Location = new Point(208, y + 3), Size = new Size(460, 18),
            ForeColor = Palette.Subtext, Font = new Font("Segoe UI", 8.5f)
        });
        y += 26;

        RowLabel("Sort:", y);
        rdoName = new RadioButton { Text = "Filename", Location = new Point(120, y), Size = new Size(90, 22), Checked = true, ForeColor = Palette.Text, BackColor = Palette.Base };
        rdoDate = new RadioButton { Text = "Date",     Location = new Point(214, y), Size = new Size(70, 22), ForeColor = Palette.Text, BackColor = Palette.Base };
        Controls.Add(rdoName); Controls.Add(rdoDate);
        y += 26;

        // ── File list ─────────────────────────────────────────────────────────
        y += 6; Divider(y); y += 4; SecLabel("Files  (double-click Chapter Name to edit)", y); y += 14;

        dgvFiles = new DataGridView
        {
            Location  = new Point(12, y), Size = new Size(544, 80),
            AllowUserToAddRows = false, AllowUserToDeleteRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false, RowHeadersVisible = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            BackgroundColor = Palette.Input, GridColor = Palette.Border,
            BorderStyle = BorderStyle.FixedSingle, Font = Font,
            AllowDrop = true, EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2
        };
        var colFile = new DataGridViewTextBoxColumn
            { Name = "File", HeaderText = "File", FillWeight = 45, ReadOnly = true };
        colFile.DefaultCellStyle.ForeColor = Palette.Subtext;
        var colChapter = new DataGridViewTextBoxColumn
            { Name = "Chapter", HeaderText = "Chapter Name  ✎", FillWeight = 55 };
        dgvFiles.Columns.Add(colFile);
        dgvFiles.Columns.Add(colChapter);
        StyleGrid(dgvFiles);
        Controls.Add(dgvFiles);

        int bx = 564, by = y;
        btnUp      = SideBtn("▲  Up",      bx, by); by += 32;
        btnDown    = SideBtn("▼  Down",    bx, by); by += 32;
        btnRemove  = SideBtn("✕  Remove",  bx, by); by += 32;
        btnRefresh = SideBtn("↺  Refresh", bx, by);
        y += 90;

        // ── Progress ──────────────────────────────────────────────────────────
        y += 8;
        lblProgress = new Label
        {
            Text = "", Location = new Point(12, y), Size = new Size(696, 18),
            ForeColor = Palette.Subtext, Font = new Font("Segoe UI", 8.5f), Visible = false
        };
        Controls.Add(lblProgress);
        y += 20;

        prgConvert = new ProgressBar
        {
            Location = new Point(12, y), Size = new Size(696, 14),
            Style = ProgressBarStyle.Continuous,
            ForeColor = Palette.Pine, Visible = false
        };
        Controls.Add(prgConvert);
        y += 24;

        // ── Convert / Cancel ──────────────────────────────────────────────────
        y += 8;
        btnConvert = new Button
        {
            Text = "Convert to M4B", Location = new Point(100, y), Size = new Size(228, 42),
            Font = new Font("Segoe UI", 11f, FontStyle.Bold),
            BackColor = Palette.Pine, ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand
        };
        btnConvert.FlatAppearance.BorderSize = 0;
        AddHover(btnConvert, Palette.Pine, Palette.PineDark);
        Controls.Add(btnConvert);

        btnCancel = new Button
        {
            Text = "Cancel", Location = new Point(338, y), Size = new Size(228, 42),
            Font = new Font("Segoe UI", 11f, FontStyle.Bold),
            BackColor = Palette.Red, ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Visible = false
        };
        btnCancel.FlatAppearance.BorderSize = 0;
        AddHover(btnCancel, Palette.Red, Palette.RedHover);
        Controls.Add(btnCancel);
        y += 52;

        // ── Fix / Split ───────────────────────────────────────────────────────
        btnFixM4B   = OutlineBtn("Fix M4B Chapters…",        100, y, 224);
        btnSplitM4B = OutlineBtn("Split M4B into Chapters…", 334, y, 254);
        y += 38;

        // ── Log ───────────────────────────────────────────────────────────────
        y += 6; Divider(y); y += 4; SecLabel("Log", y); y += 12;

        txtLog = new RichTextBox
        {
            Location = new Point(12, y), Size = new Size(696, 60),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            ReadOnly = true,
            BackColor = Color.FromArgb(22, 40, 28), ForeColor = Color.FromArgb(212, 237, 218),
            Font = new Font("Consolas", 9f), ScrollBars = RichTextBoxScrollBars.Vertical,
            BorderStyle = BorderStyle.None
        };
        Controls.Add(txtLog);

        // ── Wire events ───────────────────────────────────────────────────────
        btnBrowseIn.Click    += BtnBrowseIn_Click;
        btnAddFolder.Click   += BtnAddFolder_Click;
        btnBrowseOut.Click   += BtnBrowseOut_Click;
        btnBrowseCover.Click += BtnBrowseCover_Click;
        btnUp.Click          += BtnUp_Click;
        btnDown.Click        += BtnDown_Click;
        btnRemove.Click      += BtnRemove_Click;
        btnRefresh.Click     += (_, _) => RefreshList();
        rdoName.CheckedChanged += (_, _) => { if (rdoName.Checked) RefreshList(); };
        rdoDate.CheckedChanged += (_, _) => { if (rdoDate.Checked) RefreshList(); };
        btnConvert.Click     += BtnConvert_Click;
        btnCancel.Click      += (_, _) => cts?.Cancel();
        btnFixM4B.Click      += (_, _) => new FixChaptersForm().ShowDialog(this);
        btnSplitM4B.Click    += (_, _) => new SplitM4BForm().ShowDialog(this);
        txtTitle.TextChanged  += UpdateOutputName;
        txtAuthor.TextChanged += UpdateOutputName;
        cmbBitrate.SelectedIndexChanged += (_, _) => { settings.LastBitrate = cmbBitrate.SelectedItem?.ToString() ?? "64k"; settings.Save(); };
        FormClosed            += (_, _) => settings.Save();

        // Drag-and-drop on form and grid
        DragEnter           += HandleDragEnter;
        DragDrop            += HandleDragDrop;
        dgvFiles.DragEnter  += HandleDragEnter;
        dgvFiles.DragDrop   += HandleDragDrop;

        ResumeLayout();
    }

    // ── UI factory helpers ────────────────────────────────────────────────────

    void RowLabel(string text, int y, int x = 12) =>
        Controls.Add(new Label { Text = text, Location = new Point(x, y + 5), Size = new Size(106, 18),
            ForeColor = Palette.Subtext, Font = new Font("Segoe UI", 9f) });

    void SecLabel(string text, int y, int x = 14) =>
        Controls.Add(new Label { Text = text.ToUpper(), Location = new Point(x, y), Size = new Size(450, 14),
            ForeColor = Palette.Pine, Font = new Font("Segoe UI", 7.5f, FontStyle.Bold) });

    void Divider(int y) =>
        Controls.Add(new Panel { Location = new Point(12, y), Size = new Size(696, 1), BackColor = Palette.Border });

    TextBox Input(int x, int y, int w)
    {
        var t = new TextBox { Location = new Point(x, y - 1), Width = w,
            BackColor = Palette.Input, ForeColor = Palette.Text,
            BorderStyle = BorderStyle.FixedSingle, Font = Font };
        Controls.Add(t); return t;
    }

    Button UtilBtn(string text, int x, int y, int w = 80)
    {
        var b = new Button { Text = text, Location = new Point(x, y), Size = new Size(w, 26),
            BackColor = Palette.Surface, ForeColor = Palette.Pine,
            FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
        b.FlatAppearance.BorderColor = Palette.Border; b.FlatAppearance.BorderSize = 1;
        AddHover(b, Palette.Surface, Palette.PaleMint);
        Controls.Add(b); return b;
    }

    Button SideBtn(string text, int x, int y)
    {
        var b = new Button { Text = text, Location = new Point(x, y), Size = new Size(144, 26),
            BackColor = Palette.Surface, ForeColor = Palette.Text,
            FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand,
            Font = new Font("Segoe UI", 9f), TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(8, 0, 0, 0) };
        b.FlatAppearance.BorderColor = Palette.Border; b.FlatAppearance.BorderSize = 1;
        AddHover(b, Palette.Surface, Palette.PaleMint);
        Controls.Add(b); return b;
    }

    Button OutlineBtn(string text, int x, int y, int w)
    {
        var b = new Button { Text = text, Location = new Point(x, y), Size = new Size(w, 28),
            BackColor = Palette.Surface, ForeColor = Palette.Pine,
            FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
        b.FlatAppearance.BorderColor = Palette.Border; b.FlatAppearance.BorderSize = 1;
        AddHover(b, Palette.Surface, Palette.PaleMint);
        Controls.Add(b); return b;
    }

    static void AddHover(Button b, Color normal, Color hover)
    {
        b.MouseEnter += (_, _) => b.BackColor = hover;
        b.MouseLeave += (_, _) => b.BackColor = normal;
    }

    static void StyleGrid(DataGridView g)
    {
        g.ColumnHeadersDefaultCellStyle.BackColor = Palette.Surface;
        g.ColumnHeadersDefaultCellStyle.ForeColor = Palette.Text;
        g.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
        g.DefaultCellStyle.BackColor = Palette.Input;
        g.DefaultCellStyle.ForeColor = Palette.Text;
        g.DefaultCellStyle.SelectionBackColor = Palette.PaleMint;
        g.DefaultCellStyle.SelectionForeColor = Palette.Text;
        g.AlternatingRowsDefaultCellStyle.BackColor = Palette.Surface;
        g.EnableHeadersVisualStyles = false;
    }

    static Icon CreateAppIcon()
    {
        var bmp = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);
            g.FillEllipse(new SolidBrush(Palette.Pine), 0, 0, 31, 31);
            using var sf   = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            using var font = new Font("Segoe UI", 17f, FontStyle.Bold);
            g.DrawString("A", font, Brushes.White, new RectangleF(0, 0, 32, 32), sf);
        }
        return Icon.FromHandle(bmp.GetHicon());
    }

    void Log(string msg)
    {
        if (InvokeRequired) { Invoke(() => Log(msg)); return; }
        txtLog.AppendText(msg + "\r\n");
        txtLog.ScrollToCaret();
    }

    // ── File list helpers ─────────────────────────────────────────────────────

    void RefreshList()
    {
        if (string.IsNullOrEmpty(primaryFolder) || !Directory.Exists(primaryFolder)) return;
        var files = SortFiles(FfmpegHelper.GetAudioFiles(primaryFolder));
        fileList.Clear();
        dgvFiles.Rows.Clear();
        foreach (string f in files) AddRow(f);
        Log($"Found {fileList.Count} audio file(s).");
    }

    void AddRow(string path)
    {
        if (fileList.Contains(path)) return;
        fileList.Add(path);
        string chName = Regex.Replace(Path.GetFileNameWithoutExtension(path), @"^\d+[\s\.\-_]+", "").Trim();
        dgvFiles.Rows.Add(Path.GetFileName(path), chName);
    }

    void AddFolderFiles(string folder)
    {
        int added = 0;
        foreach (string f in SortFiles(FfmpegHelper.GetAudioFiles(folder)))
        {
            if (!fileList.Contains(f)) { AddRow(f); added++; }
        }
        Log($"Added {added} file(s) from: {folder}");
    }

    IEnumerable<string> SortFiles(string[] files) =>
        rdoDate.Checked ? files.OrderBy(File.GetLastWriteTime) : files.OrderBy(Path.GetFileName);

    void SwapItems(int a, int b)
    {
        (fileList[a], fileList[b]) = (fileList[b], fileList[a]);
        for (int c = 0; c < dgvFiles.Columns.Count; c++)
        {
            object? tmp = dgvFiles.Rows[a].Cells[c].Value;
            dgvFiles.Rows[a].Cells[c].Value = dgvFiles.Rows[b].Cells[c].Value;
            dgvFiles.Rows[b].Cells[c].Value = tmp;
        }
    }

    int SelectedRow() => dgvFiles.CurrentRow?.Index ?? -1;

    void UpdateOutputName(object? s, EventArgs e)
    {
        if (!outputAutoSet || string.IsNullOrEmpty(txtOutput.Text)) return;
        string dir    = Path.GetDirectoryName(txtOutput.Text) ?? "";
        string title  = txtTitle.Text.Trim();
        string author = txtAuthor.Text.Trim();
        if (string.IsNullOrEmpty(dir)) return;
        string name = author != "" && title != "" ? $"{author} - {title}.m4b"
                    : title != "" ? $"{title}.m4b"
                    : Path.GetFileName(txtOutput.Text);
        txtOutput.Text = Path.Combine(dir, name);
    }

    // ── Drag and drop ─────────────────────────────────────────────────────────

    static void HandleDragEnter(object? s, DragEventArgs e)
    {
        if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
            e.Effect = DragDropEffects.Copy;
    }

    void HandleDragDrop(object? s, DragEventArgs e)
    {
        var paths = (string[]?)e.Data?.GetData(DataFormats.FileDrop);
        if (paths == null) return;
        foreach (string path in paths)
        {
            if (Directory.Exists(path))
            {
                if (string.IsNullOrEmpty(primaryFolder))
                {
                    primaryFolder     = path;
                    txtInput.Text     = path;
                    string folderName = Path.GetFileName(path);
                    if (string.IsNullOrEmpty(txtOutput.Text))
                    {
                        txtOutput.Text = Path.Combine(Path.GetDirectoryName(path)!, folderName + ".m4b");
                        outputAutoSet  = true;
                    }
                    if (string.IsNullOrEmpty(txtTitle.Text)) txtTitle.Text = folderName;
                    RefreshList();
                }
                else
                {
                    AddFolderFiles(path);
                }
            }
            else if (IsAudioFile(path))
            {
                AddRow(path);
            }
        }
    }

    static bool IsAudioFile(string path)
    {
        string ext = Path.GetExtension(path).ToLowerInvariant();
        return ext is ".mp3" or ".m4a" or ".m4b" or ".flac" or ".ogg" or ".wma" or ".aac" or ".opus";
    }

    // ── Button events ─────────────────────────────────────────────────────────

    async void BtnBrowseIn_Click(object? sender, EventArgs e)
    {
        using var dlg = new FolderBrowserDialog { Description = "Select folder containing audio files" };
        if (dlg.ShowDialog() != DialogResult.OK) return;

        primaryFolder = dlg.SelectedPath;
        txtInput.Text = dlg.SelectedPath;
        string folderName = Path.GetFileName(dlg.SelectedPath);

        if (string.IsNullOrEmpty(txtOutput.Text))
        {
            txtOutput.Text = Path.Combine(Path.GetDirectoryName(dlg.SelectedPath)!, folderName + ".m4b");
            outputAutoSet  = true;
        }
        if (string.IsNullOrEmpty(txtTitle.Text)) txtTitle.Text = folderName;

        settings.LastInputFolder = dlg.SelectedPath;
        settings.Save();

        RefreshList();

        if (fileList.Count > 0)
        {
            string? ffprobe = FfmpegHelper.FindFfprobe();
            if (ffprobe != null)
            {
                var (title, artist) = await FfmpegHelper.ReadTagsAsync(ffprobe, fileList[0]);
                if (string.IsNullOrEmpty(txtTitle.Text)  && !string.IsNullOrEmpty(title))  txtTitle.Text  = title;
                if (string.IsNullOrEmpty(txtAuthor.Text) && !string.IsNullOrEmpty(artist)) txtAuthor.Text = artist;
            }
        }
    }

    void BtnAddFolder_Click(object? sender, EventArgs e)
    {
        using var dlg = new FolderBrowserDialog { Description = "Select folder to append audio files from" };
        if (dlg.ShowDialog() == DialogResult.OK) AddFolderFiles(dlg.SelectedPath);
    }

    void BtnBrowseOut_Click(object? sender, EventArgs e)
    {
        using var dlg = new SaveFileDialog
            { Filter = "M4B Audiobook (*.m4b)|*.m4b|All files (*.*)|*.*", DefaultExt = "m4b", Title = "Save M4B audiobook as..." };
        if (dlg.ShowDialog() == DialogResult.OK) { txtOutput.Text = dlg.FileName; outputAutoSet = false; }
    }

    void BtnBrowseCover_Click(object? sender, EventArgs e)
    {
        using var dlg = new OpenFileDialog
            { Title = "Select cover art image", Filter = "Image files (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png|All files (*.*)|*.*" };
        if (dlg.ShowDialog() == DialogResult.OK) txtCoverArt.Text = dlg.FileName;
    }

    void BtnUp_Click(object? sender, EventArgs e)
    {
        int i = SelectedRow(); if (i <= 0) return;
        SwapItems(i - 1, i);
        dgvFiles.ClearSelection(); dgvFiles.Rows[i - 1].Selected = true;
        dgvFiles.CurrentCell = dgvFiles.Rows[i - 1].Cells[0];
    }

    void BtnDown_Click(object? sender, EventArgs e)
    {
        int i = SelectedRow(); if (i < 0 || i >= fileList.Count - 1) return;
        SwapItems(i, i + 1);
        dgvFiles.ClearSelection(); dgvFiles.Rows[i + 1].Selected = true;
        dgvFiles.CurrentCell = dgvFiles.Rows[i + 1].Cells[0];
    }

    void BtnRemove_Click(object? sender, EventArgs e)
    {
        int i = SelectedRow(); if (i < 0) return;
        fileList.RemoveAt(i);
        dgvFiles.Rows.RemoveAt(i);
    }

    async void BtnConvert_Click(object? sender, EventArgs e)
    {
        if (fileList.Count == 0)
        { MessageBox.Show("No audio files loaded. Browse to a folder first.", "Nothing to convert", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

        string output = txtOutput.Text.Trim();
        if (string.IsNullOrEmpty(output))
        { MessageBox.Show("Set an output file path.", "Missing output", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

        string? ffmpeg = FfmpegHelper.FindFfmpeg();
        if (ffmpeg == null)
        {
            var ans = MessageBox.Show(
                "ffmpeg was not found.\n\nDownload it automatically? (~60 MB, one-time download saved to AppData)",
                "ffmpeg required", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (ans != DialogResult.Yes) return;
            txtLog.Clear();
            btnConvert.Enabled = false;
            ffmpeg = await FfmpegHelper.DownloadFfmpegAsync(new Progress<string>(Log), CancellationToken.None);
            btnConvert.Enabled = true;
            if (ffmpeg == null) return;
        }

        // Snapshot chapter names from grid before async work begins
        var chapterNames = Enumerable.Range(0, fileList.Count)
            .Select(i => dgvFiles.Rows[i].Cells["Chapter"].Value?.ToString() ?? "")
            .ToArray();

        await RunConversionAsync(ffmpeg, [.. fileList], chapterNames, output,
            cmbBitrate.SelectedItem?.ToString() ?? "64k",
            txtTitle.Text.Trim(), txtAuthor.Text.Trim(),
            txtSeries.Text.Trim(), txtCoverArt.Text.Trim());
    }

    // ── Conversion ────────────────────────────────────────────────────────────

    async Task RunConversionAsync(string ffmpeg, string[] files, string[] chapterNames,
        string outputPath, string bitrate, string bookTitle, string author,
        string series, string coverArtPath)
    {
        btnConvert.Enabled = false;
        btnCancel.Visible  = true;
        prgConvert.Value   = 0;
        prgConvert.Visible = true;
        lblProgress.Text   = "Analyzing files...";
        lblProgress.Visible = true;
        txtLog.Clear();

        cts = new CancellationTokenSource();
        var ct = cts.Token;

        bool copyMode = bitrate.Equals("Copy", StringComparison.OrdinalIgnoreCase);

        try
        {
            string tmpDir       = Path.Combine(Path.GetDirectoryName(outputPath)!, "_m4b_tmp");
            string concatList   = Path.Combine(tmpDir, "concat.txt");
            string metadataFile = Path.Combine(tmpDir, "metadata.txt");
            string rawOut       = Path.Combine(tmpDir, copyMode ? "raw.m4a" : "raw.aac");
            string coverFile    = Path.Combine(tmpDir, "cover.jpg");

            Directory.CreateDirectory(tmpDir);

            // Step 1 — durations
            Log($"Analyzing {files.Length} file(s)...");
            var chapters = new List<Chapter>();
            double cursor = 0;
            for (int i = 0; i < files.Length; i++)
            {
                ct.ThrowIfCancellationRequested();
                Log($"  [{i + 1}/{files.Length}] {Path.GetFileName(files[i])}");
                lblProgress.Text = $"Analyzing file {i + 1} of {files.Length}…";
                double dur = await FfmpegHelper.GetAudioDurationAsync(ffmpeg, files[i], ct);
                string chName = !string.IsNullOrWhiteSpace(chapterNames[i]) ? chapterNames[i]
                    : Regex.Replace(Path.GetFileNameWithoutExtension(files[i]), @"^\d+[\s\.\-_]+", "").Trim();
                chapters.Add(new Chapter(chName, cursor, cursor + dur));
                cursor += dur;
            }
            if (chapters.Count > 0) chapters[^1] = chapters[^1] with { End = cursor };

            // Step 2 — cover art
            Log("\nResolving cover art...");
            bool hasCover = false;
            if (!string.IsNullOrEmpty(coverArtPath) && File.Exists(coverArtPath))
            {
                string ext = Path.GetExtension(coverArtPath);
                coverFile = Path.Combine(tmpDir, $"cover{ext}");
                File.Copy(coverArtPath, coverFile, true);
                hasCover = true;
                Log($"  Using selected cover: {Path.GetFileName(coverArtPath)}");
            }
            else
            {
                hasCover = await Task.Run(() => FfmpegHelper.ExtractCover(ffmpeg, files[0], coverFile), ct);
                Log(hasCover ? "  Extracted embedded cover art." : "  No cover art found, skipping.");
            }

            // Step 3 — concat list
            File.WriteAllLines(concatList, files.Select(f => $"file '{f.Replace("'", "''")}'"),
                new System.Text.UTF8Encoding(false));

            // Step 4 — encode / copy
            convStart = DateTime.Now;
            prgConvert.Value = 0;

            IProgress<double> encProg = new Progress<double>(pct =>
            {
                prgConvert.Value = (int)(pct * 100);
                double elapsed   = (DateTime.Now - convStart).TotalSeconds;
                if (pct > 0.02 && elapsed > 1)
                {
                    double remaining = elapsed / pct * (1.0 - pct);
                    lblProgress.Text = $"{pct:P0}  ·  ~{TimeSpan.FromSeconds(remaining):m\\:ss} remaining";
                }
                else
                {
                    lblProgress.Text = $"{pct:P0}";
                }
            });

            string encArgs;
            if (copyMode)
            {
                Log("\nStream copying audio (no re-encode)...");
                lblProgress.Text = "Stream copying…";
                encArgs = $"-y -f concat -safe 0 -i \"{concatList}\" -vn -c:a copy -f mp4 \"{rawOut}\"";
            }
            else
            {
                Log($"\nEncoding to AAC ({bitrate})...");
                encArgs = $"-y -f concat -safe 0 -i \"{concatList}\" -vn -c:a aac -b:a {bitrate} \"{rawOut}\"";
            }

            var (encCode, encErr) = await FfmpegHelper.RunFfmpegAsync(
                ffmpeg, encArgs, ct, copyMode ? null : encProg, copyMode ? 0 : cursor);
            if (encCode != 0) throw new Exception($"Encoding failed (code {encCode}):\n{encErr}");
            Log("Encoding complete.");

            // Step 5 — metadata
            prgConvert.Value = 95;
            lblProgress.Text = "Writing metadata…";
            Log("Writing chapter metadata...");
            FfmpegHelper.BuildMetadataFile(chapters, metadataFile, bookTitle, author, series);

            // Step 6 — mux
            lblProgress.Text = "Muxing…";
            Log("Muxing into M4B...");
            string muxArgs = hasCover
                ? $"-y -i \"{rawOut}\" -i \"{metadataFile}\" -i \"{coverFile}\" " +
                  $"-map 0:a -map 2:v -map_metadata 1 -c:a copy -c:v copy " +
                  $"-disposition:v:0 attached_pic -movflags +faststart \"{outputPath}\""
                : $"-y -i \"{rawOut}\" -i \"{metadataFile}\" -map_metadata 1 -c:a copy -movflags +faststart \"{outputPath}\"";

            var (muxCode, muxErr) = await FfmpegHelper.RunFfmpegAsync(ffmpeg, muxArgs, ct);
            if (muxCode != 0) throw new Exception($"Mux failed (code {muxCode}):\n{muxErr}");

            prgConvert.Value = 100;
            try { Directory.Delete(tmpDir, true); } catch { }

            Log($"\nDone! Saved to:\n  {outputPath}");
            var open = MessageBox.Show(
                $"Audiobook saved to:\n{outputPath}\n\nOpen output folder in Explorer?",
                "Done", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
            if (open == DialogResult.Yes)
                System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{outputPath}\"");
        }
        catch (OperationCanceledException)
        {
            Log("\nCancelled.");
            prgConvert.Value = 0;
        }
        catch (Exception ex)
        {
            Log($"\nERROR: {ex.Message}");
            MessageBox.Show($"Conversion failed:\n{ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            string tmpDir = Path.Combine(Path.GetDirectoryName(outputPath)!, "_m4b_tmp");
            try { Directory.Delete(tmpDir, true); } catch { }
        }
        finally
        {
            btnConvert.Enabled  = true;
            btnCancel.Visible   = false;
            prgConvert.Visible  = false;
            lblProgress.Visible = false;
            cts?.Dispose();
            cts = null;
        }
    }
}
