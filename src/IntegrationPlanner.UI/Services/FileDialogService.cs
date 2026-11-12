using IntegrationPlanner.UI.Export;
using IntegrationPlanner.UI.Localization;
using Microsoft.Win32;

namespace IntegrationPlanner.UI.Services;

public sealed class FileDialogService : IFileDialogService
{
    public string? PickPlanToOpen()
    {
        OpenFileDialog dialog = new()
        {
            Title = UiText.DialogOpenPlanTitle,
            Filter = UiText.DialogPlanFilter,
            CheckFileExists = true,
        };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public string? PickExportTarget(ExportFormat format, string suggestedFileName)
    {
        ArgumentNullException.ThrowIfNull(suggestedFileName);
        SaveFileDialog dialog = new()
        {
            Title = UiText.DialogExportTitle,
            Filter = format == ExportFormat.Png ? UiText.DialogPngFilter : UiText.DialogSvgFilter,
            FileName = suggestedFileName,
            OverwritePrompt = true,
        };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }
}
