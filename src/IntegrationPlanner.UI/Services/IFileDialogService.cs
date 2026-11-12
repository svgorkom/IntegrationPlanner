using IntegrationPlanner.UI.Export;

namespace IntegrationPlanner.UI.Services;

public interface IFileDialogService
{
    string? PickPlanToOpen();

    string? PickExportTarget(ExportFormat format, string suggestedFileName);
}
