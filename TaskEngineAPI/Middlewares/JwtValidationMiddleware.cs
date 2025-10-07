using System.IdentityModel.Tokens.Jwt;

namespace TaskEngineAPI.Middlewares
{

    public class JwtValidationMiddleware
    {
        private readonly RequestDelegate _next;

        public JwtValidationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
            if (string.IsNullOrWhiteSpace(token))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Missing token");
                return;
            }

            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadToken(token) as JwtSecurityToken;

            var cTenantID = jsonToken?.Claims.FirstOrDefault(c => c.Type == "cTenantID")?.Value;
            var username = jsonToken?.Claims.FirstOrDefault(c => c.Type == "username")?.Value;

            if (string.IsNullOrWhiteSpace(cTenantID) || string.IsNullOrWhiteSpace(username))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Invalid token claims");
                return;
            }

            context.Items["cTenantID"] = int.Parse(cTenantID);
            context.Items["username"] = username;

            await _next(context);
        }
    }

}