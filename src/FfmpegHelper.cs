using System.Diagnostics;
using System.Globalization;
using System.IO.Compression;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AudiobookMaker;

public record Chapter(string Title, double Start, double End);

public static class FfmpegHelper
{
    static readonly string AppDataDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AudiobookMaker");
    static readonly string FfmpegExe  = Path.Combine(AppDataDir, "ffmpeg.exe");
    static readonly string FfprobeExe = Path.Combine(AppDataDir, "ffprobe.exe");

    // -------------------------------------------------------------------------
    // Discovery
    // -------------------------------------------------------------------------

    public static string? FindFfmpeg()
    {
        string local = Path.Combine(AppContext.BaseDirectory, "ffmpeg.exe");
        if (File.Exists(local)) return local;
        if (File.Exists(FfmpegExe)) return FfmpegExe;
        return FindInPath("ffmpeg.exe");
    }

    public static string? FindFfprobe()
    {
        string? ffmpeg = FindFfmpeg();
        if (ffmpeg != null)
        {
            string probe = Path.Combine(Path.GetDirectoryName(ffmpeg)!, "ffprobe.exe");
            if (File.Exists(probe)) return probe;
        }
        string local = Path.Combine(AppContext.BaseDirectory, "ffprobe.exe");
        if (File.Exists(local)) return local;
        if (File.Exists(FfprobeExe)) return FfprobeExe;
        return FindInPath("ffprobe.exe");
    }

    static string? FindInPath(string exe)
    {
        string? pathVar = Environment.GetEnvironmentVariable("PATH");
        if (pathVar == null) return null;
        foreach (string dir in pathVar.Split(Path.PathSeparator))
        {
            try { string full = Path.Combine(dir, exe); if (File.Exists(full)) return full; }
            catch { }
        }
        return null;
    }

    // -------------------------------------------------------------------------
    // Download
    // -------------------------------------------------------------------------

    public static async Task<string?> DownloadFfmpegAsync(IProgress<string> log, CancellationToken ct)
    {
        Directory.CreateDirectory(AppDataDir);
        string zipPath = Path.Combine(AppDataDir, "ffmpeg.zip");
        string[] urls =
        [
            "https://github.com/BtbN/FFmpeg-Builds/releases/download/latest/ffmpeg-master-latest-win64-gpl.zip",
            "https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip"
        ];

        bool downloaded = false;
        using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
        foreach (string url in urls)
        {
            log.Report($"Downloading ffmpeg from:\n  {url}");
            try
            {
                await using var fs = File.Create(zipPath);
                await using var stream = await http.GetStreamAsync(url, ct);
                await stream.CopyToAsync(fs, ct);
                downloaded = true;
                break;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                log.Report($"  Failed: {ex.Message}");
            }
        }

        if (!downloaded)
        {
            log.Report("ERROR: Could not download ffmpeg. Place ffmpeg.exe next to AudiobookMaker.exe manually.");
            return null;
        }

        log.Report("Extracting...");
        await Task.Run(() => ZipFile.ExtractToDirectory(zipPath, AppDataDir, true), ct);

        string? foundFfmpeg = Directory.GetFiles(AppDataDir, "ffmpeg.exe", SearchOption.AllDirectories).FirstOrDefault();
        if (foundFfmpeg == null) { log.Report("ERROR: ffmpeg.exe not found in archive."); return null; }
        File.Copy(foundFfmpeg, FfmpegExe, true);

        string? foundFfprobe = Directory.GetFiles(AppDataDir, "ffprobe.exe", SearchOption.AllDirectories).FirstOrDefault();
        if (foundFfprobe != null) { File.Copy(foundFfprobe, FfprobeExe, true); log.Report("ffprobe ready."); }

        try { File.Delete(zipPath); } catch { }
        foreach (string item in Directory.GetFileSystemEntries(AppDataDir))
        {
            if (item == FfmpegExe || item == FfprobeExe) continue;
            try { if (Directory.Exists(item)) Directory.Delete(item, true); else File.Delete(item); } catch { }
        }

        log.Report("ffmpeg ready.\n");
        return FfmpegExe;
    }

    // -------------------------------------------------------------------------
    // Audio info
    // -------------------------------------------------------------------------

    public static async Task<double> GetAudioDurationAsync(string ffmpeg, string file, CancellationToken ct = default)
    {
        var psi = new ProcessStartInfo(ffmpeg, $"-i \"{file}\" -hide_banner")
            { UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true };
        using var proc = new Process { StartInfo = psi };
        proc.Start();
        string stderr;
        try
        {
            stderr = await proc.StandardError.ReadToEndAsync(ct);
            await proc.WaitForExitAsync(ct);
        }
        catch (OperationCanceledException) { try { proc.Kill(); } catch { } throw; }

        var m = Regex.Match(stderr, @"Duration:\s*(\d+):(\d+):([\d\.]+)");
        if (!m.Success) return 0;
        return int.Parse(m.Groups[1].Value) * 3600
             + int.Parse(m.Groups[2].Value) * 60
             + double.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture);
    }

    public static async Task<(string title, string artist)> ReadTagsAsync(string ffprobe, string file)
    {
        try
        {
            var psi = new ProcessStartInfo(ffprobe, $"-v quiet -print_format json -show_format \"{file}\"")
                { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true };
            using var proc = new Process { StartInfo = psi };
            proc.Start();
            string json = await proc.StandardOutput.ReadToEndAsync();
            await proc.WaitForExitAsync();

            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.GetProperty("format").TryGetProperty("tags", out var tags)) return ("", "");

            string title = "";
            string artist = "";
            if (tags.TryGetProperty("album", out var al))        title  = al.GetString() ?? "";
            if (string.IsNullOrEmpty(title)  && tags.TryGetProperty("title",        out var ti)) title  = ti.GetString() ?? "";
            if (tags.TryGetProperty("album_artist", out var aa)) artist = aa.GetString() ?? "";
            if (string.IsNullOrEmpty(artist) && tags.TryGetProperty("artist",       out var ar)) artist = ar.GetString() ?? "";
            return (title, artist);
        }
        catch { return ("", ""); }
    }

    public static async Task<(double totalDuration, (string Title, double Start, double End)[] chapters)>
        ReadChaptersAsync(string ffprobe, string file)
    {
        var psi = new ProcessStartInfo(ffprobe,
            $"-v quiet -print_format json -show_chapters -show_format \"{file}\"")
            { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true };
        using var proc = new Process { StartInfo = psi };
        proc.Start();
        string json = await proc.StandardOutput.ReadToEndAsync();
        await proc.WaitForExitAsync();

        using var doc = JsonDocument.Parse(json);
        double totalDur = 0;
        if (doc.RootElement.TryGetProperty("format", out var fmt) &&
            fmt.TryGetProperty("duration", out var durProp))
            totalDur = double.Parse(durProp.GetString()!, CultureInfo.InvariantCulture);

        if (!doc.RootElement.TryGetProperty("chapters", out var chArr) || chArr.GetArrayLength() == 0)
            return (totalDur, []);

        var chapters = chArr.EnumerateArray().Select((ch, idx) =>
        {
            double start = double.Parse(ch.GetProperty("start_time").GetString()!, CultureInfo.InvariantCulture);
            double end   = double.Parse(ch.GetProperty("end_time").GetString()!,   CultureInfo.InvariantCulture);
            if (end <= 0 || end > totalDur) end = totalDur;
            string title = ch.TryGetProperty("tags", out var tags) && tags.TryGetProperty("title", out var t)
                ? t.GetString() ?? $"Chapter {idx + 1}"
                : $"Chapter {idx + 1}";
            return (title, start, end);
        }).ToArray();

        return (totalDur, chapters);
    }

    // -------------------------------------------------------------------------
    // Cover art
    // -------------------------------------------------------------------------

    public static bool ExtractCover(string ffmpeg, string inputFile, string outPath)
    {
        var psi = new ProcessStartInfo(ffmpeg, $"-y -i \"{inputFile}\" -an -vcodec copy \"{outPath}\"")
            { UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true };
        using var proc = new Process { StartInfo = psi };
        proc.Start();
        proc.StandardError.ReadToEnd();
        proc.WaitForExit();
        return proc.ExitCode == 0 && File.Exists(outPath) && new FileInfo(outPath).Length > 0;
    }

    // -------------------------------------------------------------------------
    // Process runner
    // -------------------------------------------------------------------------

    public static async Task<(int exitCode, string stderr)> RunFfmpegAsync(
        string ffmpeg, string args, CancellationToken ct,
        IProgress<double>? encProgress = null, double totalSeconds = 0)
    {
        var psi = new ProcessStartInfo(ffmpeg, args)
            { UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true };
        using var proc = new Process { StartInfo = psi, EnableRaisingEvents = true };
        proc.Start();

        if (encProgress != null && totalSeconds > 0)
        {
            var stderrLines = new System.Collections.Concurrent.ConcurrentQueue<string>();
            proc.ErrorDataReceived += (_, e) =>
            {
                if (e.Data == null) return;
                stderrLines.Enqueue(e.Data);
                var m = Regex.Match(e.Data, @"time=(\d+):(\d+):([\d\.]+)");
                if (!m.Success) return;
                double t = int.Parse(m.Groups[1].Value) * 3600
                         + int.Parse(m.Groups[2].Value) * 60
                         + double.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture);
                encProgress.Report(Math.Min(1.0, t / totalSeconds));
            };
            proc.BeginErrorReadLine();
            try
            {
                await proc.WaitForExitAsync(ct);
            }
            catch (OperationCanceledException)
            {
                try { proc.Kill(true); } catch { }
                proc.WaitForExit(3000);
                throw;
            }
            return (proc.ExitCode, string.Join("\n", stderrLines));
        }
        else
        {
            string stderr;
            try
            {
                stderr = await proc.StandardError.ReadToEndAsync(ct);
                await proc.WaitForExitAsync(ct);
            }
            catch (OperationCanceledException) { try { proc.Kill(); } catch { } throw; }
            return (proc.ExitCode, stderr);
        }
    }

    // -------------------------------------------------------------------------
    // Metadata
    // -------------------------------------------------------------------------

    public static void BuildMetadataFile(IEnumerable<Chapter> chapters, string outPath,
        string title, string author, string series)
    {
        var lines = new List<string> { ";FFMETADATA1" };
        if (!string.IsNullOrEmpty(title))  lines.Add($"title={title}");
        if (!string.IsNullOrEmpty(author)) { lines.Add($"artist={author}"); lines.Add($"album_artist={author}"); }
        string albumName = !string.IsNullOrEmpty(series) && !string.IsNullOrEmpty(title)
            ? $"{series} - {title}" : title;
        if (!string.IsNullOrEmpty(albumName)) lines.Add($"album={albumName}");
        if (!string.IsNullOrEmpty(series))    lines.Add($"grouping={series}");
        lines.Add("genre=Audiobook");
        lines.Add("");
        foreach (var ch in chapters)
        {
            lines.AddRange([
                "[CHAPTER]", "TIMEBASE=1/1000",
                $"START={(long)(ch.Start * 1000)}", $"END={(long)(ch.End * 1000)}",
                $"title={ch.Title}", ""
            ]);
        }
        File.WriteAllLines(outPath, lines, new UTF8Encoding(false));
    }

    // -------------------------------------------------------------------------
    // File helpers
    // -------------------------------------------------------------------------

    public static string[] GetAudioFiles(string folder)
    {
        string[] exts = ["*.mp3", "*.m4a", "*.m4b", "*.flac", "*.ogg", "*.wma", "*.aac", "*.opus"];
        return exts.SelectMany(ext => Directory.GetFiles(folder, ext, SearchOption.TopDirectoryOnly)).ToArray();
    }

    public static string FormatTimestamp(double seconds)
    {
        int h = (int)(seconds / 3600), m = (int)((seconds % 3600) / 60), s = (int)(seconds % 60);
        return $"{h}:{m:D2}:{s:D2}";
    }

    public static double ParseTimestamp(string input)
    {
        string s = input.Trim();
        var m1 = Regex.Match(s, @"^(\d+):(\d+):(\d+(?:\.\d+)?)$");
        if (m1.Success)
            return int.Parse(m1.Groups[1].Value) * 3600 + int.Parse(m1.Groups[2].Value) * 60
                 + double.Parse(m1.Groups[3].Value, CultureInfo.InvariantCulture);
        var m2 = Regex.Match(s, @"^(\d+):(\d+(?:\.\d+)?)$");
        if (m2.Success)
            return int.Parse(m2.Groups[1].Value) * 60
                 + double.Parse(m2.Groups[2].Value, CultureInfo.InvariantCulture);
        if (Regex.IsMatch(s, @"^[\d\.]+$"))
            return double.Parse(s, CultureInfo.InvariantCulture);
        throw new ArgumentException($"Invalid timestamp '{s}' — use H:MM:SS or MM:SS");
    }
}
