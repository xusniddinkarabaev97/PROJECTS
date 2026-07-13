using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using GzsBilling.Domain.Entities;

namespace GzsBilling.Application.Services;

public interface ITransactionStore
{
    void AddTransaction(Transaction transaction);
    Transaction? GetByTransactionId(string transactionId);
    List<Transaction> GetByDate(DateTimeOffset date);
}

public class InMemoryTransactionStore : ITransactionStore
{
    private readonly ConcurrentDictionary<string, Transaction> _transactions = new();

    public void AddTransaction(Transaction transaction)
    {
        _transactions[transaction.TransactionId] = transaction;
    }

    public Transaction? GetByTransactionId(string transactionId)
    {
        _transactions.TryGetValue(transactionId, out var transaction);
        return transaction;
    }

    public List<Transaction> GetByDate(DateTimeOffset date)
    {
        var startOfDay = new DateTimeOffset(date.Year, date.Month, date.Day, 0, 0, 0, date.Offset);
        var endOfDay = startOfDay.AddDays(1);

        return _transactions.Values
            .Where(t => t.CreatedAt >= startOfDay && t.CreatedAt < endOfDay)
            .ToList();
    }
}

public class ReconciliationService
{
    private readonly ITransactionStore _transactionStore;
    private readonly ILogger<ReconciliationService> _logger;

    public ReconciliationService(ITransactionStore transactionStore, ILogger<ReconciliationService> logger)
    {
        _transactionStore = transactionStore;
        _logger = logger;
    }

    public async Task<ReconciliationReport> ReconcileAsync(string provider, DateTimeOffset date)
    {
        _logger.LogInformation(
            "Reconciliation started for Provider={Provider}, Date={Date}",
            provider, date.ToString("yyyy-MM-dd"));

        var report = new ReconciliationReport
        {
            Id = Guid.NewGuid(),
            ReportDate = date,
            Provider = provider,
            Status = ReconciliationStatus.InProgress,
            GeneratedAt = DateTimeOffset.UtcNow
        };

        try
        {
            var billingTransactions = _transactionStore.GetByDate(date);
            report.BillingTransactionCount = billingTransactions.Count;
            report.BillingTotalAmount = billingTransactions.Sum(t => t.Amount);

            _logger.LogInformation(
                "Billing transactions loaded: Count={Count}, TotalAmount={TotalAmount}",
                report.BillingTransactionCount, report.BillingTotalAmount);

            var providerTransactions = await SimulateProviderDataAsync(provider, date);
            report.ProviderTransactionCount = providerTransactions.Count;
            report.ProviderTotalAmount = providerTransactions.Sum(t => t.Amount);

            _logger.LogInformation(
                "Provider transactions loaded: Count={Count}, TotalAmount={TotalAmount}",
                report.ProviderTransactionCount, report.ProviderTotalAmount);

            var discrepancies = new List<DiscrepancyItem>();

            var billingMap = billingTransactions.ToDictionary(t => t.TransactionId);
            var providerMap = providerTransactions.ToDictionary(t => t.TransactionId);

            foreach (var billingTxn in billingTransactions)
            {
                if (providerMap.TryGetValue(billingTxn.TransactionId, out var providerTxn))
                {
                    if (billingTxn.Amount != providerTxn.Amount)
                    {
                        discrepancies.Add(new DiscrepancyItem
                        {
                            TransactionId = billingTxn.TransactionId,
                            Type = "AmountMismatch",
                            BillingAmount = billingTxn.Amount,
                            ProviderAmount = providerTxn.Amount,
                            Difference = billingTxn.Amount - providerTxn.Amount,
                            Notes = string.Format(
                                "Amount mismatch: billing={0:N2}, provider={1:N2}",
                                billingTxn.Amount, providerTxn.Amount)
                        });
                    }
                }
                else
                {
                    discrepancies.Add(new DiscrepancyItem
                    {
                        TransactionId = billingTxn.TransactionId,
                        Type = "MissingInProvider",
                        BillingAmount = billingTxn.Amount,
                        ProviderAmount = 0,
                        Difference = billingTxn.Amount,
                        Notes = "Transaction exists in billing but missing from provider"
                    });
                }
            }

            foreach (var providerTxn in providerTransactions)
            {
                if (!billingMap.ContainsKey(providerTxn.TransactionId))
                {
                    discrepancies.Add(new DiscrepancyItem
                    {
                        TransactionId = providerTxn.TransactionId,
                        Type = "MissingInBilling",
                        BillingAmount = 0,
                        ProviderAmount = providerTxn.Amount,
                        Difference = -providerTxn.Amount,
                        Notes = "Transaction exists in provider but missing from billing"
                    });
                }
            }

            report.DiscrepancyCount = discrepancies.Count;
            report.DiscrepancyAmount = discrepancies.Sum(d => Math.Abs(d.Difference));

            var totalTransactionValue = report.BillingTotalAmount + report.ProviderTotalAmount;
            if (totalTransactionValue > 0)
            {
                report.DiscrepancyPercentage = report.DiscrepancyAmount / totalTransactionValue * 100;
            }

            report.ThresholdExceeded = report.DiscrepancyPercentage > report.ThresholdPercentage;

            report.DiscrepancyDetails = System.Text.Json.JsonSerializer.Serialize(discrepancies);

            if (report.ThresholdExceeded)
            {
                _logger.LogWarning(
                    "RECONCILIATION_ALERT: Threshold exceeded! Provider={Provider}, DiscrepancyPercentage={DiscrepancyPercentage:F4}%, Threshold={ThresholdPercentage:F4}%",
                    provider, report.DiscrepancyPercentage, report.ThresholdPercentage);

                report.Status = ReconciliationStatus.DiscrepanciesFound;
            }
            else
            {
                report.Status = ReconciliationStatus.Completed;
            }

            _logger.LogInformation(
                "Reconciliation completed for Provider={Provider}: Status={Status}, Discrepancies={DiscrepancyCount}, DiscrepancyAmount={DiscrepancyAmount:N2}, Percentage={DiscrepancyPercentage:F4}%",
                provider, report.Status, report.DiscrepancyCount, report.DiscrepancyAmount, report.DiscrepancyPercentage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Reconciliation failed for Provider={Provider}, Date={Date}",
                provider, date.ToString("yyyy-MM-dd"));

            report.Status = ReconciliationStatus.Failed;
            report.DiscrepancyDetails = System.Text.Json.JsonSerializer.Serialize(new
            {
                error = ex.Message,
                stackTrace = ex.StackTrace
            });
        }

        return report;
    }

    private Task<List<Transaction>> SimulateProviderDataAsync(string provider, DateTimeOffset date)
    {
        var billingTransactions = _transactionStore.GetByDate(date);

        var providerTransactions = new List<Transaction>();

        foreach (var billingTxn in billingTransactions)
        {
            providerTransactions.Add(new Transaction
            {
                Id = Guid.NewGuid(),
                TransactionId = billingTxn.TransactionId,
                ContragentId = billingTxn.ContragentId,
                PaymentSystem = provider,
                Amount = billingTxn.Amount,
                Currency = billingTxn.Currency,
                Status = TransactionStatus.Completed,
                CreatedAt = billingTxn.CreatedAt,
                ProcessedAt = billingTxn.ProcessedAt
            });
        }

        _logger.LogInformation(
            "Simulated {Count} provider transactions for Provider={Provider}, Date={Date}",
            providerTransactions.Count, provider, date.ToString("yyyy-MM-dd"));

        return Task.FromResult(providerTransactions);
    }
}
