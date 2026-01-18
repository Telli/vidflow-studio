using System.Diagnostics;

namespace VidFlow.Api.Features.Render;

public class FfmpegVideoRenderer
{
    public static bool IsAvailable()
    {
        try
        {
            using var p = new Process();
            p.StartInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            p.StartInfo.ArgumentList.Add("-version");
            p.Start();
            if (!p.WaitForExit(1500))
            {
                try { p.Kill(entireProcessTree: true); } catch { }
                return false;
            }
            return p.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    public async Task RenderPlaceholderMp4Async(
        string outputFilePath,
        string label,
        int durationSeconds,
        int width,
        int height,
        CancellationToken ct)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(outputFilePath) ?? ".");

        var safeLabel = (label ?? string.Empty)
            .Replace("\\", "\\\\")
            .Replace("'", "\\'")
            .Replace(":", "\\:")
            .Replace("\n", " ")
            .Replace("\r", " ");

        var filter = $"drawtext=text='{safeLabel}':fontcolor=white:fontsize=48:x=(w-text_w)/2:y=(h-text_h)/2";
        var size = $"{width}x{height}";

        using var p = new Process();
        p.StartInfo = new ProcessStartInfo
        {
            FileName = "ffmpeg",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        p.StartInfo.ArgumentList.Add("-y");
        p.StartInfo.ArgumentList.Add("-f");
        p.StartInfo.ArgumentList.Add("lavfi");
        p.StartInfo.ArgumentList.Add("-i");
        p.StartInfo.ArgumentList.Add($"color=c=black:s={size}:d={Math.Max(1, durationSeconds)}");
        p.StartInfo.ArgumentList.Add("-vf");
        p.StartInfo.ArgumentList.Add(filter);
        p.StartInfo.ArgumentList.Add("-pix_fmt");
        p.StartInfo.ArgumentList.Add("yuv420p");
        p.StartInfo.ArgumentList.Add(outputFilePath);

        p.Start();

        await p.WaitForExitAsync(ct);

        if (p.ExitCode != 0)
        {
            var stderr = await p.StandardError.ReadToEndAsync(ct);
            throw new InvalidOperationException($"ffmpeg failed with exit code {p.ExitCode}: {stderr}");
        }

        var info = new FileInfo(outputFilePath);
        if (!info.Exists || info.Length == 0)
        {
            throw new InvalidOperationException("Render produced no output file");
        }
    }
}
