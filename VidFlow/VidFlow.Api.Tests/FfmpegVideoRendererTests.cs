using VidFlow.Api.Features.Render;
using Xunit;

namespace VidFlow.Api.Tests;

public class FfmpegVideoRendererTests
{
    [Fact]
    public async Task RenderPlaceholderMp4Async_WritesNonEmptyMp4_WhenFfmpegAvailable()
    {
        if (!FfmpegVideoRenderer.IsAvailable())
            return;

        var tmpDir = Path.Combine(Path.GetTempPath(), "vidflow-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tmpDir);

        var outputPath = Path.Combine(tmpDir, "placeholder.mp4");

        var renderer = new FfmpegVideoRenderer();
        await renderer.RenderPlaceholderMp4Async(outputPath, "VidFlow Test", 1, 640, 360, CancellationToken.None);

        var info = new FileInfo(outputPath);
        Assert.True(info.Exists);
        Assert.True(info.Length > 0);
    }
}
