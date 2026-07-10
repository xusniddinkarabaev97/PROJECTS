using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using GzsBilling.Infrastructure.Clients;
using GzsBilling.Infrastructure.Data;
using GzsBilling.Domain.Configuration;
using GzsBilling.Infrastructure.Messaging;
using GzsBilling.Infrastructure.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using RabbitMQ.Client;

var builder = WebApplication.CreateBuilder(args);

// ── Bind BillingOptions ──
builder.Services.Configure<BillingOptions>(builder.Configuration.GetSection(BillingOptions.SectionName));

// ── System Settings ──
builder.Services.AddScoped<ISystemSettingService, SystemSettingService>();

// ── Swagger / OpenAPI ──
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var groups = new[] { "Auth", "UGaz", "QR", "Admin" };
    foreach (var group in groups)
    {
        options.SwaggerDoc(group, new()
        {
            Title = $"GZS Billing — {group}",
            Version = "v1",
            Description = $"{group} endpoints for the GZS Billing system."
        });
    }

    options.DocInclusionPredicate((docName, api) =>
    {
        var groupAttr = api.ActionDescriptor.EndpointMetadata
            .OfType<ApiExplorerSettingsAttribute>()
            .FirstOrDefault();
        return groupAttr?.GroupName == docName || (docName == "Admin" && groupAttr == null);
    });

    options.AddSecurityDefinition("Bearer", new()
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: 'Bearer {token}'",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }


});

// ── JWT Authentication ──
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? "gzs-billing-super-secret-key-2024-min-32-chars!!";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "gzs-billing",
            ValidAudience = "gzs-billing-admin",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
        };
    });
builder.Services.AddAuthorization();

// ── Controllers ──
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.WriteIndented = false;
        options.JsonSerializerOptions.NumberHandling = JsonNumberHandling.AllowReadingFromString;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

// ── PostgreSQL with Read/Write Split ──
var writeConnectionString = builder.Configuration.GetConnectionString("GzsBillingWrite")
    ?? "Host=primary-db;Database=gzs_billing;Username=billing_user;Password=changeme";

var readConnectionString = builder.Configuration.GetConnectionString("GzsBillingRead")
    ?? "Host=replica-db;Database=gzs_billing;Username=billing_user;Password=changeme";

builder.Services.AddDbContext<GzsBillingDbContext>(options =>
    options.UseNpgsql(writeConnectionString));

// Register read-only context factory
builder.Services.AddSingleton(readConnectionString);

// ── RabbitMQ ──
var rabbitMqConnectionString = builder.Configuration.GetConnectionString("RabbitMQ")
    ?? "amqp://guest:guest@localhost:5672";

try
{
    var rmqConnection = new ConnectionFactory
    {
        Uri = new Uri(rabbitMqConnectionString),
        ClientProvidedName = "gzs-billing-api"
    }.CreateConnectionAsync().GetAwaiter().GetResult();

    builder.Services.AddSingleton<IConnection>(rmqConnection);
    builder.Services.AddSingleton<IRabbitMqTranzaksiyaPublisher, RabbitMqTranzaksiyaPublisher>();
}
catch (Exception ex)
{
    var logger = LoggerFactory.Create(c => c.AddConsole()).CreateLogger("Startup");
    logger.LogWarning(ex, "RabbitMQ not available at startup. UGaz payment events will not be published.");

    builder.Services.AddSingleton<IRabbitMqTranzaksiyaPublisher, NoOpRabbitMqPublisher>();
}

// ── HTTP Clients ──
builder.Services.AddHttpClient<IUGazInfrastrukturaClient, UGazInfrastrukturaClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Billing:UGaz:BaseUrl"] ?? "https://api.ugaz.uz");
    client.Timeout = TimeSpan.FromSeconds(5);
});

builder.Services.AddHttpClient<IAbcBankClient, AbcBankClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Billing:AbcBank:BaseUrl"] ?? "https://api.abcbank.uz");
    client.Timeout = TimeSpan.FromSeconds(30);
});

var app = builder.Build();

// ── Middleware Pipeline ──
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/Auth/swagger.json", "Auth");
    options.SwaggerEndpoint("/swagger/UGaz/swagger.json", "UGaz");
    options.SwaggerEndpoint("/swagger/QR/swagger.json", "QR");
    options.SwaggerEndpoint("/swagger/Admin/swagger.json", "Admin");
    options.RoutePrefix = "swagger";
    options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
});

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

// ── Root redirect ──
app.MapGet("/", () => Results.Redirect("/swagger"));

app.MapControllers();

// ── Ensure Database Schema & Seed ──
try
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<GzsBillingDbContext>();
    await dbContext.Database.MigrateAsync();

    // Seed default payment providers
    if (!dbContext.Payments.Any())
    {
        dbContext.Payments.AddRange(
            new GzsBilling.Domain.Entities.Payment { Id = 1, Name = "Uzcard", IsActive = true },
            new GzsBilling.Domain.Entities.Payment { Id = 2, Name = "Humo", IsActive = true },
            new GzsBilling.Domain.Entities.Payment { Id = 3, Name = "Click", IsActive = true },
            new GzsBilling.Domain.Entities.Payment { Id = 4, Name = "Paynet", IsActive = true }
        );
        await dbContext.SaveChangesAsync();
        app.Logger.LogInformation("Default payment providers seeded.");
    }

    // Seed / reset default superadmin user
    var adminUser = await dbContext.Users.FirstOrDefaultAsync(u => u.Username == "admin");
    if (adminUser is null)
    {
        dbContext.Users.Add(new GzsBilling.Domain.Entities.User
        {
            Id = Guid.NewGuid(),
            Username = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123!"),
            FullName = "Super Admin",
            Role = "superadmin",
            IsActive = true
        });
        await dbContext.SaveChangesAsync();
        app.Logger.LogInformation("Default superadmin user seeded (admin / admin123!).");
    }
    else
    {
        // Ensure password is up to date
        var expectedHash = BCrypt.Net.BCrypt.HashPassword("admin123!");
        if (!BCrypt.Net.BCrypt.Verify("admin123!", adminUser.PasswordHash))
        {
            adminUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123!");
            adminUser.IsActive = true;
            await dbContext.SaveChangesAsync();
            app.Logger.LogInformation("Admin password reset to admin123!");
        }
    }

    if (!dbContext.SystemSettings.Any())
    {
        var sql = "INSERT INTO system_settings (\"Id\", \"Key\", \"Value\", \"Category\", \"Description\", \"UpdatedAt\", \"UpdatedBy\") VALUES " +
            "(gen_random_uuid(), 'CardTypeSplits', '{{\"Uzcard\":{{\"bankSplitRate\":0.20,\"platformSplitRate\":0.80}},\"Humo\":{{\"bankSplitRate\":0.80,\"platformSplitRate\":0.20}}}}'::jsonb, 'CardType', 'Bank/platform split percentages per card type', NOW(), 'seed')," +
            "(gen_random_uuid(), 'PaymentIdCardTypeMap', '{{\"1\":\"Uzcard\",\"2\":\"Humo\"}}'::jsonb, 'CardType', 'Mapping from PaymentId to card type', NOW(), 'seed')," +
            "(gen_random_uuid(), 'DefaultCardType', '\"Unknown\"'::jsonb, 'CardType', 'Default card type when no mapping found', NOW(), 'seed')," +
            "(gen_random_uuid(), 'ActiveSeansCacheTtlMinutes', '5'::jsonb, 'Cache', 'Active session cache TTL in minutes', NOW(), 'seed')," +
            "(gen_random_uuid(), 'CommissionRates', '{{\"default\":{{\"commissionRate\":0.01,\"bankSplitRate\":0.50,\"platformSplitRate\":0.50}},\"1\":{{\"commissionRate\":0.01,\"bankSplitRate\":0.20,\"platformSplitRate\":0.80}},\"2\":{{\"commissionRate\":0.01,\"bankSplitRate\":0.80,\"platformSplitRate\":0.20}}}}'::jsonb, 'Commission', 'Payment provider commission rates keyed by PaymentId', NOW(), 'seed')," +
            "(gen_random_uuid(), 'SystemCommissionRate', '0.01'::jsonb, 'Commission', 'Default system commission rate', NOW(), 'seed')," +
            "(gen_random_uuid(), 'NetDistributionRate', '0.99'::jsonb, 'Commission', 'Net distribution rate after commission', NOW(), 'seed')";
        await dbContext.Database.ExecuteSqlRawAsync(sql);
        app.Logger.LogInformation("Default system settings seeded.");
    }

    app.Logger.LogInformation("Database ready. Existing data preserved. Schema up to date.");
}
catch (Exception ex)
{
    app.Logger.LogWarning(ex, "Database not available at startup. The API will start but database operations will fail until connectivity is restored.");
}

app.Run();
