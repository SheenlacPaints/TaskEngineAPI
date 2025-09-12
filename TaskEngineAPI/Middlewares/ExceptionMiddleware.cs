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
                _logger.LogError(ex, "Unhandled exception occurred");

                var tenantId = httpContext.Items.ContainsKey("TenantID") ? httpContext.Items["TenantID"] as int? : null;
                var userId = httpContext.Items.ContainsKey("UserID") ? httpContext.Items["UserID"] as int? : null;
                var requestId = Guid.NewGuid(); // Or read from X-Correlation-ID header if present
               
                Exceptionlog.LogException(
                    message: ex.Message,
                    docType: "GlobalMiddleware",
                    ex: ex,
                    tenantId: tenantId,
                    userId: userId,
                    requestId: requestId
                );
                await HandleExceptionAsync(httpContext, ex);
            }
        }
        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            var statusCode = (int)HttpStatusCode.InternalServerError;

            if (exception is UnauthorizedAccessException)
            {
                statusCode = (int)HttpStatusCode.Unauthorized;
            }
            else if (exception is ArgumentException)
            {
                statusCode = (int)HttpStatusCode.BadRequest;
            }

            context.Response.StatusCode = statusCode;

            var response = new
            {
                success = false,
                statusCode = statusCode,
                message = exception.Message,
                requestId = Guid.NewGuid()
                //  details = exception.StackTrace 
            };
            return context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
        }

    }
}
