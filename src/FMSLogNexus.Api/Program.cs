using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.SignalR;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using FMSLogNexus.Api.Authentication;
using FMSLogNexus.Api.BackgroundServices;
using FMSLogNexus.Api.Hubs;
using FMSLogNexus.Api.Middleware;
using FMSLogNexus.Core.Interfaces.Services;
using FMSLogNexus.Infrastructure.Data;
using FMSLogNexus.Infrastructure.Data.Repositories;
using FMSLogNexus.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// ============================================================================
// Configuration
// ============================================================================
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

var jwtSettings = builder.Configuration.GetSection("Jwt");
var jwtSecretKey = jwtSettings["SecretKey"]
    ?? throw new InvalidOperationException("JWT SecretKey not configured.");

// ============================================================================
// Services
// ============================================================================

// Add Data Access Layer
var isDevelopment = builder.Environment.IsDevelopment();
builder.Services.AddDataAccessLayer(connectionString, isDevelopment, isDevelopment);

// Add Application Services
builder.Services.AddScoped<ILogService, LogService>();
builder.Services.AddScoped<IJobService, JobService>();
builder.Services.AddScoped<IJobExecutionService, JobExecutionService>();
builder.Services.AddScoped<IServerService, ServerService>();
builder.Services.AddScoped<IAlertService, AlertService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IApiKeyService, ApiKeyService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IConfigurationService, ConfigurationService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IMaintenanceService, FMSLogNexus.Infrastructure.Services.MaintenanceService>();

// Add Memory Cache
builder.Services.AddMemoryCache();

// Add Controllers
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.WriteIndented = isDevelopment;
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

// Add Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"] ?? "FMSLogNexus",
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"] ?? "FMSLogNexusClients",
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromMinutes(1)
    };

    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            if (context.Exception is SecurityTokenExpiredException)
            {
                context.Response.Headers.Append("Token-Expired", "true");
            }
            return Task.CompletedTask;
        },
        OnMessageReceived = context =>
        {
            // Allow JWT token from query string for SignalR
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;

            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
            }

            return Task.CompletedTask;
        }
    };
});

// Add Authorization Policies
builder.Services.AddAuthorization(options =>
{
    // Role-based policies
    options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
    options.AddPolicy("Operator", policy => policy.RequireRole("Admin", "Operator"));
    options.AddPolicy("Viewer", policy => policy.RequireRole("Admin", "Operator", "Viewer"));
    options.AddPolicy("Agent", policy => policy.RequireRole("Admin", "Agent"));

    // Operation-based policies
    options.AddPolicy("LogIngestion", policy => policy.RequireRole("Admin", "Agent"));
    options.AddPolicy("LogRead", policy => policy.RequireRole("Admin", "Operator", "Viewer", "Agent"));
    options.AddPolicy("LogExport", policy => policy.RequireRole("Admin", "Operator"));

    options.AddPolicy("JobRead", policy => policy.RequireRole("Admin", "Operator", "Viewer", "Agent"));
    options.AddPolicy("JobWrite", policy => policy.RequireRole("Admin", "Operator", "Agent"));

    options.AddPolicy("ExecutionRead", policy => policy.RequireRole("Admin", "Operator", "Viewer", "Agent"));
    options.AddPolicy("ExecutionWrite", policy => policy.RequireRole("Admin", "Operator", "Agent"));

    options.AddPolicy("ServerRead", policy => policy.RequireRole("Admin", "Operator", "Viewer", "Agent"));
    options.AddPolicy("ServerWrite", policy => policy.RequireRole("Admin", "Operator"));
    options.AddPolicy("Heartbeat", policy => policy.RequireRole("Admin", "Agent"));

    options.AddPolicy("AlertRead", policy => policy.RequireRole("Admin", "Operator", "Viewer"));
    options.AddPolicy("AlertWrite", policy => policy.RequireRole("Admin", "Operator"));
});

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });

    options.AddPolicy("AllowSpecific", policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Add Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "FMS Log Nexus API",
        Description = "Centralized logging and job monitoring API for FileMaker Server environments",
        Contact = new OpenApiContact
        {
            Name = "FMS Log Nexus Support",
            Email = "support@fmslognexus.example.com"
        }
    });

    // Add JWT authentication
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
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
            Array.Empty<string>()
        }
    });

    // Add API key authentication
    options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Description = "API Key authentication. Enter your API key.",
        Name = "X-Api-Key",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey
    });

    // Group by tag
    options.TagActionsBy(api => new[] { api.GroupName ?? "Other" });

    // Include all endpoints in the v1 doc regardless of GroupName
    options.DocInclusionPredicate((docName, api) => true);

    // Include XML comments
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

// Add Health Checks
builder.Services.AddHealthChecks()
    .AddSqlServer(connectionString, name: "database");

// Add SignalR
builder.Services.AddSignalRHubs();
builder.Services.AddSingleton<IUserIdProvider, ClaimsUserIdProvider>();

// Add Background Services
builder.Services.AddBackgroundServices(builder.Configuration);

// ============================================================================
// Build Application
// ============================================================================
var app = builder.Build();

// ============================================================================
// Middleware Pipeline
// ============================================================================

// Global exception handling (first in pipeline)
app.UseExceptionHandling();

// Request logging
app.UseRequestLogging();

// Development only
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// Swagger
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "FMS Log Nexus API v1");
    options.RoutePrefix = "swagger";
});

// HTTPS Redirection
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// CORS
app.UseCors(app.Environment.IsDevelopment() ? "AllowAll" : "AllowSpecific");

// Rate limiting
var rateLimitingOptions = new RateLimitingOptions
{
    Enabled = !app.Environment.IsDevelopment(),
    WindowDuration = TimeSpan.FromMinutes(1),
    DefaultLimit = 100,
    LogIngestionLimit = 1000,
    BatchLimit = 50
};
app.UseRateLimiting(rateLimitingOptions);

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Health Checks
app.MapHealthChecks("/health");

// SignalR Hubs
app.MapSignalRHubs();

// Controllers
app.MapControllers();

// ============================================================================
// Run Application
// ============================================================================
app.Run();
