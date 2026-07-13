using GzsBilling.Domain.Configuration;
using GzsBilling.Infrastructure.Clients;
using GzsBilling.Infrastructure.Data;
using GzsBilling.Infrastructure.Settings;
using GzsBilling.Sverka.Calculation;
using GzsBilling.Sverka.Reconciliation;
using GzsBilling.Sverka.Workers;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;

var builder = Host.CreateApplicationBuilder(args);

// ── Bind BillingOptions ──
builder.Services.Configure<BillingOptions>(builder.Configuration.GetSection(BillingOptions.SectionName));

// ── System Settings ──
builder.Services.AddScoped<ISystemSettingService, SystemSettingService>();

// ── PostgreSQL ──
builder.Services.AddDbContext<GzsBillingDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("GzsBillingWrite")
        ?? "Host=primary-db;Database=gzs_billing;Username=billing_user;Password=changeme"));

// ── RabbitMQ ──
builder.Services.AddSingleton<IConnection>(sp =>
{
    var factory = new ConnectionFactory
    {
        Uri = new Uri(builder.Configuration.GetConnectionString("RabbitMQ")
            ?? "amqp://guest:guest@localhost:5672"),
        ClientProvidedName = "gzs-billing-sverka"
    };
    return factory.CreateConnectionAsync().GetAwaiter().GetResult();
});

// ── HTTP Client for ABC Bank ──
builder.Services.AddHttpClient<IAbcBankClient, AbcBankClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Billing:AbcBank:BaseUrl"] ?? "https://api.abcbank.uz");
    client.Timeout = TimeSpan.FromSeconds(30);
});

// ── Background Workers & Services ──
builder.Services.AddSingleton<RabbitMqSverkaConsumerWorker>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<RabbitMqSverkaConsumerWorker>());

builder.Services.AddScoped<UlushTaqsimotHisoblagich>();
builder.Services.AddScoped<IKunlikSverkaMenejeri, KunlikSverkaMenejeri>();

var host = builder.Build();
host.Run();
