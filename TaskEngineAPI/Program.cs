using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Data.SqlClient;
using System.Data;
using System.Text;
using TaskEngineAPI.Interfaces;
using TaskEngineAPI.Middleware;
using TaskEngineAPI.Middlewares;
using TaskEngineAPI.Repositories;
using TaskEngineAPI.Services;
using Serilog;
using Serilog.Enrichers;
using TaskEngineAPI.Helpers;
using TaskEngineAPI.WebSockets;
using static System.Net.WebRequestMethods;
using TaskEngineAPI.Models;


var builder = WebApplication.CreateBuilder(args);
//Log.Logger = new LoggerConfiguration()
//    .MinimumLevel.Information()
//    .WriteTo.Console()
//    .WriteTo.File(
//        "Logs/api-log-.txt",
//        rollingInterval: RollingInterval.Day,
//        retainedFileCountLimit: 30,     // keep 30 days logs
//        fileSizeLimitBytes: 10_000_000, // 10 MB per file
//        rollOnFileSizeLimit: true
//    )
//    .CreateLogger();
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .WriteTo.Console()
    .WriteTo.File(
        "Logs/api-log-.txt",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        fileSizeLimitBytes: 10_000_000,
        rollOnFileSizeLimit: true,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] [{MachineName}] {Message:lj} {Properties:j}{NewLine}{Exception}"
    )
    .CreateLogger();


builder.Host.UseSerilog();

//builder.Services.AddScoped<IRoleService, RoleService>();
//builder.Services.AddDbContext<ApplicationDbContext>(options =>
//   options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddMemoryCache();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null; // Keeps PascalCase
        options.JsonSerializerOptions.DefaultIgnoreCondition =
            System.Text.Json.Serialization.JsonIgnoreCondition.Never;
    });
builder.Services.AddAuthentication(options =>
{
    // 👤 Default → USER TOKEN
    options.DefaultAuthenticateScheme = "UserScheme";
    options.DefaultChallengeScheme = "UserScheme";
})
// ================= USER JWT =================
.AddJwtBearer("UserScheme", o =>
{
    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])
        ),
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = false, // change to true in production
        ValidateIssuerSigningKey = true,
        ClockSkew = TimeSpan.Zero
    };

    // 🔌 WebSocket support
    o.Events = new JwtBearerEvents
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
         OnAuthenticationFailed = context =>
         {
             var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
             logger.LogError(context.Exception, "USER Auth Failed");
             return Task.CompletedTask;
         }

    };
})

// ================= TENANT JWT =================
.AddJwtBearer("TenantScheme", o =>
{
    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidIssuer = builder.Configuration["JwtTenant:Issuer"],
        ValidAudience = builder.Configuration["JwtTenant:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["JwtTenant:Key"])
        ),
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = false, // change to true in production
        ValidateIssuerSigningKey = true,
        ClockSkew = TimeSpan.Zero
    };

    o.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogError(context.Exception, "TENANT Auth Failed");
            return Task.CompletedTask;
        }
    };
});
builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpClient();

//builder.Services.AddSwaggerGen();
builder.Services.AddSwaggerGen(swagger =>
{
    swagger.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "TaskEngineAPI",
        Version = "v1"
    });
    swagger.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        // Description = "JWT Authorization header using the Bearer scheme. \r\n\r\n Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\nExample: \"Bearer 12345abcdef\"",
        Description = "Enter 'Bearer {token}'"

    });
    swagger.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
           // new string[] {}
           Array.Empty<string>()
        }
    });
    swagger.OperationFilter<TaskEngineAPI.Helpers.FileUploadOperationFilter>();
});



builder.Services.AddWebSocketServices();

builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IAdminService, AccountService>();
builder.Services.AddScoped<IAdminRepository, AdminRepository>();
builder.Services.AddScoped<IProcessEngineService, ProcessEngineService>();
builder.Services.AddScoped<ITaskMasterService, TaskMasterService>();
builder.Services.AddScoped<ILookUpService, LookUpService>();
builder.Services.AddScoped<IMinioService, MinioService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IAnalyticalService, AnalyticalService>();
builder.Services.AddScoped<ProjectSocketHandler>();
builder.Services.AddSingleton<WebSocketConnectionManager>();
builder.Services.AddScoped<IApiProxyService, APIIntegrationService>();
builder.Services.Configure<WhatsAppSettings>(
    builder.Configuration.GetSection("WhatsAppSettings"));

//var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";
//builder.Services.AddCors(options =>
//{
//    options.AddPolicy(name: MyAllowSpecificOrigins,
//        policy =>
//        {
//            policy.WithOrigins(
//                "https://portal.sheenlac.com",
//                "https://AllPaintsEcomAPI.sheenlac.com",
//                "https://vendor.sheenlac.com",
//                "https://devmisportal.sheenlac.com",
//                "https://misportal.sheenlac.com",
//                "http://localhost:4200",
//                "http://localhost:5000",
//                "https://localhost:7257",
//                "https://devvendor.sheenlac.com",
//                "https://devportal.sheenlac.com",
//                "https://devtaskflow.sheenlac.com",
//                "https://misapi.sheenlac.com",
//                "https://devmisapi.sheenlac.com",
//                "https://misapi.sheenlac.com",
//                "https://devmisapi.sheenlac.com",
//                "https://misapi.sheenlac.com/api",
//                "https://misdevapi.sheenlac.com"
//            )

//            .AllowAnyHeader()
//            .AllowAnyMethod();

//        });
//});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.WithOrigins(
                "https://devtaskflow.sheenlac.com",
                "https://progovex.sheenlac.com",
                "http://localhost:3000",   // Add your React/Vue local port
                "http://localhost:5173",   // Add your Vite local port
                "http://localhost:4200"    // Add your Angular local port
              )
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials() // Required for JWT if sent via cookies/auth headers
              .SetIsOriginAllowed(origin => true);
    });
});


var app = builder.Build();
//app.UseSerilogRequestLogging();
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "Handled {RequestPath}";
});
app.UseSwagger();
app.UseSwaggerUI();
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
//app.UseMiddleware<JwtValidationMiddleware>();

//app.UseExceptionHandler("/Error");
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exceptionHandler = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();

        logger.LogError(exceptionHandler?.Error, "Global Exception Occurred");

        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";

        await context.Response.WriteAsync(
            System.Text.Json.JsonSerializer.Serialize(new
            {
                StatusCode = 500,
                Message = "Internal Server Error"
            })
        );
    });
});



app.UseRouting();
//app.UseCors(MyAllowSpecificOrigins);
app.UseMiddleware<ExceptionMiddleware>();
app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();
app.UseLookUpMiddleware();
//app.UseWebSockets();
app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(30) // Sends a protocol-level ping
});

app.UseWebSocketEndpoints();

app.MapControllers();

//app.Map("/ws/project", async context =>
//{
//    var handler = context.RequestServices.GetRequiredService<ProjectSocketHandler>();
//    await handler.HandleAsync(context);
//});

app.Run();