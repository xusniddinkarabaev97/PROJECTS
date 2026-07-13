using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.RateLimiting;
using GzsBilling.Domain.Enums;
using GzsBilling.Api.Authorization;
using GzsBilling.Api.Middleware;
using GzsBilling.Application;
using GzsBilling.Infrastructure.Configuration;
using GzsBilling.Infrastructure.Persistence;
using GzsBilling.Infrastructure.Services;
using GzsBilling.Api.Services;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Enrichers.CorrelationId;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// ──────────────────────────────────────────────
// 1. Configuration
// ──────────────────────────────────────────────
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

// ──────────────────────────────────────────────
// 2. Serilog structured logging to console AND file (logs/audit-.log)
// ──────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.WithCorrelationId()
    .Enrich.WithMachineName()
    .Enrich.WithEnvironmentName()
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] {CorrelationId} {MachineName} {EnvironmentName} {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/audit-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 365,
        outputTemplate:
        "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] {CorrelationId} {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

// ──────────────────────────────────────────────
// 3. Controllers and service registration
// ──────────────────────────────────────────────
builder.Services.AddControllers();

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(GzsBilling.Application.AssemblyMarker).Assembly);
});

builder.Services.AddBillingInfrastructure(builder.Configuration);

builder.Services.AddDbContext<BillingDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddBillingApplication();

// UGaz Payment Service registration
builder.Services.Configure<UGazSettings>(builder.Configuration.GetSection("UGazSettings"));
builder.Services.AddHttpClient<IUGazPaymentService, UGazPaymentService>()
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback =
            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
    });

// ──────────────────────────────────────────────
// 4. JWT Bearer Authentication (RS256)
// ──────────────────────────────────────────────
var jwtSettings = builder.Configuration
    .GetSection("JwtSettings")
    .Get<JwtSettings>()!;

// Use HMAC-SHA256 symmetric key for JWT signing
var signingKey = jwtSettings.IssuerSigningKey;
var keyBytes = Encoding.UTF8.GetBytes(signingKey);
if (keyBytes.Length < 32)
{
    var padded = new byte[32];
    Array.Copy(keyBytes, padded, Math.Min(keyBytes.Length, 32));
    keyBytes = padded;
}
var symmetricKey = new SymmetricSecurityKey(keyBytes);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = symmetricKey,
        ValidateIssuer = true,
        ValidIssuer = jwtSettings.Authority,
        ValidateAudience = true,
        ValidAudience = jwtSettings.Audience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromMinutes(5)
    };

    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            Log.Error(context.Exception, "JWT AUTH FAILED: {Message}", context.Exception.Message);
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            Log.Information("JWT TOKEN VALIDATED: {Subject}",
                context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            Log.Warning("JWT CHALLENGE: {Error}", context.ErrorDescription);
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

// Register permission-based authorization
builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
builder.Services.AddAuthorization(options =>
{
    // Add policies for each controller area
    options.AddPolicy("StationsView", p => p.Requirements.Add(new PermissionRequirement(Permission.StationsView)));
    options.AddPolicy("StationsCreate", p => p.Requirements.Add(new PermissionRequirement(Permission.StationsCreate)));
    options.AddPolicy("UsersCreate", p => p.Requirements.Add(new PermissionRequirement(Permission.UsersCreate)));
    options.AddPolicy("ShareholdersCreate", p => p.Requirements.Add(new PermissionRequirement(Permission.ShareholdersCreate)));
    options.AddPolicy("TransactionsView", p => p.Requirements.Add(new PermissionRequirement(Permission.TransactionsView)));
    options.AddPolicy("RefundsView", p => p.Requirements.Add(new PermissionRequirement(Permission.RefundsView)));
    options.AddPolicy("ReconciliationView", p => p.Requirements.Add(new PermissionRequirement(Permission.ReconciliationView)));
    options.AddPolicy("DisputesView", p => p.Requirements.Add(new PermissionRequirement(Permission.DisputesView)));
    options.AddPolicy("ReportsView", p => p.Requirements.Add(new PermissionRequirement(Permission.ReportsView)));
    options.AddPolicy("DashboardView", p => p.Requirements.Add(new PermissionRequirement(Permission.DashboardView)));
});

// ──────────────────────────────────────────────
// 5. Rate Limiter (FixedWindow, configurable per policy)
// ──────────────────────────────────────────────
var rateLimitSettings = builder.Configuration
    .GetSection("RateLimitingSettings")
    .Get<RateLimitingSettings>()!;

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddFixedWindowLimiter("Default", config =>
    {
        config.PermitLimit = rateLimitSettings.DefaultPolicy.PermitLimit;
        config.Window = TimeSpan.FromSeconds(rateLimitSettings.DefaultPolicy.WindowSeconds);
        config.QueueProcessingOrder = rateLimitSettings.DefaultPolicy.QueueProcessingOrder switch
        {
            "OldestFirst" => QueueProcessingOrder.OldestFirst,
            "NewestFirst" => QueueProcessingOrder.NewestFirst,
            _ => QueueProcessingOrder.OldestFirst
        };
        config.QueueLimit = rateLimitSettings.DefaultPolicy.QueueLimit;
    });

    if (rateLimitSettings.ContragentPolicies is not null)
    {
        foreach (var kvp in rateLimitSettings.ContragentPolicies)
        {
            string policyName = kvp.Key;
            var policy = kvp.Value;

            options.AddFixedWindowLimiter(policyName, config =>
            {
                config.PermitLimit = policy.PermitLimit;
                config.Window = TimeSpan.FromSeconds(policy.WindowSeconds);
                config.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                config.QueueLimit = 0;
            });
        }
    }
});

// ──────────────────────────────────────────────
// 6. Redis distributed cache
// ──────────────────────────────────────────────
string redisConnectionString = builder.Configuration.GetValue<string>("Redis:ConnectionString") ?? "localhost:6379";
string redisInstanceName = builder.Configuration.GetValue<string>("Redis:InstanceName") ?? "billing:";

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConnectionString;
    options.InstanceName = redisInstanceName;
});

// ──────────────────────────────────────────────
// 7. Swagger / OpenAPI with Bearer token security
// ──────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "GzsBilling API",
        Version = "v1",
        Description = "Billing system API for payment processing and invoice management"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT Bearer token. Example: Bearer eyJhbGci..."
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

    var xmlDocPath = Path.Combine(AppContext.BaseDirectory, "GzsBilling.Api.xml");
    if (File.Exists(xmlDocPath))
    {
        options.IncludeXmlComments(xmlDocPath);
    }
});

// ──────────────────────────────────────────────
// 8. MassTransit RabbitMQ configuration (optional)
// ──────────────────────────────────────────────
var rabbitMqEnabled = builder.Configuration.GetValue<bool>("RabbitMqSettings:Enabled");
if (rabbitMqEnabled)
{
var rabbitMqSettings = builder.Configuration
    .GetSection("RabbitMqSettings")
    .Get<RabbitMqSettings>()!;

builder.Services.AddMassTransit(configurator =>
{
    configurator.SetKebabCaseEndpointNameFormatter();

    configurator.AddConsumers(typeof(GzsBilling.Application.AssemblyMarker).Assembly);

    configurator.UsingRabbitMq((context, factoryConfig) =>
    {
        factoryConfig.Host(new Uri(rabbitMqSettings.Host), hostCfg =>
        {
            hostCfg.Username(rabbitMqSettings.Username);
            hostCfg.Password(rabbitMqSettings.Password);
        });

        factoryConfig.PrefetchCount = rabbitMqSettings.PrefetchCount;
        factoryConfig.AutoDelete = false;
        factoryConfig.Durable = true;

        if (rabbitMqSettings.RetryPolicy is not null)
        {
            factoryConfig.UseMessageRetry(retryCfg =>
            {
                List<TimeSpan> intervals = rabbitMqSettings.RetryPolicy.Intervals
                    .Select(TimeSpan.Parse)
                    .ToList();

                retryCfg.Interval(
                    rabbitMqSettings.RetryPolicy.MaxRetryCount,
                    intervals.Count == 1 ? intervals[0] : TimeSpan.FromSeconds(5));
            });
        }

        if (rabbitMqSettings.CircuitBreaker is not null)
        {
            factoryConfig.UseCircuitBreaker(cbConfig =>
            {
                cbConfig.TrackingPeriod = TimeSpan.Parse(rabbitMqSettings.CircuitBreaker.TrackingPeriod);
                cbConfig.TripThreshold = rabbitMqSettings.CircuitBreaker.TripThreshold;
                cbConfig.ActiveThreshold = rabbitMqSettings.CircuitBreaker.ActiveThreshold;
                cbConfig.ResetInterval = TimeSpan.Parse(rabbitMqSettings.CircuitBreaker.ResetInterval);
            });
        }

        // Configure endpoints (DLQ handled by queue setup)
        factoryConfig.ConfigureEndpoints(context);
    });
});
} // End MassTransit conditional block

// ──────────────────────────────────────────────
// 9. Health checks
// ──────────────────────────────────────────────
builder.Services.AddHealthChecks();

// ──────────────────────────────────────────────
// Build and configure middleware pipeline
// ──────────────────────────────────────────────
var app = builder.Build();

// Auto-migrate database on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
    db.Database.Migrate();
}

app.UseSerilogRequestLogging();

app.UseMiddleware<AuditLoggingMiddleware>();
app.UseMiddleware<ReplayProtectionMiddleware>();
app.UseMiddleware<WebhookSignatureMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.UseRateLimiter();

app.MapControllers();

app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";

        var response = new
        {
            status = report.Status.ToString(),
            totalDuration = report.TotalDuration.ToString(),
            entries = report.Entries.Select(e => new
            {
                key = e.Key,
                status = e.Value.Status.ToString(),
                duration = e.Value.Duration.ToString(),
                description = e.Value.Description
            })
        };

        await context.Response.WriteAsJsonAsync(response);
    }
});

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "GzsBilling API v1");
    options.RoutePrefix = "swagger";
});

try
{
    Log.Information("Starting GzsBilling API");
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "GzsBilling API terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
