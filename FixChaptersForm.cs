namespace AudiobookMaker;

public class FixChaptersForm : Form
{
    TextBox      txtInput = null!, txtOutput = null!;
    Label        lblDuration = null!, lblStatus = null!;
    DataGridView dgv         = null!;
    Button       btnBrowseIn = null!, btnBrowseOut = null!,
                 btnAdd      = null!, btnRemove    = null!, btnApply = null!;

    double totalDuration = 0;

    public FixChaptersForm()
    {
        InitializeComponent();
    }

    void InitializeComponent()
    {
        SuspendLayout();

        Text            = "Fix M4B Chapter Metadata";
        ClientSize      = new Size(680, 560);
        StartPosition   = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimumSize     = new Size(580, 460);
        BackColor       = Palette.Base;
        Font            = new Font("Segoe UI", 9.5f);

        // Header
        var header = new Panel { Location = new Point(0, 0), Size = new Size(680, 44), BackColor = Palette.Pine };
        header.Controls.Add(new Label
        {
            Text = "Fix M4B Chapter Metadata", Location = new Point(14, 10), Size = new Size(500, 24),
            ForeColor = Color.White, BackColor = Color.Transparent,
            Font = new Font("Segoe UI", 13f, FontStyle.Bold)
        });
        Controls.Add(header);

        int y = 58;

        // Input
        RowLabel("Input M4B:", 12, y);
        txtInput    = Input(130, y, 412);  txtInput.ReadOnly = true;
        btnBrowseIn = UtilBtn("Browse", 550, y - 1);
        y += 32;

        lblDuration = new Label
        {
            Text = "Duration: —", Location = new Point(132, y), Size = new Size(400, 16),
            ForeColor = Palette.Subtext, Font = new Font("Segoe UI", 8.5f)
        };
        Controls.Add(lblDuration);
        y += 22;

        // Output
        RowLabel("Output M4B:", 12, y);
        txtOutput    = Input(130, y, 412);
        btnBrowseOut = UtilBtn("Browse", 550, y - 1);
        y += 36;

        // Divider + Chapters label
        Divider(y); y += 8;
        SectionLabel("Chapters", y); y += 20;

        // Grid
        dgv = new DataGridView
        {
            Location  = new Point(12, y),
            Anchor    = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            Size      = new Size(538, 310),
            AllowUserToAddRows = false, AllowUserToDeleteRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false, RowHeadersVisible = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            BackgroundColor = Palette.Input,
            GridColor = Palette.Border,
            BorderStyle = BorderStyle.FixedSingle,
            Font = Font
        };
        dgv.ColumnHeadersDefaultCellStyle.BackColor = Palette.Surface;
        dgv.ColumnHeadersDefaultCellStyle.ForeColor = Palette.Text;
        dgv.DefaultCellStyle.BackColor = Palette.Input;
        dgv.DefaultCellStyle.ForeColor = Palette.Text;
        dgv.DefaultCellStyle.SelectionBackColor = Palette.PaleMint;
        dgv.DefaultCellStyle.SelectionForeColor = Palette.Text;
        dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "StartTime",   HeaderText = "Start Time (H:MM:SS)", FillWeight = 30 });
        dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "ChapterName", HeaderText = "Chapter Name",         FillWeight = 70 });
        Controls.Add(dgv);

        // Side buttons
        btnAdd    = UtilBtn("+ Add",    558, y);    btnAdd.Anchor    = AnchorStyles.Top | AnchorStyles.Right; y += 32;
        btnRemove = UtilBtn("✕ Remove", 558, y);    btnRemove.Anchor = AnchorStyles.Top | AnchorStyles.Right;

        // Status + Apply
        lblStatus = new Label
        {
            Text = "", Location = new Point(12, 506), Size = new Size(380, 18),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
            ForeColor = Palette.Subtext, Font = new Font("Segoe UI", 8.5f)
        };
        Controls.Add(lblStatus);

        btnApply = new Button
        {
            Text      = "Apply Chapters",
            Location  = new Point(400, 496), Size = new Size(240, 38),
            Anchor    = AnchorStyles.Bottom | AnchorStyles.Right,
            Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
            BackColor = Palette.Pine, ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand
        };
        btnApply.FlatAppearance.BorderSize = 0;
        AddHover(btnApply, Palette.Pine, Palette.PineDark);
        Controls.Add(btnApply);

        btnBrowseIn.Click  += BtnBrowseIn_Click;
        btnBrowseOut.Click += BtnBrowseOut_Click;
        btnAdd.Click       += BtnAdd_Click;
        btnRemove.Click    += BtnRemove_Click;
        btnApply.Click     += BtnApply_Click;

        ResumeLayout();
    }

    void RowLabel(string text, int x, int y) =>
        Controls.Add(new Label { Text = text, Location = new Point(x, y + 3), Size = new Size(116, 18), ForeColor = Palette.Subtext });

    void SectionLabel(string text, int y) =>
        Controls.Add(new Label { Text = text.ToUpper(), Location = new Point(14, y), Size = new Size(300, 14),
            ForeColor = Palette.Pine, Font = new Font("Segoe UI", 7.5f, FontStyle.Bold) });

    void Divider(int y) =>
        Controls.Add(new Panel { Location = new Point(12, y), Size = new Size(656, 1), BackColor = Palette.Border });

    TextBox Input(int x, int y, int w)
    {
        var t = new TextBox { Location = new Point(x, y - 1), Size = new Size(w, 24), BorderStyle = BorderStyle.FixedSingle,
            BackColor = Palette.Input, ForeColor = Palette.Text, Font = Font };
        Controls.Add(t); return t;
    }

    Button UtilBtn(string text, int x, int y)
    {
        var b = new Button { Text = text, Location = new Point(x, y), Size = new Size(108, 26),
            BackColor = Palette.Surface, ForeColor = Palette.Pine, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand,
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

    // ── Events ────────────────────────────────────────────────────────────────

    async void BtnBrowseIn_Click(object? sender, EventArgs e)
    {
        using var dlg = new OpenFileDialog { Title = "Select M4B file to fix", Filter = "M4B Audiobook (*.m4b)|*.m4b|All files (*.*)|*.*" };
        if (dlg.ShowDialog() != DialogResult.OK) return;

        txtInput.Text = dlg.FileName;
        if (string.IsNullOrEmpty(txtOutput.Text))
            txtOutput.Text = Path.Combine(Path.GetDirectoryName(dlg.FileName)!,
                Path.GetFileNameWithoutExtension(dlg.FileName) + "_fixed.m4b");

        dgv.Rows.Clear();
        lblDuration.Text = "Duration: reading...";
        lblStatus.Text   = "";

        string? ffprobe = FfmpegHelper.FindFfprobe();
        if (ffprobe == null)
        {
            lblDuration.Text = "Duration: ffprobe not available";
            dgv.Rows.Add("0:00:00", "Chapter 1");
            lblStatus.Text = "ffprobe not found — run a conversion first to download it.";
            return;
        }
        try
        {
            var (dur, chapters) = await FfmpegHelper.ReadChaptersAsync(ffprobe, dlg.FileName);
            totalDuration    = dur;
            lblDuration.Text = $"Duration: {FfmpegHelper.FormatTimestamp(dur)}";

            if (chapters.Length > 0)
            {
                foreach (var (title, start, _) in chapters)
                    dgv.Rows.Add(FfmpegHelper.FormatTimestamp(start), title);
                lblStatus.Text = $"Loaded {chapters.Length} existing chapter(s).";
            }
            else
            {
                dgv.Rows.Add("0:00:00", "Chapter 1");
                lblStatus.Text = "No chapters found — starting from scratch.";
            }
        }
        catch (Exception ex)
        {
            lblDuration.Text = "Duration: could not read";
            dgv.Rows.Add("0:00:00", "Chapter 1");
            lblStatus.Text = $"Error: {ex.Message}";
        }
    }

    void BtnBrowseOut_Click(object? sender, EventArgs e)
    {
        using var dlg = new SaveFileDialog { Title = "Save fixed M4B as...", Filter = "M4B Audiobook (*.m4b)|*.m4b|All files (*.*)|*.*", DefaultExt = "m4b" };
        if (dlg.ShowDialog() == DialogResult.OK) txtOutput.Text = dlg.FileName;
    }

    void BtnAdd_Click(object? sender, EventArgs e)
    {
        int idx = dgv.CurrentRow?.Index ?? -1;
        if (idx >= 0) dgv.Rows.Insert(idx + 1, "", "Chapter");
        else          dgv.Rows.Add("", "Chapter");
    }

    void BtnRemove_Click(object? sender, EventArgs e)
    {
        int idx = dgv.CurrentRow?.Index ?? -1;
        if (idx >= 0 && dgv.Rows.Count > 1) dgv.Rows.RemoveAt(idx);
    }

    async void BtnApply_Click(object? sender, EventArgs e)
    {
        string inputFile  = txtInput.Text.Trim();
        string outputFile = txtOutput.Text.Trim();

        if (string.IsNullOrEmpty(inputFile) || !File.Exists(inputFile))
        { MessageBox.Show("Select a valid input M4B file.", "Missing input", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        if (string.IsNullOrEmpty(outputFile))
        { MessageBox.Show("Set an output file path.", "Missing output", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

        string? ffmpeg = FfmpegHelper.FindFfmpeg();
        if (ffmpeg == null)
        { MessageBox.Show("ffmpeg not found. Run a conversion from the main window first.", "ffmpeg required", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

        var chapters = new List<(string Title, double Start)>();
        for (int r = 0; r < dgv.Rows.Count; r++)
        {
            string? timeVal = dgv.Rows[r].Cells["StartTime"].Value?.ToString();
            string? nameVal = dgv.Rows[r].Cells["ChapterName"].Value?.ToString();
            if (string.IsNullOrWhiteSpace(timeVal)) continue;
            try { chapters.Add((string.IsNullOrWhiteSpace(nameVal) ? $"Chapter {r + 1}" : nameVal, FfmpegHelper.ParseTimestamp(timeVal))); }
            catch (Exception ex) { MessageBox.Show($"Row {r + 1}: {ex.Message}", "Invalid timestamp", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        }

        if (chapters.Count == 0)
        { MessageBox.Show("Add at least one chapter.", "No chapters", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

        var sorted = chapters.OrderBy(c => c.Start).ToList();
        double total = totalDuration > 0 ? totalDuration : 999999;
        var chapterList = sorted.Select((c, i) =>
            new Chapter(c.Title, c.Start, i + 1 < sorted.Count ? sorted[i + 1].Start : total)).ToList();

        btnApply.Enabled = false;
        lblStatus.Text   = "Writing metadata...";

        try
        {
            string tmpDir   = Path.Combine(Path.GetTempPath(), "FixM4B_tmp");
            Directory.CreateDirectory(tmpDir);
            string metaFile = Path.Combine(tmpDir, "chapters.txt");

            var lines = new List<string> { ";FFMETADATA1" };
            foreach (var ch in chapterList)
                lines.AddRange(["[CHAPTER]", "TIMEBASE=1/1000",
                    $"START={(long)(ch.Start * 1000)}", $"END={(long)(ch.End * 1000)}", $"title={ch.Title}", ""]);
            File.WriteAllLines(metaFile, lines, new System.Text.UTF8Encoding(false));

            lblStatus.Text = "Remuxing (no re-encode)...";
            var (code, err) = await FfmpegHelper.RunFfmpegAsync(ffmpeg,
                $"-y -i \"{inputFile}\" -i \"{metaFile}\" -map 0 -map_metadata 0 -map_chapters 1 -c copy \"{outputFile}\"",
                CancellationToken.None);
            if (code != 0) throw new Exception($"ffmpeg failed (code {code}):\n{err}");

            try { Directory.Delete(tmpDir, true); } catch { }
            lblStatus.Text = "Done.";

            var open = MessageBox.Show($"Saved to:\n{outputFile}\n\nOpen in Explorer?", "Done",
                MessageBoxButtons.YesNo, MessageBoxIcon.Information);
            if (open == DialogResult.Yes)
                System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{outputFile}\"");
        }
        catch (Exception ex)
        {
            lblStatus.Text = $"Error: {ex.Message}";
            MessageBox.Show($"Failed:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally { btnApply.Enabled = true; }
    }
}
