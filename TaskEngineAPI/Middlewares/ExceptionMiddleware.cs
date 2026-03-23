using System.Net;
using TaskEngineAPI.Services;
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
            try
            {
                await _next(httpContext);
            }
            catch (Exception ex)
            {
                var requestId = Guid.NewGuid();

                _logger.LogError(ex,
                    "Error | Path: {Path} | Method: {Method} | User: {User} | RequestId: {RequestId}",
                    httpContext.Request.Path,
                    httpContext.Request.Method,
                    httpContext.User?.Identity?.Name ?? "Anonymous",
                    requestId
                );

                var tenantId = httpContext.Items.ContainsKey("TenantID") ? httpContext.Items["TenantID"] as int? : null;
                var userId = httpContext.Items.ContainsKey("UserID") ? httpContext.Items["UserID"] as int? : null;

                try
                {
                    Exceptionlog.LogException(
                        message: ex.Message,
                        docType: "GlobalMiddleware",
                        ex: ex,
                        tenantId: tenantId,
                        userId: userId,
                        requestId: requestId
                    );
                }
                catch (Exception logEx)
                {
                    _logger.LogError(logEx, "Error while saving exception log");
                }

                await HandleExceptionAsync(httpContext, ex, requestId);
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
