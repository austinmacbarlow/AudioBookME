Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
[System.Windows.Forms.Application]::EnableVisualStyles()

# ---------------------------------------------------------------------------
# ffmpeg
# ---------------------------------------------------------------------------
$script:ffmpegDir = Join-Path $env:APPDATA 'AudiobookMaker'
$script:ffmpegExe = Join-Path $script:ffmpegDir 'ffmpeg.exe'

function Find-Ffmpeg {
    $local = Join-Path $PSScriptRoot 'ffmpeg.exe'
    if (Test-Path $local) { return $local }
    if (Test-Path $script:ffmpegExe) { return $script:ffmpegExe }
    $inPath = Get-Command ffmpeg -ErrorAction SilentlyContinue
    if ($inPath) { return $inPath.Source }
    return $null
}

function Download-Ffmpeg {
    New-Item -ItemType Directory -Force -Path $script:ffmpegDir | Out-Null
    $zipPath = Join-Path $script:ffmpegDir 'ffmpeg.zip'
    $urls = @(
        'https://github.com/BtbN/FFmpeg-Builds/releases/download/latest/ffmpeg-master-latest-win64-gpl.zip',
        'https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip'
    )
    $downloaded = $false
    foreach ($url in $urls) {
        Log-Line "Downloading ffmpeg from:`n  $url"
        try {
            $wc = New-Object System.Net.WebClient
            $wc.DownloadFile($url, $zipPath)
            $downloaded = $true
            break
        } catch {
            Log-Line "  Failed, trying next source..."
        }
    }
    if (-not $downloaded) {
        Log-Line "ERROR: Could not download ffmpeg. Place ffmpeg.exe next to AudiobookMaker.vbs manually."
        return $null
    }
    Log-Line "Extracting..."
    Expand-Archive -Path $zipPath -DestinationPath $script:ffmpegDir -Force
    $found = Get-ChildItem -Recurse -Filter 'ffmpeg.exe' -Path $script:ffmpegDir |
             Where-Object { $_.DirectoryName -notmatch 'ffprobe' } |
             Select-Object -First 1
    if (-not $found) {
        Log-Line "ERROR: ffmpeg.exe not found in archive."
        return $null
    }
    Copy-Item $found.FullName $script:ffmpegExe -Force
    Remove-Item $zipPath -Force -ErrorAction SilentlyContinue
    Get-ChildItem $script:ffmpegDir -Exclude 'ffmpeg.exe' |
        Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
    Log-Line "ffmpeg ready.`n"
    return $script:ffmpegExe
}

# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------
function Log-Line([string]$msg) {
    $txtLog.AppendText($msg + "`r`n")
    $txtLog.ScrollToCaret()
    [System.Windows.Forms.Application]::DoEvents()
}

function Get-AudioDuration([string]$ffmpeg, [string]$file) {
    $info = & $ffmpeg -i $file -hide_banner 2>&1 | Out-String
    if ($info -match 'Duration:\s*(\d+):(\d+):([\d\.]+)') {
        return [int]$Matches[1] * 3600 + [int]$Matches[2] * 60 + [double]$Matches[3]
    }
    return 0
}

function Build-MetadataFile([array]$chapters, [string]$outPath, [string]$title, [string]$author) {
    $lines = @(';FFMETADATA1')
    if ($title)  { $lines += "title=$title" }
    if ($author) { $lines += "artist=$author"; $lines += "album_artist=$author" }
    if ($title)  { $lines += "album=$title" }
    $lines += 'genre=Audiobook'
    $lines += ''
    foreach ($ch in $chapters) {
        $startMs = [long]($ch.Start * 1000)
        $endMs   = [long]($ch.End   * 1000)
        $lines += '[CHAPTER]'
        $lines += 'TIMEBASE=1/1000'
        $lines += "START=$startMs"
        $lines += "END=$endMs"
        $lines += "title=$($ch.Title)"
        $lines += ''
    }
    [System.IO.File]::WriteAllLines($outPath, $lines, [System.Text.UTF8Encoding]::new($false))
}

function Extract-Cover([string]$ffmpeg, [string]$mp3, [string]$outPath) {
    $args = "-y -i `"$mp3`" -an -vcodec copy `"$outPath`""
    $proc = Start-Process -FilePath $ffmpeg -ArgumentList $args `
                -NoNewWindow -Wait -PassThru -RedirectStandardError (Join-Path ([System.IO.Path]::GetDirectoryName($outPath)) 'cover.log')
    return ($proc.ExitCode -eq 0 -and (Test-Path $outPath) -and (Get-Item $outPath).Length -gt 0)
}

function Run-Conversion([string]$ffmpeg, [string[]]$files, [string]$outputPath, [string]$bitrate, [string]$bookTitle, [string]$author) {
    $btnConvert.Enabled = $false
    $txtLog.Clear()

    try {
        $tmpDir       = Join-Path ([System.IO.Path]::GetDirectoryName($outputPath)) '_m4b_tmp'
        $concatList   = Join-Path $tmpDir 'concat.txt'
        $metadataFile = Join-Path $tmpDir 'metadata.txt'
        $rawAac       = Join-Path $tmpDir 'raw.aac'
        $coverFile    = Join-Path $tmpDir 'cover.jpg'
        $encodeLog    = Join-Path $tmpDir 'encode.log'
        $muxLog       = Join-Path $tmpDir 'mux.log'

        New-Item -ItemType Directory -Force -Path $tmpDir | Out-Null

        # Step 1 - durations
        Log-Line "Analyzing $($files.Count) file(s)..."
        $chapters = @()
        $cursor   = 0.0
        for ($i = 0; $i -lt $files.Count; $i++) {
            $f    = $files[$i]
            $name = [System.IO.Path]::GetFileNameWithoutExtension($f)
            Log-Line "  [$($i+1)/$($files.Count)] $([System.IO.Path]::GetFileName($f))"
            $dur   = Get-AudioDuration $ffmpeg $f
            $title = ($name -replace '^\d+[\s\.\-_]+', '').Trim()
            $chapters += [PSCustomObject]@{ Title = $title; Start = $cursor; End = $cursor + $dur }
            $cursor += $dur
        }
        if ($chapters.Count -gt 0) { $chapters[-1].End = $cursor }

        # Step 2 - extract cover art from first file
        Log-Line "`nExtracting cover art..."
        $hasCover = Extract-Cover $ffmpeg $files[0] $coverFile
        if ($hasCover) { Log-Line "  Cover found." }
        else           { Log-Line "  No cover art found in MP3s, skipping." }

        # Step 3 - concat list (UTF-8 no BOM)
        $concatLines = $files | ForEach-Object { "file '$($_ -replace "'", "''")'" }
        [System.IO.File]::WriteAllLines($concatList, $concatLines, [System.Text.UTF8Encoding]::new($false))

        # Step 4 - encode
        Log-Line "`nEncoding to AAC ($bitrate) - please wait..."
        $encArgs = "-y -f concat -safe 0 -i `"$concatList`" -vn -c:a aac -b:a $bitrate `"$rawAac`""
        $proc = Start-Process -FilePath $ffmpeg -ArgumentList $encArgs `
                    -NoNewWindow -Wait -PassThru -RedirectStandardError $encodeLog
        if ($proc.ExitCode -ne 0) {
            $err = Get-Content $encodeLog -Raw -ErrorAction SilentlyContinue
            throw "Encoding failed (code $($proc.ExitCode)):`n$err"
        }
        Log-Line "Encoding complete."

        # Step 5 - metadata file
        Log-Line "Writing metadata and chapter markers..."
        Build-MetadataFile $chapters $metadataFile $bookTitle $author

        # Step 6 - mux (with or without cover)
        Log-Line "Muxing into M4B..."
        if ($hasCover) {
            $muxArgs = "-y -i `"$rawAac`" -i `"$metadataFile`" -i `"$coverFile`" " +
                       "-map 0:a -map 2:v -map_metadata 1 -c:a copy -c:v copy " +
                       "-disposition:v:0 attached_pic -movflags +faststart `"$outputPath`""
        } else {
            $muxArgs = "-y -i `"$rawAac`" -i `"$metadataFile`" -map_metadata 1 -c:a copy -movflags +faststart `"$outputPath`""
        }
        $proc2 = Start-Process -FilePath $ffmpeg -ArgumentList $muxArgs `
                     -NoNewWindow -Wait -PassThru -RedirectStandardError $muxLog
        if ($proc2.ExitCode -ne 0) {
            $err = Get-Content $muxLog -Raw -ErrorAction SilentlyContinue
            throw "Mux failed (code $($proc2.ExitCode)):`n$err"
        }

        Remove-Item $tmpDir -Recurse -Force -ErrorAction SilentlyContinue

        Log-Line "`nDone! Saved to:`n  $outputPath"
        [System.Windows.Forms.MessageBox]::Show(
            "Audiobook saved to:`n$outputPath", 'Done',
            [System.Windows.Forms.MessageBoxButtons]::OK,
            [System.Windows.Forms.MessageBoxIcon]::Information) | Out-Null

    } catch {
        Log-Line "`nERROR: $_"
        [System.Windows.Forms.MessageBox]::Show("Conversion failed:`n$_", 'Error',
            [System.Windows.Forms.MessageBoxButtons]::OK,
            [System.Windows.Forms.MessageBoxIcon]::Error) | Out-Null
    } finally {
        $btnConvert.Enabled = $true
    }
}

# ---------------------------------------------------------------------------
# GUI
# ---------------------------------------------------------------------------
$form = New-Object System.Windows.Forms.Form
$form.Text            = 'MP3 to M4B Audiobook Maker'
$form.Size            = New-Object System.Drawing.Size(660, 680)
$form.StartPosition   = 'CenterScreen'
$form.FormBorderStyle = 'FixedSingle'
$form.MaximizeBox     = $false
$form.Font            = New-Object System.Drawing.Font('Segoe UI', 9)

function New-Label([string]$text, [int]$x, [int]$y, [int]$w = 130) {
    $l           = New-Object System.Windows.Forms.Label
    $l.Text      = $text
    $l.Location  = New-Object System.Drawing.Point($x, $y)
    $l.Size      = New-Object System.Drawing.Size($w, 20)
    $l.TextAlign = 'MiddleLeft'
    return $l
}

# Input folder
$form.Controls.Add((New-Label 'Input Folder (MP3s):' 10 18))
$txtInput          = New-Object System.Windows.Forms.TextBox
$txtInput.Location = New-Object System.Drawing.Point(145, 15)
$txtInput.Size     = New-Object System.Drawing.Size(385, 22)
$txtInput.ReadOnly = $true
$form.Controls.Add($txtInput)

$btnBrowseIn          = New-Object System.Windows.Forms.Button
$btnBrowseIn.Text     = 'Browse...'
$btnBrowseIn.Location = New-Object System.Drawing.Point(540, 14)
$btnBrowseIn.Size     = New-Object System.Drawing.Size(90, 24)
$form.Controls.Add($btnBrowseIn)

# Output file
$form.Controls.Add((New-Label 'Output File (.m4b):' 10 52))
$txtOutput          = New-Object System.Windows.Forms.TextBox
$txtOutput.Location = New-Object System.Drawing.Point(145, 49)
$txtOutput.Size     = New-Object System.Drawing.Size(385, 22)
$form.Controls.Add($txtOutput)

$btnBrowseOut          = New-Object System.Windows.Forms.Button
$btnBrowseOut.Text     = 'Browse...'
$btnBrowseOut.Location = New-Object System.Drawing.Point(540, 48)
$btnBrowseOut.Size     = New-Object System.Drawing.Size(90, 24)
$form.Controls.Add($btnBrowseOut)

# Book title
$form.Controls.Add((New-Label 'Book Title:' 10 88))
$txtTitle          = New-Object System.Windows.Forms.TextBox
$txtTitle.Location = New-Object System.Drawing.Point(145, 85)
$txtTitle.Size     = New-Object System.Drawing.Size(485, 22)
$form.Controls.Add($txtTitle)

# Author
$form.Controls.Add((New-Label 'Author:' 10 118))
$txtAuthor          = New-Object System.Windows.Forms.TextBox
$txtAuthor.Location = New-Object System.Drawing.Point(145, 115)
$txtAuthor.Size     = New-Object System.Drawing.Size(485, 22)
$form.Controls.Add($txtAuthor)

# Options
$form.Controls.Add((New-Label 'Bitrate:' 10 150))
$cmbBitrate               = New-Object System.Windows.Forms.ComboBox
$cmbBitrate.Location      = New-Object System.Drawing.Point(145, 147)
$cmbBitrate.Size          = New-Object System.Drawing.Size(70, 22)
$cmbBitrate.DropDownStyle = 'DropDownList'
'32k','48k','64k','96k','128k' | ForEach-Object { $cmbBitrate.Items.Add($_) | Out-Null }
$cmbBitrate.SelectedItem  = '64k'
$form.Controls.Add($cmbBitrate)

$form.Controls.Add((New-Label 'Sort by:' 240 150 60))
$rdoName          = New-Object System.Windows.Forms.RadioButton
$rdoName.Text     = 'Filename'
$rdoName.Location = New-Object System.Drawing.Point(305, 147)
$rdoName.Size     = New-Object System.Drawing.Size(85, 22)
$rdoName.Checked  = $true
$form.Controls.Add($rdoName)

$rdoDate          = New-Object System.Windows.Forms.RadioButton
$rdoDate.Text     = 'Modified date'
$rdoDate.Location = New-Object System.Drawing.Point(395, 147)
$rdoDate.Size     = New-Object System.Drawing.Size(110, 22)
$form.Controls.Add($rdoDate)

# File list
$form.Controls.Add((New-Label 'Files to process:' 10 182))
$listBox               = New-Object System.Windows.Forms.ListBox
$listBox.Location      = New-Object System.Drawing.Point(10, 202)
$listBox.Size          = New-Object System.Drawing.Size(500, 145)
$listBox.SelectionMode = 'One'
$form.Controls.Add($listBox)

$btnUp          = New-Object System.Windows.Forms.Button
$btnUp.Text     = 'Up'
$btnUp.Location = New-Object System.Drawing.Point(520, 202)
$btnUp.Size     = New-Object System.Drawing.Size(110, 28)
$form.Controls.Add($btnUp)

$btnDown          = New-Object System.Windows.Forms.Button
$btnDown.Text     = 'Down'
$btnDown.Location = New-Object System.Drawing.Point(520, 236)
$btnDown.Size     = New-Object System.Drawing.Size(110, 28)
$form.Controls.Add($btnDown)

$btnRemove          = New-Object System.Windows.Forms.Button
$btnRemove.Text     = 'Remove'
$btnRemove.Location = New-Object System.Drawing.Point(520, 270)
$btnRemove.Size     = New-Object System.Drawing.Size(110, 28)
$form.Controls.Add($btnRemove)

$btnRefresh          = New-Object System.Windows.Forms.Button
$btnRefresh.Text     = 'Refresh'
$btnRefresh.Location = New-Object System.Drawing.Point(520, 304)
$btnRefresh.Size     = New-Object System.Drawing.Size(110, 28)
$form.Controls.Add($btnRefresh)

# Convert button
$btnConvert           = New-Object System.Windows.Forms.Button
$btnConvert.Text      = 'Convert to M4B'
$btnConvert.Location  = New-Object System.Drawing.Point(200, 360)
$btnConvert.Size      = New-Object System.Drawing.Size(240, 36)
$btnConvert.Font      = New-Object System.Drawing.Font('Segoe UI', 11, [System.Drawing.FontStyle]::Bold)
$btnConvert.BackColor = [System.Drawing.Color]::FromArgb(0, 120, 215)
$btnConvert.ForeColor = [System.Drawing.Color]::White
$btnConvert.FlatStyle = 'Flat'
$form.Controls.Add($btnConvert)

# Log
$form.Controls.Add((New-Label 'Log:' 10 408))
$txtLog            = New-Object System.Windows.Forms.RichTextBox
$txtLog.Location   = New-Object System.Drawing.Point(10, 425)
$txtLog.Size       = New-Object System.Drawing.Size(620, 195)
$txtLog.ReadOnly   = $true
$txtLog.BackColor  = [System.Drawing.Color]::FromArgb(30, 30, 30)
$txtLog.ForeColor  = [System.Drawing.Color]::FromArgb(212, 212, 212)
$txtLog.Font       = New-Object System.Drawing.Font('Consolas', 9)
$txtLog.ScrollBars = 'Vertical'
$form.Controls.Add($txtLog)

# ---------------------------------------------------------------------------
# State + helpers
# ---------------------------------------------------------------------------
$script:fileList = [System.Collections.Generic.List[string]]::new()

function Refresh-List {
    $folder = $txtInput.Text
    if (-not $folder -or -not (Test-Path $folder)) { return }
    $mp3s = Get-ChildItem -Path $folder -Filter '*.mp3'
    if ($rdoDate.Checked) { $mp3s = $mp3s | Sort-Object LastWriteTime }
    else                  { $mp3s = $mp3s | Sort-Object Name }
    $script:fileList.Clear()
    $listBox.Items.Clear()
    foreach ($f in $mp3s) {
        $script:fileList.Add($f.FullName)
        $listBox.Items.Add($f.Name) | Out-Null
    }
    Log-Line "Found $($script:fileList.Count) MP3 file(s)."
}

function Swap-Items([int]$a, [int]$b) {
    $tmpF = $script:fileList[$a]; $script:fileList[$a] = $script:fileList[$b]; $script:fileList[$b] = $tmpF
    $tmpN = $listBox.Items[$a];   $listBox.Items[$a]   = $listBox.Items[$b];   $listBox.Items[$b]   = $tmpN
}

# ---------------------------------------------------------------------------
# Events
# ---------------------------------------------------------------------------
$btnBrowseIn.Add_Click({
    $dlg = New-Object System.Windows.Forms.FolderBrowserDialog
    $dlg.Description = 'Select folder containing MP3 files'
    if ($dlg.ShowDialog() -eq 'OK') {
        $txtInput.Text = $dlg.SelectedPath
        $folderName = Split-Path $dlg.SelectedPath -Leaf
        if (-not $txtOutput.Text) {
            $txtOutput.Text = Join-Path (Split-Path $dlg.SelectedPath -Parent) ($folderName + '.m4b')
        }
        if (-not $txtTitle.Text) { $txtTitle.Text = $folderName }
        Refresh-List
    }
})

$btnBrowseOut.Add_Click({
    $dlg = New-Object System.Windows.Forms.SaveFileDialog
    $dlg.Filter     = 'M4B Audiobook (*.m4b)|*.m4b|All files (*.*)|*.*'
    $dlg.DefaultExt = 'm4b'
    $dlg.Title      = 'Save M4B audiobook as...'
    if ($dlg.ShowDialog() -eq 'OK') { $txtOutput.Text = $dlg.FileName }
})

$btnRefresh.Add_Click({ Refresh-List })
$rdoName.Add_CheckedChanged({ if ($rdoName.Checked) { Refresh-List } })
$rdoDate.Add_CheckedChanged({ if ($rdoDate.Checked) { Refresh-List } })

$btnUp.Add_Click({
    $i = $listBox.SelectedIndex
    if ($i -le 0) { return }
    Swap-Items ($i - 1) $i
    $listBox.SelectedIndex = $i - 1
})

$btnDown.Add_Click({
    $i = $listBox.SelectedIndex
    if ($i -lt 0 -or $i -ge $script:fileList.Count - 1) { return }
    Swap-Items $i ($i + 1)
    $listBox.SelectedIndex = $i + 1
})

$btnRemove.Add_Click({
    $i = $listBox.SelectedIndex
    if ($i -lt 0) { return }
    $script:fileList.RemoveAt($i)
    $listBox.Items.RemoveAt($i)
})

$btnConvert.Add_Click({
    if ($script:fileList.Count -eq 0) {
        [System.Windows.Forms.MessageBox]::Show('No MP3 files loaded. Browse to a folder first.',
            'Nothing to convert', 'OK', 'Warning') | Out-Null
        return
    }
    $output = $txtOutput.Text.Trim()
    if (-not $output) {
        [System.Windows.Forms.MessageBox]::Show('Set an output file path.', 'Missing output', 'OK', 'Warning') | Out-Null
        return
    }

    $ffmpeg = Find-Ffmpeg
    if (-not $ffmpeg) {
        $ans = [System.Windows.Forms.MessageBox]::Show(
            "ffmpeg was not found.`n`nDownload it automatically? (~60 MB, one-time download saved to AppData)",
            'ffmpeg required',
            [System.Windows.Forms.MessageBoxButtons]::YesNo,
            [System.Windows.Forms.MessageBoxIcon]::Question)
        if ($ans -ne 'Yes') { return }
        $txtLog.Clear()
        $btnConvert.Enabled = $false
        $ffmpeg = Download-Ffmpeg
        $btnConvert.Enabled = $true
        if (-not $ffmpeg) { return }
    }

    Run-Conversion $ffmpeg $script:fileList.ToArray() $output $cmbBitrate.SelectedItem $txtTitle.Text.Trim() $txtAuthor.Text.Trim()
})

# ---------------------------------------------------------------------------
# Run
# ---------------------------------------------------------------------------
$form.Add_Shown({ $form.Activate() })
[System.Windows.Forms.Application]::Run($form)
