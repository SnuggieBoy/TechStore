using Microsoft.AspNetCore.Http;
using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace TechStore.API.Middlewares
{
    /// <summary>
    /// Global exception handler middleware for consistent error responses.
    /// Catches all unhandled exceptions and returns proper JSON to mobile client.
    /// </summary>
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;
        private readonly IHostEnvironment _env;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger, IHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception occurred: {Path}", context.Request.Path);
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            var (statusCode, message) = exception switch
            {
                UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
                KeyNotFoundException => (StatusCodes.Status404NotFound, exception.Message),
                ArgumentException => (StatusCodes.Status400BadRequest, exception.Message),
                InvalidOperationException => (StatusCodes.Status400BadRequest, exception.Message),
                _ => (StatusCodes.Status500InternalServerError, "An internal server error occurred")
            };

            context.Response.StatusCode = statusCode;

            // In production, do not expose detailed 4xx messages (reduces enumeration / info leakage).
            var isDev = _env.IsDevelopment();
            var clientMessage = message;
            if (!isDev)
            {
                if (statusCode == StatusCodes.Status404NotFound) clientMessage = "Resource not found.";
                else if (statusCode == StatusCodes.Status400BadRequest) clientMessage = "Invalid request.";
                _logger.LogInformation("4xx response (detail not shown to client): {Detail}", exception.Message);
            }
            // For 500, hide internals in non-Dev (already done below via errorDetails).
            var errorDetails = statusCode >= 500 && !isDev
                ? new[] { "An unexpected error occurred. Please try again later." }
                : new[] { statusCode >= 500 ? exception.Message : clientMessage };

            var response = new
            {
                success = false,
                message = statusCode >= 500 ? message : clientMessage,
                data = (object?)null,
                errors = errorDetails
            };

            var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(json);
        }
    }
}
