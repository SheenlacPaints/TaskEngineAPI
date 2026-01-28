using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using TaskEngineAPI.Interfaces;
using TaskEngineAPI.Middleware;
using TaskEngineAPI.Middlewares;
using TaskEngineAPI.Repositories;
using TaskEngineAPI.Services;

using Hangfire;
using Hangfire.MemoryStorage;

var builder = WebApplication.CreateBuilder(args);

//builder.Services.AddScoped<IRoleService, RoleService>();
//builder.Services.AddDbContext<ApplicationDbContext>(options =>
//   options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddMemoryCache();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // CHANGE THIS LINE:
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
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
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true
    };
});

builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddHttpClient();

//builder.Services.AddSwaggerGen();
builder.Services.AddSwaggerGen(swagger =>
{
    swagger.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme. \r\n\r\n Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\nExample: \"Bearer 12345abcdef\"",
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
            new string[] {}
        }
    });
});


builder.Services.AddHttpClient<IWhatsAppSchedulerService, WhatsAppSchedulerService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Add logging
builder.Services.AddLogging();

// Add scheduler services
builder.Services.AddSingleton<BackgroundSchedulerService>();
builder.Services.AddSingleton<ISchedulerService>(provider =>
    provider.GetRequiredService<BackgroundSchedulerService>());

// Hangfire configuration
builder.Services.AddHangfire(config =>
    config.UseMemoryStorage());
builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = 1; // Reduce if needed
    options.ServerTimeout = TimeSpan.FromMinutes(5);
    options.ServerCheckInterval = TimeSpan.FromSeconds(5);
});

builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IAdminService, AccountService>();
builder.Services.AddScoped<IAdminRepository, AdminRepository>();
builder.Services.AddScoped<IProcessEngineService, ProcessEngineService>();
builder.Services.AddScoped<ITaskMasterService, TaskMasterService>();
builder.Services.AddScoped<ILookUpService, LookUpService>();
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
app.UseHangfireDashboard("/hangfire");
app.UseLookUpMiddleware();
app.MapControllers();

app.Run();