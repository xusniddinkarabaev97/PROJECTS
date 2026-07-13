using GzsBilling.Domain.Entities;
using GzsBilling.Domain.Enums;
using GzsBilling.Infrastructure.Clients;
using GzsBilling.Infrastructure.Data;
using GzsBilling.Sverka.Calculation;
using GzsBilling.Sverka.Workers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GzsBilling.Sverka.Reconciliation;

public class KunlikSverkaMenejeri : IKunlikSverkaMenejeri
{
    private readonly GzsBillingDbContext _dbContext;
    private readonly IAbcBankClient _abcBankClient;
    private readonly UlushTaqsimotHisoblagich _hisoblagich;
    private readonly RabbitMqSverkaConsumerWorker _consumerWorker;
    private readonly ILogger<KunlikSverkaMenejeri> _logger;

    public KunlikSverkaMenejeri(
        GzsBillingDbContext dbContext,
        IAbcBankClient abcBankClient,
        UlushTaqsimotHisoblagich hisoblagich,
        RabbitMqSverkaConsumerWorker consumerWorker,
        ILogger<KunlikSverkaMenejeri> logger)
    {
        _dbContext = dbContext;
        _abcBankClient = abcBankClient;
        _hisoblagich = hisoblagich;
        _consumerWorker = consumerWorker;
        _logger = logger;
    }

    public async Task ExecuteProcessKunlikSverkaAsync(DateOnly sana)
    {
        _logger.LogInformation("Starting daily reconciliation for {Date}", sana);

        // Step 1: Fetch all records from 24-hour RabbitMQ local buffer for the target date
        var bufferTranzaktsiyalar = new List<Tranzaksiya>();
        if (_consumerWorker.DailyBuffer.TryGetValue(sana, out var buffered))
        {
            lock (buffered)
            {
                bufferTranzaktsiyalar = buffered.ToList();
            }
        }

        // Also query DB for any transactions that may not have been buffered
        var dbTranzaktsiyalar = await _dbContext.Tranzaktsiyalar
            .Where(t => t.Status == TranzaksiyaStatus.Completed
                        && t.CreatedAt.Date == sana.ToDateTime(TimeOnly.MinValue).Date)
            .ToListAsync();

        var allTranzaktsiyalar = bufferTranzaktsiyalar
            .UnionBy(dbTranzaktsiyalar, t => t.Id)
            .ToList();

        _logger.LogInformation("Fetched {Count} completed transactions for {Date}",
            allTranzaktsiyalar.Count, sana);

        if (allTranzaktsiyalar.Count == 0)
        {
            _logger.LogInformation("No transactions to reconcile for {Date}. Skipping.", sana);
            return;
        }

        // Step 2: Fetch external payment provider settlement data (simulated)
        // In production, this would call the payment provider API
        var paymentProviderTransactions = allTranzaktsiyalar; // Placeholder

        // Step 3: Fetch ABC Bank daily account statement
        AbcBankStatement bankStatement;
        try
        {
            bankStatement = await _abcBankClient.GetDailyStatementAsync(sana);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch ABC Bank statement for {Date}", sana);
            await SaveSverkaXatolikAsync(new SverkaLog
            {
                ReconciliationDate = sana,
                IssueType = SverkaIssueType.MissingInBank,
                Details = $"Bank statement API unavailable: {ex.Message}",
                IsResolved = false
            });
            return;
        }

        // Step 4: 3-way match
        var hasErrors = false;

        foreach (var tranzaksiya in allTranzaktsiyalar)
        {
            var providerMatch = paymentProviderTransactions
                .FirstOrDefault(p => p.Id == tranzaksiya.Id);

            if (providerMatch is null)
            {
                hasErrors = true;
                await SaveSverkaXatolikAsync(new SverkaLog
                {
                    ReconciliationDate = sana,
                    IssueType = SverkaIssueType.MissingInBilling,
                    Details = $"{{\"transaction_id\":\"{tranzaksiya.Id}\",\"amount\":{tranzaksiya.TotalSum},\"reason\":\"Missing in payment provider registry\"}}",
                    IsResolved = false
                });
                continue;
            }

            if (providerMatch.TotalSum != tranzaksiya.TotalSum)
            {
                hasErrors = true;
                await SaveSverkaXatolikAsync(new SverkaLog
                {
                    ReconciliationDate = sana,
                    IssueType = SverkaIssueType.AmountMismatch,
                    Details = $"{{\"transaction_id\":\"{tranzaksiya.Id}\",\"billing_amount\":{tranzaksiya.TotalSum},\"provider_amount\":{providerMatch.TotalSum}}}",
                    IsResolved = false
                });
            }

            var bankMatch = bankStatement.Transactions
                .FirstOrDefault(b => b.Reference == tranzaksiya.Id.ToString());

            if (bankMatch is null)
            {
                hasErrors = true;
                await SaveSverkaXatolikAsync(new SverkaLog
                {
                    ReconciliationDate = sana,
                    IssueType = SverkaIssueType.MissingInBank,
                    Details = $"{{\"transaction_id\":\"{tranzaksiya.Id}\",\"amount\":{tranzaksiya.TotalSum},\"reason\":\"Missing in ABC Bank statement\"}}",
                    IsResolved = false
                });
            }
        }

        // Step 5: If reconciliation verified and clear, generate Schet-faktura
        if (!hasErrors)
        {
            _logger.LogInformation("Reconciliation passed for {Date}. Generating Schet-faktura...", sana);
            var natija = await _hisoblagich.CalculateUlushAndKomissiya(allTranzaktsiyalar);
            await _hisoblagich.GenerateSchetfakturaAsync(natija, sana);
            _logger.LogInformation("Schet-faktura generated successfully for {Date}", sana);
        }
        else
        {
            _logger.LogWarning("Reconciliation for {Date} has discrepancies. Schet-faktura generation skipped.", sana);
        }
    }

    private async Task SaveSverkaXatolikAsync(SverkaLog log)
    {
        _dbContext.SverkaLogs.Add(log);
        await _dbContext.SaveChangesAsync();
        _logger.LogWarning("Reconciliation discrepancy logged: {IssueType} for date {Date}",
            log.IssueType, log.ReconciliationDate);
    }
}
