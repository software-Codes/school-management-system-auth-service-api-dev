using System.Net;
using System.Text.Json;
using AuthService.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Infrastructure.Middleware;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionHandler(
        ILogger<GlobalExceptionHandler> logger,
        IHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(
            exception,
            "Exception occurred: {Message} | Path: {Path} | Method: {Method} | TraceId: {TraceId}",
            exception.Message,
            httpContext.Request.Path,
            httpContext.Request.Method,
            httpContext.TraceIdentifier);

        var problemDetails = CreateProblemDetails(exception, httpContext);

        httpContext.Response.StatusCode = problemDetails.Status ?? (int)HttpStatusCode.InternalServerError;
        httpContext.Response.ContentType = "application/problem+json";

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true; // Exception handled
    }

    private ProblemDetails CreateProblemDetails(Exception exception, HttpContext context)
    {
        var (status, title, type) = MapExceptionToStatusCode(exception);

        var problemDetails = new ProblemDetails
        {
            Status = status,
            Title = title,
            Type = type,
            Instance = context.Request.Path,
            Detail = GetErrorDetail(exception),
            Extensions =
            {
                ["traceId"] = context.TraceIdentifier,
                ["timestamp"] = DateTime.UtcNow
            }
        };

        if (_environment.IsDevelopment())
        {
            problemDetails.Extensions["exceptionType"] = exception.GetType().Name;
            problemDetails.Extensions["stackTrace"] = exception.StackTrace;
            
            if (exception.InnerException != null)
            {
                problemDetails.Extensions["innerException"] = new
                {
                    message = exception.InnerException.Message,
                    type = exception.InnerException.GetType().Name
                };
            }
        }

        return problemDetails;
    }

    private (int status, string title, string type) MapExceptionToStatusCode(Exception exception)
    {
        return exception switch
        {
            // Domain exceptions (business logic violations)
            // Note: More specific exceptions must come before base classes
            InvalidUserStateException => (
                (int)HttpStatusCode.BadRequest,
                "Invalid User State",
                "https://tools.ietf.org/html/rfc7231#section-6.5.1"
            ),

            DomainException => (
                (int)HttpStatusCode.BadRequest,
                "Business Rule Violation",
                "https://tools.ietf.org/html/rfc7231#section-6.5.1"
            ),

            // Validation exceptions (ArgumentNullException is a subclass of ArgumentException)
            ArgumentException => (
                (int)HttpStatusCode.BadRequest,
                "Invalid Request",
                "https://tools.ietf.org/html/rfc7231#section-6.5.1"
            ),

            // Not found exceptions
            KeyNotFoundException => (
                (int)HttpStatusCode.NotFound,
                "Resource Not Found",
                "https://tools.ietf.org/html/rfc7231#section-6.5.4"
            ),

            // Unauthorized exceptions
            UnauthorizedAccessException => (
                (int)HttpStatusCode.Unauthorized,
                "Unauthorized",
                "https://tools.ietf.org/html/rfc7235#section-3.1"
            ),

            // Operation cancelled (client disconnected)
            OperationCanceledException or TaskCanceledException => (
                499, // Client Closed Request (non-standard but widely used)
                "Request Cancelled",
                "https://httpstatuses.com/499"
            ),

            // Timeout exceptions
            TimeoutException => (
                (int)HttpStatusCode.RequestTimeout,
                "Request Timeout",
                "https://tools.ietf.org/html/rfc7231#section-6.5.7"
            ),

            // Invalid operation
            InvalidOperationException => (
                (int)HttpStatusCode.Conflict,
                "Invalid Operation",
                "https://tools.ietf.org/html/rfc7231#section-6.5.8"
            ),

            // Default: Internal Server Error
            _ => (
                (int)HttpStatusCode.InternalServerError,
                "Internal Server Error",
                "https://tools.ietf.org/html/rfc7231#section-6.6.1"
            )
        };
    }

    private string GetErrorDetail(Exception exception)
    {
        // In production, return generic messages for security
        if (!_environment.IsDevelopment())
        {
            return exception switch
            {
                DomainException or InvalidUserStateException => exception.Message,
                ArgumentException => exception.Message,
                _ => "An error occurred while processing your request. Please contact support if the issue persists."
            };
        }

        // In development, return actual exception message
        return exception.Message;
    }
}

