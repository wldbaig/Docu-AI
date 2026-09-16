using DocuChat.Application;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace DocuChat.Api;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title) = exception switch
        {
            ValidationException => (400, "Validation failed"),
            UnauthorizedAccessException => (401, "Unauthorized"),
            NotFoundException => (404, "Not found"),
            ConflictException => (409, "Conflict"),
            ExternalServiceException => (502, "AI service error"),
            _ => (500, "Unexpected error")
        };
        if (status >= 500) logger.LogError(exception, "Request failed with status {StatusCode}", status);
        else logger.LogWarning(exception, "Request rejected with status {StatusCode}", status);
        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = status, Title = title, Detail = status == 500 ? "An unexpected error occurred." : exception.Message, Instance = context.Request.Path
        }, cancellationToken);
        return true;
    }
}
