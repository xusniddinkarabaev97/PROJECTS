using ODULink.Data;
using ODULink.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secret = jwtSettings["Secret"] ?? throw new InvalidOperationException("JWT Secret is not configured.");
var issuer = jwtSettings["Issuer"] ?? throw new InvalidOperationException("JWT Issuer is not configured.");
var audience = jwtSettings["Audience"] ?? throw new InvalidOperationException("JWT Audience is not configured.");

var key = Encoding.ASCII.GetBytes(secret);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = issuer,
        ValidateAudience = true,
        ValidAudience = audience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminPolicy", policy => policy.RequireRole("Admin"));
});

builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IClickService, ClickService>();
builder.Services.AddScoped<IAuditService, AuditService>();

// Background: random transaction generator (every 15 min)
builder.Services.AddHostedService<TransactionGeneratorService>();

// EMIS HttpClient
builder.Services.AddHttpClient<IEmisService, EmisService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "BillingSCHR API — Мобилизационный призывной резерв",
        Version = "v1",
        Description = "Биллинговая система Мобилизационного призывного резерва МО РУз.\n\n" +
                      "**Основные модули:** организации, отделения, пациенты, транзакции, тарифы.\n\n" +
                      "**Интеграции:** Click (платежи), EMIS (медицинская информсистема)."
    });

    c.SwaggerDoc("integration", new OpenApiInfo
    {
        Title = "Интеграция Click + EMIS",
        Version = "v1",
        Description = "Эндпоинты для интеграции с платёжной системой Click и EMIS.\n\n" +
                      "**Бизнес-процесс:**\n" +
                      "1. Click → `/api/click/prepare` — валидация пациента через EMIS\n" +
                      "2. Click → `/api/click/complete` — подтверждение оплаты → EMIS\n\n" +
                      "**EMIS API (внешняя система):**\n" +
                      "- `POST /api/v1/billing/check` — проверка задолженности\n" +
                      "- `POST /api/v1/billing/pay` — подтверждение оплаты"
    });

    // XML-комментарии из сборки
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        c.IncludeXmlComments(xmlPath);

    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Enter JWT Bearer token **_only_**",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Reference = new OpenApiReference
        {
            Id = "Bearer",
            Type = ReferenceType.SecurityScheme
        }
    };
    c.AddSecurityDefinition("Bearer", securityScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { securityScheme, new[] { "Bearer" } }
    });
});
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
}); 
var app = builder.Build();

// Auto-create database tables and seed admin on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.EnsureCreated();

    // Seed default admin company if none exists
    if (!db.Companies.Any())
    {
        db.Companies.Add(new ODULink.Models.Company
        {
            Name = "BillingSCHR Admin",
            Login = "admin",
            Inn = "000000000",
            Address = "Tashkent",
            Phone = "+998000000000",
            Email = "admin@billing-schr.uz",
            Role = "Admin",
            JwtAuthToken = "admin123",
            RefreshToken = "",
            TokenExpiry = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        db.SaveChanges();
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "BillingSCHR — МПР API V1");
        c.SwaggerEndpoint("/swagger/integration/swagger.json", "Интеграция Click + EMIS");
    });
}

app.UseDefaultFiles();
app.UseStaticFiles();


app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapFallbackToFile("admin/index.html");

app.Run();
