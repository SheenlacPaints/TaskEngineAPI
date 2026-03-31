using System.Net;
using TaskEngineAPI.Services;
using Serilog.Context;

namespace TaskEngineAPI.Middlewares
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var requestGuid = Guid.NewGuid();
            var requestId = requestGuid.ToString("N").Substring(0, 8);

            var user = httpContext.User?.Identity?.Name ?? "Anonymous";
            var path = $"{httpContext.Request.Path}{httpContext.Request.QueryString}";

            using (LogContext.PushProperty("RequestId", requestId))
            using (LogContext.PushProperty("User", user))
            {
                try
                {
                    await _next(httpContext);

                    stopwatch.Stop();

                    _logger.LogInformation(
                        "{Method} {Path} → {StatusCode} ({Elapsed} ms)",
                        httpContext.Request.Method,
                        path,
                        httpContext.Response.StatusCode,
                        stopwatch.ElapsedMilliseconds
                    );
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();

                    var tenantId = httpContext.Items.ContainsKey("TenantID") ? httpContext.Items["TenantID"] as int? : null;
                    var userId = httpContext.Items.ContainsKey("UserID") ? httpContext.Items["UserID"] as int? : null;

                    _logger.LogError(ex,
                        "{Method} {Path} → {StatusCode} ({Elapsed} ms) | Tenant: {TenantId} | UserId: {UserId}",
                        httpContext.Request.Method,
                        path,
                        (int)HttpStatusCode.InternalServerError,
                        stopwatch.ElapsedMilliseconds,
                        tenantId,
                        userId
                    );

                    try
                    {
                        Exceptionlog.LogException(
                            message: ex.Message,
                            docType: "GlobalMiddleware",
                            ex: ex,
                            tenantId: tenantId,
                            userId: userId,
                            requestId: requestGuid
                        );
                    }
                    catch (Exception logEx)
                    {
                        _logger.LogError(logEx, "Error while saving exception log");
                    }

                    await HandleExceptionAsync(httpContext, ex, requestGuid);
                }
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception, Guid requestId)
        {
            context.Response.ContentType = "application/json";

            var statusCode = exception switch
            {
                UnauthorizedAccessException => (int)HttpStatusCode.Unauthorized,
                ArgumentException => (int)HttpStatusCode.BadRequest,
                _ => (int)HttpStatusCode.InternalServerError
            };

            context.Response.StatusCode = statusCode;

            var response = new
            {
                success = false,
                statusCode = statusCode,
                message = "Something went wrong. Please contact support.",
                requestId = requestId
            };

            return context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
        }
    }
}