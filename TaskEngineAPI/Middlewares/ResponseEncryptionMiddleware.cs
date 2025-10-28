using Newtonsoft.Json;
using System.Text;
using TaskEngineAPI.Helpers;

namespace TaskEngineAPI.Middlewares
{
    public class ResponseEncryptionMiddleware
    {
        private readonly RequestDelegate _next;

        public ResponseEncryptionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var originalBodyStream = context.Response.Body;

            try
            {
                using var memoryStream = new MemoryStream();
                context.Response.Body = memoryStream;

                // Call the next middleware
                await _next(context);

                // Reset stream to read response
                memoryStream.Seek(0, SeekOrigin.Begin);
                var responseBody = await new StreamReader(memoryStream).ReadToEndAsync();

                // Encrypt response if not empty
                if (!string.IsNullOrEmpty(responseBody))
                {
                    var encrypted = AesEncryption.Encrypt(responseBody);
                    context.Response.ContentType = "application/json";
                    context.Response.ContentLength = Encoding.UTF8.GetByteCount(encrypted);

                    memoryStream.SetLength(0); // clear memory stream
                    await context.Response.WriteAsync(encrypted);
                }

                memoryStream.Seek(0, SeekOrigin.Begin);
                await memoryStream.CopyToAsync(originalBodyStream);
            }
            catch (Exception ex)
            {
                // Handle exceptions globally
                context.Response.StatusCode = 500;
                context.Response.ContentType = "application/json";

                var errorResponse = new
                {
                    success = false,
                    statusCode = 500,
                    message = ex.Message
                };

                var json = JsonConvert.SerializeObject(errorResponse);
                var encrypted = AesEncryption.Encrypt(json);

                await originalBodyStream.WriteAsync(Encoding.UTF8.GetBytes(encrypted));
            }
            finally
            {
                context.Response.Body = originalBodyStream;
            }
        }
    }

    // Extension method to register middleware
    public static class ResponseEncryptionMiddlewareExtensions
    {
        public static IApplicationBuilder UseResponseEncryption(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ResponseEncryptionMiddleware>();
        }
    }
}
