using SchoolAdmission.Api.Contracts;

namespace SchoolAdmission.Api.Infrastructure;

public class ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ApiException ex)
        {
            await WriteError(context, ex.StatusCode, ex.Code, ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled error while processing {Path}", context.Request.Path);
            await WriteError(context, StatusCodes.Status500InternalServerError, "INTERNAL_ERROR", "Unexpected server error.");
        }
    }

    private static Task WriteError(HttpContext context, int status, string code, string message)
    {
        if (context.Response.HasStarted)
        {
            return Task.CompletedTask;
        }

        context.Response.StatusCode = status;
        return context.Response.WriteAsJsonAsync(new ErrorResponse(code, message));
    }
}
