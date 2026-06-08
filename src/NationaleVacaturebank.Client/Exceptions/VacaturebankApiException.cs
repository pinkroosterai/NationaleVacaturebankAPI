namespace NationaleVacaturebank.Client.Exceptions;

public sealed class VacaturebankApiException : Exception
{
    public int StatusCode { get; }
    public string RequestUrl { get; }

    public VacaturebankApiException(int statusCode, string requestUrl, string? message = null)
        : base(message ?? $"API request to '{requestUrl}' failed with status {statusCode}.")
    {
        StatusCode = statusCode;
        RequestUrl = requestUrl;
    }
}
