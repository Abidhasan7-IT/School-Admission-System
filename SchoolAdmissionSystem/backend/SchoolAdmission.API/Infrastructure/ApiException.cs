namespace SchoolAdmission.Api.Infrastructure;

/// <summary>An expected, client-facing error. Converted to {code, message} JSON by the middleware.</summary>
public class ApiException(int statusCode, string code, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
    public string Code { get; } = code;
}
