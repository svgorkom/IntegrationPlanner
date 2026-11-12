using IntegrationPlanner.Layout.Scene;

namespace IntegrationPlanner.UI.Export;

public interface ISceneExporter
{
    Task ExportAsync(DiagramScene scene, ExportFormat format, string path, CancellationToken cancellationToken);

    void CopyToClipboard(DiagramScene scene);
}
