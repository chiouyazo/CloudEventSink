namespace CloudEventSink.Core.Query;

public sealed class QueryValidationException : Exception
{
    public QueryValidationException() { }

    public QueryValidationException(string message)
        : base(message) { }

    public QueryValidationException(string message, Exception innerException)
        : base(message, innerException) { }
}
