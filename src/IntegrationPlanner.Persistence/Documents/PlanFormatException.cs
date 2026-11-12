namespace IntegrationPlanner.Persistence.Documents;

internal sealed class PlanFormatException : Exception
{
    public PlanFormatException(string message)
        : base(message)
    {
    }
}
