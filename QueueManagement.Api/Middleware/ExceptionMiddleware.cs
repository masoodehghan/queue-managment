using System.Net;
using System.Text.Json;
using QueueManagement.Application.Common.Exceptions;

namespace QueueManagement.Api.Middleware;

public class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        
        var (statusCode, response) = exception switch
        {
            NotFoundException => (HttpStatusCode.NotFound, new { error = exception.Message }),
            UnauthorizedException => (HttpStatusCode.Unauthorized, new { error = exception.Message }),
            ValidationException => (HttpStatusCode.BadRequest, new { error = exception.Message }),
            _ => (HttpStatusCode.InternalServerError, new { error = "An internal server error occurred" })
        };

        context.Response.StatusCode = (int)statusCode;
        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}
