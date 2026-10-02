using CashClarity.Api.Domain;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace CashClarity.Api;

/// <summary>
/// Single place deciding how domain failures map to HTTP responses.
/// </summary>
public class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public static (int Status, string Message) Map(Exception exception) => exception switch
    {
        NotFoundException e => (StatusCodes.Status404NotFound, e.Message),
        InvalidOperationException e => (StatusCodes.Status400BadRequest, e.Message),
        FormatException => (StatusCodes.Status400BadRequest, "Malformed identifier or value"),
        _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred"),
    };

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, message) = Map(exception);
        if (status >= 500)
            logger.LogError(exception, "Unhandled exception for {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        else
            logger.LogInformation("Request failed with {Status}: {Message}", status, exception.Message);

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(new { error = message }, cancellationToken);
        return true;
    }
}
