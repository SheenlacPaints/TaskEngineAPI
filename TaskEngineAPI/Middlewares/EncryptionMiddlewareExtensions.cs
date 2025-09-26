using TaskEngineAPI.Middlewares;
using Microsoft.AspNetCore.Builder;

namespace TaskEngineAPI.Middlewares
{
    public static class EncryptionMiddlewareExtensions
    
    {
        public static IApplicationBuilder UseEncryptionMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<EncryptionMiddleware>();
        }
    }
}




