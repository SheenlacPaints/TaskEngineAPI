using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using TaskEngineAPI.WebSockets;
using TaskEngineAPI.Services;
using TaskEngineAPI.Repositories;
using TaskEngineAPI.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using System.Text;

namespace TaskEngineAPI.Helpers;
public static class WebSocketExtensions
{
    public static IServiceCollection AddWebSocketServices(this IServiceCollection services)
    {
        services.AddSingleton<WebSocketConnectionManager>();
        services.AddScoped<ProjectSocketHandler>();
        services.AddScoped<IWorkflowService, WorkflowService>();
        services.AddScoped<IWorkflowRepository, WorkflowRepository>();

        return services;
    }

    //public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    //{
    //    services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    //        .AddJwtBearer(options =>
    //        {
    //            options.Authority = configuration["Jwt:Authority"];
    //            options.Audience = configuration["Jwt:Audience"];
    //            options.TokenValidationParameters = new TokenValidationParameters
    //            {
    //                ValidateIssuer = true,
    //                ValidateAudience = true,
    //                ValidateLifetime = true,
    //                ValidateIssuerSigningKey = true
    //            };

    //            // Handle WebSocket token from query string
    //            options.Events = new JwtBearerEvents
    //            {
    //                OnMessageReceived = context =>
    //                {
    //                    var accessToken = context.Request.Query["access_token"];
    //                    var path = context.HttpContext.Request.Path;

    //                    if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/ws"))
    //                    {
    //                        context.Token = accessToken;
    //                    }
    //                    return Task.CompletedTask;
    //                }
    //            };
    //        });

    //    services.AddAuthorization();
    //    return services;
    //}


    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var issuer = configuration["Jwt:Issuer"];
        var audience = configuration["Jwt:Audience"];
        var securityKey = configuration["Jwt:Key"];

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = issuer,
                    ValidateAudience = true,
                    ValidAudience = audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(securityKey)),                
                    ClockSkew = TimeSpan.Zero
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;

                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/ws"))
                        {
                            context.Token = accessToken;
                        }
                        return Task.CompletedTask;
                    },
                    // 3. ADD THIS FOR DEBUGGING: It will print the exact reason for failure in your console
                    OnAuthenticationFailed = context =>
                    {
                        Console.WriteLine("Authentication failed: " + context.Exception.Message);
                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization();
        return services;
    }


    //public static IApplicationBuilder UseWebSocketEndpoints(this IApplicationBuilder app)
    //{
    //    // 1. Enable WebSocket support
    //    app.UseWebSockets(new WebSocketOptions
    //    {
    //        KeepAliveInterval = TimeSpan.FromMinutes(2)
    //    });

    //    // 2. Use Middleware to handle the specific route
    //    app.Use(async (context, next) =>
    //    {
    //        if (context.Request.Path == "/ws/project")
    //        {
    //            if (context.WebSockets.IsWebSocketRequest)
    //            {
    //                // Resolve the scoped handler from RequestServices
    //                var handler = context.RequestServices.GetRequiredService<ProjectSocketHandler>();
    //                await handler.HandleAsync(context);
    //            }
    //            else
    //            {
    //                context.Response.StatusCode = StatusCodes.Status400BadRequest;
    //            }
    //        }
    //        else
    //        {
    //            await next();
    //        }
    //    });

    //    return app;

    //}

    public static IApplicationBuilder UseWebSocketEndpoints(this IApplicationBuilder app)
    {
        // 1. Enable WebSocket support
        app.UseWebSockets(new WebSocketOptions
        {
            KeepAliveInterval = TimeSpan.FromMinutes(2)
        });

        // 2. Diagnostic Middleware
        app.Use(async (context, next) =>
        {
            if (context.Request.Path == "/ws/project")
            {
                // --- DIAGNOSTICS: Check your Console window for these ---
                Console.WriteLine("========================================");
                Console.WriteLine($"WebSocket Connection Attempt: {DateTime.Now}");
                Console.WriteLine($"Is WebSocket: {context.WebSockets.IsWebSocketRequest}");
                Console.WriteLine($"User Authenticated: {context.User.Identity?.IsAuthenticated}");

                if (context.User.Identity?.IsAuthenticated == true)
                {
                    Console.WriteLine($"User Name: {context.User.Identity.Name}");
                    foreach (var claim in context.User.Claims)
                    {
                        Console.WriteLine($"Claim: {claim.Type} = {claim.Value}");
                    }
                }
                Console.WriteLine("========================================");

                if (context.WebSockets.IsWebSocketRequest)
                {
                    var handler = context.RequestServices.GetRequiredService<ProjectSocketHandler>();
                    await handler.HandleAsync(context);
                }
                else
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                }
            }
            else
            {
                await next();
            }
        });

        return app;
    }



}