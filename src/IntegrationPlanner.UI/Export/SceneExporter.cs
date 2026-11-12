using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using IntegrationPlanner.Layout.Scene;
using IntegrationPlanner.UI.Rendering;
using Microsoft.Extensions.Logging;

namespace IntegrationPlanner.UI.Export;

public sealed class SceneExporter : ISceneExporter
{
    public const double BitmapScale = 2.0;

    private const double ScreenDpi = 96.0;

    private readonly ScenePalette _palette;
    private readonly SvgSceneWriter _svgWriter;
    private readonly ILogger<SceneExporter> _logger;

    public SceneExporter(ScenePalette palette, SvgSceneWriter svgWriter, ILogger<SceneExporter> logger)
    {
        ArgumentNullException.ThrowIfNull(palette);
        ArgumentNullException.ThrowIfNull(svgWriter);
        ArgumentNullException.ThrowIfNull(logger);
        _palette = palette;
        _svgWriter = svgWriter;
        _logger = logger;
    }

    public async Task ExportAsync(DiagramScene scene, ExportFormat format, string path, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(scene);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        // Rendering needs the UI thread; only the file write is asynchronous.
        byte[] content = format switch
        {
            ExportFormat.Png => EncodePng(RenderBitmap(scene)),
            ExportFormat.Svg => Encoding.UTF8.GetBytes(_svgWriter.Write(scene)),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, null),
        };
        await WriteAtomicallyAsync(path, content, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("{Operation} wrote a {Format} export", nameof(ExportAsync), format);
    }

    public void CopyToClipboard(DiagramScene scene)
    {
        ArgumentNullException.ThrowIfNull(scene);
        Clipboard.SetImage(RenderBitmap(scene));
        _logger.LogInformation("{Operation} completed", nameof(CopyToClipboard));
    }

    public BitmapSource RenderBitmap(DiagramScene scene)
    {
        ArgumentNullException.ThrowIfNull(scene);
        SceneRect bounds = scene.Bounds;
        DrawingVisual visual = new();
        using (DrawingContext context = visual.RenderOpen())
        {
            context.PushTransform(new ScaleTransform(BitmapScale, BitmapScale));
            context.PushTransform(new TranslateTransform(-bounds.Left, -bounds.Top));
            context.DrawDrawing(SceneDrawingBuilder.Build(scene, _palette, BitmapScale));
        }

        RenderTargetBitmap bitmap = new(
            (int)Math.Ceiling(bounds.Width * BitmapScale),
            (int)Math.Ceiling(bounds.Height * BitmapScale),
            ScreenDpi,
            ScreenDpi,
            PixelFormats.Pbgra32);
        bitmap.Render(visual);
        bitmap.Freeze();
        return bitmap;
    }

    private static byte[] EncodePng(BitmapSource bitmap)
    {
        PngBitmapEncoder encoder = new();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using MemoryStream stream = new();
        encoder.Save(stream);
        return stream.ToArray();
    }

    private async Task WriteAtomicallyAsync(string path, byte[] content, CancellationToken cancellationToken)
    {
        string fullPath = Path.GetFullPath(path);
        string directory = Path.GetDirectoryName(fullPath)
            ?? throw new ArgumentException($"'{path}' has no parent directory.", nameof(path));
        string tempPath = Path.Combine(directory, $"{Path.GetFileName(fullPath)}.{Guid.NewGuid():N}.tmp");
        bool replaced = false;
        try
        {
            await File.WriteAllBytesAsync(tempPath, content, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            File.Move(tempPath, fullPath, overwrite: true);
            replaced = true;
        }
        finally
        {
            if (!replaced)
            {
                DeleteTempFile(tempPath);
            }
        }
    }

    private void DeleteTempFile(string tempPath)
    {
        try
        {
            File.Delete(tempPath);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // The export failure is what the caller reports; a leftover temp file is only logged.
            _logger.LogWarning(exception, "{Operation} could not remove its temporary file", nameof(WriteAtomicallyAsync));
        }
    }
}
