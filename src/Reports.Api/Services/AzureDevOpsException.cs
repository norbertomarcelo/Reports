using System.Net;

namespace Reports.Api.Services;

public class AzureDevOpsException : Exception
{
    public HttpStatusCode StatusCode { get; }

    public AzureDevOpsException(string message, HttpStatusCode statusCode, Exception innerException)
        : base(message, innerException)
    {
        StatusCode = statusCode;
    }
}
