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

    c.SwaggerDoc("uparking", new OpenApiInfo
    {
        Title = "UParking Billing Integration",
        Version = "v1",
        Description = "Billing Provider API per UParking spec v1.0. Direction A: POST /api/billing/create. Direction B2: POST /api/billing/payment. Barriers managed by UParking."
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
        if (docName == "uparking")
            return apiDesc.GroupName == "uparking" || apiDesc.RelativePath?.Contains("DahuaIntegration") == true || apiDesc.RelativePath?.Contains("billing") == true;
        if (docName == "click")
            return apiDesc.GroupName == "click" || apiDesc.RelativePath?.Contains("click") == true;
        return apiDesc.GroupName != "uparking" && apiDesc.GroupName != "click";
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
    c.SwaggerEndpoint("/smartparking/swagger/uparking/swagger.json", "UParking Интеграция");
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
