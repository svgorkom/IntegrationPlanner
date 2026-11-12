using IntegrationPlanner.UI.Localization;
using IntegrationPlanner.UI.Services;
using Microsoft.Extensions.Logging;

namespace IntegrationPlanner.UI.Diagnostics;

public sealed class UnhandledExceptionHandler
{
    private readonly ILogger<UnhandledExceptionHandler> _logger;
    private readonly IMessageService _messages;

    public UnhandledExceptionHandler(ILogger<UnhandledExceptionHandler> logger, IMessageService messages)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(messages);
        _logger = logger;
        _messages = messages;
    }

    public void HandleOnUiThread(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        _logger.LogCritical(exception, "{Handler} caught an unhandled exception on the UI thread", nameof(HandleOnUiThread));
        _messages.ShowError(UiText.ErrorUnexpected);
    }

    public void HandleTerminating(object? exceptionObject)
    {
        _logger.LogCritical(
            exceptionObject as Exception,
            "{Handler} caught an unhandled exception that terminates the application: {Type}",
            nameof(HandleTerminating),
            exceptionObject?.GetType().FullName);
    }

    public void HandleUnobservedTask(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        _logger.LogError(exception, "{Handler} caught an unobserved task exception", nameof(HandleUnobservedTask));
    }
}
