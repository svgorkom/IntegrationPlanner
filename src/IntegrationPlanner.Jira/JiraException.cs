namespace IntegrationPlanner.Jira;

public enum JiraFailure
{
    Unreachable,
    Unauthorized,
    NotFound,
    InvalidRequest,
    ServerError,
    InvalidResponse,
}

public sealed class JiraException : Exception
{
    public JiraException(JiraFailure failure, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        Failure = failure;
    }

    public JiraFailure Failure { get; }
}
