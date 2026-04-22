using FluentValidation;
using HoneyDo.Common.Exceptions;
using System.Text.Json;

namespace HoneyDo.Common.Middleware;

public class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IHostEnvironment env)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, title, errors) = exception switch
        {
            NotFoundException => (404, "Not Found", (object?)null),
            UnauthorizedException => (401, "Unauthorized", (object?)null),
            ForbiddenException => (403, "Forbidden", (object?)null),
            ValidationException vex => (400, "Validation Failed", vex.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())),
            _ => (500, env.IsDevelopment() ? $"{exception.GetType().Name}: {exception.Message}" : "An unexpected error occurred.", (object?)null)
        };

        if (statusCode == 500)
            logger.LogError(exception, "Unhandled exception: {Title}", title);
        else
            logger.LogInformation(exception, "Handled exception: {StatusCode} {Title}", statusCode, title);

        if (context.Response.HasStarted)
        {
            logger.LogWarning("Response already started — cannot write error response for: {Title}", title);
            return;
        }

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var response = new { title, errors, traceId = context.TraceIdentifier };
        await context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));
    }
}
