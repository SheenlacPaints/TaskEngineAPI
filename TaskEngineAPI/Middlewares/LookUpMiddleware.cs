using System.Net;
using System.Text.Json;
using TaskEngineAPI.Helpers;

namespace TaskEngineAPI.Middleware
{
    public class LookUpMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<LookUpMiddleware> _logger;
        private readonly IWebHostEnvironment _env;

        public LookUpMiddleware(RequestDelegate next, ILogger<LookUpMiddleware> logger, IWebHostEnvironment env)
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
                _logger.LogError(ex, "An unhandled exception occurred: {Message}", ex.Message);
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var (statusCode, errorCode, message) = GetExceptionDetails(exception);

            var errorResponse = new
            {
                body = Array.Empty<object>(),
                statusText = message,
                status = statusCode,
                error = errorCode,
                path = context.Request.Path,
                timestamp = DateTime.Now,
                requestId = context.TraceIdentifier
            };

            if (statusCode == 500)
            {
                _logger.LogError(exception, "Internal Server Error: {Message}", exception.Message);
            }

            var jsonResponse = JsonSerializer.Serialize(errorResponse);
            var encryptedResponse = AesEncryption.Encrypt(jsonResponse);

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = statusCode;
            await context.Response.WriteAsync(encryptedResponse);
        }

        private (int statusCode, string errorCode, string message) GetExceptionDetails(Exception exception)
        {
            return exception switch
            {
                KeyNotFoundException => (404, "NOT_FOUND", "The requested resource was not found."),
                UnauthorizedAccessException => (401, "UNAUTHORIZED", "Access denied. Please authenticate."),
                ArgumentException => (400, "BAD_REQUEST", "Invalid request parameters."),
                InvalidOperationException => (400, "INVALID_OPERATION", "The operation cannot be performed."),
                TimeoutException => (408, "TIMEOUT", "The request timed out."),
                NotImplementedException => (501, "NOT_IMPLEMENTED", "This feature is not implemented."),

                System.Data.SqlClient.SqlException sqlEx => HandleSqlException(sqlEx),

                JsonException => (400, "INVALID_JSON", "Invalid JSON format in request."),

                System.Security.Cryptography.CryptographicException => (400, "ENCRYPTION_ERROR", "Encryption/decryption error."),

                _ => (500, "INTERNAL_ERROR", _env.IsDevelopment()
                    ? $"Internal server error: {exception.Message}"
                    : "An internal server error occurred. Please try again later.")
            };
        }

        private (int statusCode, string errorCode, string message) HandleSqlException(System.Data.SqlClient.SqlException sqlEx)
        {
            return sqlEx.Number switch
            {
                2627 => (409, "DUPLICATE_ENTRY", "A record with this information already exists."),

                547 => (409, "REFERENCE_CONSTRAINT", "This record cannot be deleted because it is referenced by other records."),

                2601 => (409, "DUPLICATE_KEY", "Duplicate key value violates unique constraint."),

                -2 => (408, "DATABASE_TIMEOUT", "Database operation timed out."),

                1205 => (409, "DEADLOCK", "Database deadlock occurred. Please retry the operation."),

                53 or 121 => (503, "DATABASE_UNAVAILABLE", "Database is temporarily unavailable."),

                _ => (500, "DATABASE_ERROR", "A database error occurred.")
            };
        }
    }

    public static class LookUpMiddlewareExtensions
    {
        public static IApplicationBuilder UseLookUpMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<LookUpMiddleware>();
        }
    }
}