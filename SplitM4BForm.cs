namespace AudiobookMaker;

public class SplitM4BForm : Form
{
    TextBox      txtInput = null!, txtOutput = null!;
    ComboBox     cmbFormat = null!;
    Label        lblStatus = null!;
    DataGridView dgv       = null!;
    Button       btnBrowseIn = null!, btnBrowseOut = null!, btnSplit = null!;

    (string Title, double Start, double End)[] chapters = [];

    public SplitM4BForm()
    {
        InitializeComponent();
    }

    void InitializeComponent()
    {
        SuspendLayout();

        Text            = "Split M4B into Chapters";
        ClientSize      = new Size(680, 500);
        StartPosition   = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimumSize     = new Size(580, 400);
        BackColor       = Palette.Base;
        Font            = new Font("Segoe UI", 9.5f);

        // Header
        var header = new Panel { Location = new Point(0, 0), Size = new Size(680, 44), BackColor = Palette.Pine };
        header.Controls.Add(new Label
        {
            Text = "Split M4B into Chapters", Location = new Point(14, 10), Size = new Size(500, 24),
            ForeColor = Color.White, BackColor = Color.Transparent,
            Font = new Font("Segoe UI", 13f, FontStyle.Bold)
        });
        Controls.Add(header);

        int y = 58;

        // Input
        RowLabel("Input M4B:", y);
        txtInput    = Input(130, y, 412); txtInput.ReadOnly = true;
        btnBrowseIn = UtilBtn("Browse", 550, y - 1);
        y += 34;

        // Output folder
        RowLabel("Output Folder:", y);
        txtOutput    = Input(130, y, 412); txtOutput.ReadOnly = true;
        btnBrowseOut = UtilBtn("Browse", 550, y - 1);
        y += 34;

        // Format
        RowLabel("Output Format:", y);
        cmbFormat = new ComboBox
        {
            Location = new Point(130, y - 1), Size = new Size(140, 26),
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = Palette.Input, ForeColor = Palette.Text,
            FlatStyle = FlatStyle.Flat, Font = Font
        };
        foreach (string s in (string[])["M4B (copy)", "M4A (copy)", "MP3 (re-encode)"])
            cmbFormat.Items.Add(s);
        cmbFormat.SelectedIndex = 0;
        Controls.Add(cmbFormat);

        Controls.Add(new Label
        {
            Text = "Copy = instant, no quality loss.  MP3 re-encodes.",
            Location = new Point(280, y + 3), Size = new Size(390, 18),
            ForeColor = Palette.Subtext, Font = new Font("Segoe UI", 8.5f)
        });
        y += 36;

        // Chapters grid
        Divider(y); y += 8;
        SectionLabel("Chapters", y); y += 20;

        dgv = new DataGridView
        {
            Location  = new Point(12, y),
            Anchor    = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            Size      = new Size(656, 268),
            ReadOnly  = true,
            AllowUserToAddRows = false, AllowUserToDeleteRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            RowHeadersVisible = false,
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
        dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Start", HeaderText = "Start",         FillWeight = 20 });
        dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "End",   HeaderText = "End",           FillWeight = 20 });
        dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Title", HeaderText = "Chapter Title", FillWeight = 60 });
        Controls.Add(dgv);

        // Status + Split
        lblStatus = new Label
        {
            Text      = "Browse an M4B to load its chapters.",
            Location  = new Point(12, 458), Size = new Size(400, 18),
            Anchor    = AnchorStyles.Bottom | AnchorStyles.Left,
            ForeColor = Palette.Subtext, Font = new Font("Segoe UI", 8.5f)
        };
        Controls.Add(lblStatus);

        btnSplit = new Button
        {
            Text      = "Split into Files",
            Location  = new Point(432, 448), Size = new Size(220, 38),
            Anchor    = AnchorStyles.Bottom | AnchorStyles.Right,
            Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
            BackColor = Palette.Pine, ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand,
            Enabled   = false
        };
        btnSplit.FlatAppearance.BorderSize = 0;
        AddHover(btnSplit, Palette.Pine, Palette.PineDark);
        Controls.Add(btnSplit);

        btnBrowseIn.Click  += BtnBrowseIn_Click;
        btnBrowseOut.Click += BtnBrowseOut_Click;
        btnSplit.Click     += BtnSplit_Click;

        ResumeLayout();
    }

    void RowLabel(string text, int y) =>
        Controls.Add(new Label { Text = text, Location = new Point(12, y + 3), Size = new Size(116, 18), ForeColor = Palette.Subtext });

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
        using var dlg = new OpenFileDialog { Title = "Select M4B to split", Filter = "M4B Audiobook (*.m4b)|*.m4b|All files (*.*)|*.*" };
        if (dlg.ShowDialog() != DialogResult.OK) return;

        txtInput.Text = dlg.FileName;
        if (string.IsNullOrEmpty(txtOutput.Text))
            txtOutput.Text = Path.GetDirectoryName(dlg.FileName)!;

        dgv.Rows.Clear();
        chapters         = [];
        btnSplit.Enabled = false;
        lblStatus.Text   = "Reading chapters...";

        string? ffprobe = FfmpegHelper.FindFfprobe();
        if (ffprobe == null) { lblStatus.Text = "ffprobe not found — run a conversion first to download it."; return; }

        try
        {
            var (_, loaded) = await FfmpegHelper.ReadChaptersAsync(ffprobe, dlg.FileName);
            if (loaded.Length == 0) { lblStatus.Text = "No chapters found in this file."; return; }

            chapters = loaded;
            foreach (var (title, start, end) in chapters)
                dgv.Rows.Add(FfmpegHelper.FormatTimestamp(start), FfmpegHelper.FormatTimestamp(end), title);

            lblStatus.Text   = $"Found {chapters.Length} chapter(s). Ready to split.";
            btnSplit.Enabled = true;
        }
        catch (Exception ex) { lblStatus.Text = $"Error: {ex.Message}"; }
    }

    void BtnBrowseOut_Click(object? sender, EventArgs e)
    {
        using var dlg = new FolderBrowserDialog { Description = "Select output folder for chapter files" };
        if (dlg.ShowDialog() == DialogResult.OK) txtOutput.Text = dlg.SelectedPath;
    }

    async void BtnSplit_Click(object? sender, EventArgs e)
    {
        string inputFile = txtInput.Text.Trim();
        string outputDir = txtOutput.Text.Trim();

        string? ffmpeg = FfmpegHelper.FindFfmpeg();
        if (ffmpeg == null)  { MessageBox.Show("ffmpeg not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        if (!File.Exists(inputFile)) { MessageBox.Show("Input file not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        if (string.IsNullOrEmpty(outputDir)) { MessageBox.Show("Select an output folder.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

        Directory.CreateDirectory(outputDir);
        string fmt      = cmbFormat.SelectedItem!.ToString()!;
        string ext      = fmt.StartsWith("MP3") ? "mp3" : fmt.StartsWith("M4A") ? "m4a" : "m4b";
        bool   reEncode = fmt.StartsWith("MP3");

        btnSplit.Enabled = false;
        try
        {
            for (int i = 0; i < chapters.Length; i++)
            {
                var (title, start, end) = chapters[i];
                string safeTitle = string.Join("_", title.Split(Path.GetInvalidFileNameChars()));
                string outFile   = Path.Combine(outputDir, $"{i + 1:D2} - {safeTitle}.{ext}");

                lblStatus.Text = $"[{i + 1}/{chapters.Length}] {title}";
                Application.DoEvents();

                string encArgs = reEncode
                    ? $"-y -i \"{inputFile}\" -ss {start} -to {end} -vn -c:a libmp3lame -q:a 2 \"{outFile}\""
                    : $"-y -i \"{inputFile}\" -ss {start} -to {end} -c copy \"{outFile}\"";

                var (code, err) = await FfmpegHelper.RunFfmpegAsync(ffmpeg, encArgs, CancellationToken.None);
                if (code != 0) { lblStatus.Text = $"Error on chapter {i + 1}."; btnSplit.Enabled = true; return; }
            }

            lblStatus.Text   = $"Done — {chapters.Length} file(s) saved.";
            btnSplit.Enabled = true;

            var open = MessageBox.Show($"Split {chapters.Length} chapter(s) to:\n{outputDir}\n\nOpen folder?",
                "Done", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
            if (open == DialogResult.Yes)
                System.Diagnostics.Process.Start("explorer.exe", $"\"{outputDir}\"");
        }
        catch (Exception ex) { lblStatus.Text = $"Error: {ex.Message}"; btnSplit.Enabled = true; }
    }
}
