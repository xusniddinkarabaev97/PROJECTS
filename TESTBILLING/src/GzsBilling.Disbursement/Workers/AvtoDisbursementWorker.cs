using GzsBilling.Domain.Entities;
using GzsBilling.Domain.Enums;
using GzsBilling.Infrastructure.Clients;
using GzsBilling.Infrastructure.Data;
using GzsBilling.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace GzsBilling.Disbursement.Workers;

public class AvtoDisbursementWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AvtoDisbursementWorker> _logger;
    private readonly TimeSpan _processingInterval;

    public AvtoDisbursementWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<AvtoDisbursementWorker> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;

        var intervalDays = configuration.GetValue<int>("Disbursement:ProcessingIntervalDays", 7);
        _processingInterval = TimeSpan.FromDays(intervalDays);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Auto-Disbursement Worker started. Processing interval: {Interval} days",
            _processingInterval.TotalDays);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessDisbursementsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during disbursement processing cycle.");
            }

            _logger.LogInformation("Next disbursement cycle in {Interval} days.", _processingInterval.TotalDays);
            await Task.Delay(_processingInterval, stoppingToken);
        }
    }

    private async Task ProcessDisbursementsAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GzsBillingDbContext>();
        var bankClient = scope.ServiceProvider.GetRequiredService<IAbcBankClient>();

        // Fetch all authorized but unpaid Schetfaktura documents
        var pendingInvoices = await dbContext.Schetfakturalar
            .Where(s => s.IsAuthorized && !s.IsPaid)
            .OrderBy(s => s.InvoiceDate)
            .ToListAsync(cancellationToken);

        if (pendingInvoices.Count == 0)
        {
            _logger.LogInformation("No pending invoices for disbursement.");
            return;
        }

        _logger.LogInformation("Processing {Count} pending invoices for disbursement.", pendingInvoices.Count);

        foreach (var invoice in pendingInvoices)
        {
            var natija = JsonSerializer.Deserialize<SchetfakturaNatija>(invoice.CalculationJson);
            if (natija?.StakeholderPayouts is null || natija.StakeholderPayouts.Count == 0)
            {
                _logger.LogWarning("Invoice {Id} has no stakeholder payouts. Skipping.", invoice.Id);
                continue;
            }

            foreach (var lineItem in natija.StakeholderPayouts)
            {
                var disbursement = new DisbursementTarixi
                {
                    Id = Guid.NewGuid(),
                    StakeholderId = lineItem.StakeholderId,
                    Amount = lineItem.PayoutAmount,
                    Status = DisbursementStatus.Processing,
                    SentAt = DateTimeOffset.UtcNow
                };

                dbContext.DisbursementTarixi.Add(disbursement);
                await dbContext.SaveChangesAsync(cancellationToken);

                try
                {
                    var bankRef = await bankClient.SendDisbursementAsync(
                        lineItem.BankAccount,
                        lineItem.PayoutAmount,
                        $"SCF-{invoice.Id}-{lineItem.StakeholderId}");

                    disbursement.BankReference = bankRef;
                    disbursement.Status = DisbursementStatus.Completed;

                    _logger.LogInformation(
                        "Disbursement completed: Stakeholder={Name}, Amount={Amount}, Ref={Ref}",
                        lineItem.FullName, lineItem.PayoutAmount, bankRef);
                }
                catch (Exception ex)
                {
                    disbursement.Status = DisbursementStatus.Failed;
                    _logger.LogError(ex,
                        "Disbursement FAILED: Stakeholder={Name}, Amount={Amount}",
                        lineItem.FullName, lineItem.PayoutAmount);
                }

                dbContext.DisbursementTarixi.Update(disbursement);
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            invoice.IsPaid = true;
            dbContext.Schetfakturalar.Update(invoice);
            await dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Invoice {Id} fully processed for disbursement.", invoice.Id);
        }
    }
}
