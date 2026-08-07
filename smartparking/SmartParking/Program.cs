// <--- ADD THESE USING STATEMENTS for your specific project namespaces
using SmartParking.Data;
using SmartParking.Services;
using SmartParking.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System;
using System.Text;
using System.Text.Json.Serialization;
using System.Reflection;


var builder = WebApplication.CreateBuilder(args);

// CORS — allow avto.itpanda.uz to call API
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

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

// Dahua DSS Integration Services
builder.Services.AddHttpClient<IDahuaApiService, DahuaApiService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(15);
    client.DefaultRequestHeaders.Accept.Add(
        new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
});
builder.Services.AddScoped<IParkingSessionService, ParkingSessionService>();

// Security Services
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddSingleton<ISignatureService, SignatureService>();


builder.Services.AddControllers();
builder.Services.AddRazorPages();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SmartParking API",
        Version = "v1",
        Description = "Core API: транзакции, QR-коды, компании, тарифы"
    });

    c.SwaggerDoc("dahua", new OpenApiInfo
    {
        Title = "Dahua Интеграция",
        Version = "v1",
        Description = "Webhook для камер Dahua DSS:\n\n" +
                      "**Подключение:**\n" +
                      "1. В DSS: System Integration → Event Transferal → Web Service\n" +
                      "2. URL: `https://whirl.uz/api/DahuaIntegration/events`\n" +
                      "3. Secret: `dss_webhook_secret_2026`\n" +
                      "4. Формат: JSON, ANPR события\n\n" +
                      "**Бизнес-процесс:**\n" +
                      "- Въезд → создание сессии + открытие шлагбаума\n" +
                      "- Выезд → расчёт стоимости + QR для Click\n" +
                      "- Оплата через Click → авт. открытие шлагбаума"
    });

    c.SwaggerDoc("click", new OpenApiInfo
    {
        Title = "Click Платёжная интеграция",
        Version = "v1",
        Description = "Интеграция с платёжной системой Click для оплаты парковки.\n\n" +
                      "**Формат QR:** `service_id=2005&merchant_id=19876&amount={tiyin}&transaction_param={id}`\n\n" +
                      "**Коды ошибок:**\n" +
                      "- `0` — успех\n" +
                      "- `-1` — неверная подпись\n" +
                      "- `-2` — сумма не совпадает\n" +
                      "- `-4` — уже оплачено / отменено\n" +
                      "- `-5` — транзакция не найдена\n" +
                      "- `-9` — внутренняя ошибка"
    });

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

    // XML comments
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        c.IncludeXmlComments(xmlPath);

    // Group controllers by namespace/attribute
    c.DocInclusionPredicate((docName, apiDesc) =>
    {
        if (docName == "dahua")
            return apiDesc.GroupName == "dahua" || apiDesc.RelativePath?.Contains("DahuaIntegration") == true;
        if (docName == "click")
            return apiDesc.GroupName == "click" || apiDesc.RelativePath?.Contains("click") == true;
        return apiDesc.GroupName != "dahua" && apiDesc.GroupName != "click";
    });
});
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
});
var app = builder.Build();

// Swagger always available (including production)
app.UseSwagger(c =>
{
    c.RouteTemplate = "swagger/{documentName}/swagger.json";
});
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/smartparking/swagger/v1/swagger.json", "SmartParking Core API V1");
    c.SwaggerEndpoint("/smartparking/swagger/dahua/swagger.json", "Dahua Интеграция");
    c.SwaggerEndpoint("/smartparking/swagger/click/swagger.json", "Click Платёжная интеграция");
    c.RoutePrefix = "swagger";
});

app.UseCors();

app.UseSecurityHeaders();
app.UseIpWhitelist();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapRazorPages();

app.MapGet("/", () => Results.Redirect("/Billing"));

app.Run();
