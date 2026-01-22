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
using TaskEngineAPI.Helpers;



var builder = WebApplication.CreateBuilder(args);
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File(
        "Logs/api-log-.txt",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,     // keep 30 days logs
        fileSizeLimitBytes: 10_000_000, // 10 MB per file
        rollOnFileSizeLimit: true
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
    });

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(o =>
{
    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey
            (Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])),
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = false,
        ValidateIssuerSigningKey = true
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





builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IAdminService, AccountService>();
builder.Services.AddScoped<IAdminRepository, AdminRepository>();
builder.Services.AddScoped<IProcessEngineService, ProcessEngineService>();
builder.Services.AddScoped<ITaskMasterService, TaskMasterService>();
builder.Services.AddScoped<ILookUpService, LookUpService>();
builder.Services.AddScoped<IMinioService, MinioService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<TaskMasterService>();
builder.Services.AddSingleton<ProjectSocketHandler>();

var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
        policy =>
        {
            policy.WithOrigins(
                "https://portal.sheenlac.com",
                "https://AllPaintsEcomAPI.sheenlac.com",
                "https://vendor.sheenlac.com",
                "https://devmisportal.sheenlac.com",
                "https://misportal.sheenlac.com",
                "http://localhost:4200",
                "http://localhost:5000",
                "https://localhost:7257",
                "https://devvendor.sheenlac.com",
                "https://devportal.sheenlac.com",
                "https://devtaskflow.sheenlac.com"

            )

            .AllowAnyHeader()
            .AllowAnyMethod();
        });
});

var app = builder.Build();
app.UseSerilogRequestLogging();


app.UseSwagger();
app.UseSwaggerUI();
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
//app.UseMiddleware<JwtValidationMiddleware>();

app.UseExceptionHandler("/Error");
app.UseCors(MyAllowSpecificOrigins);
app.UseAuthentication();
app.UseAuthorization();
app.UseLookUpMiddleware();
app.UseWebSockets();
app.MapControllers();
app.Map("/ws/project", async context =>
{
    var handler = context.RequestServices.GetRequiredService<ProjectSocketHandler>();
    await handler.HandleAsync(context);
});

app.Run();