using System.Text.Json;
using GzsBilling.Domain.Entities;
using GzsBilling.Domain.Models;
using GzsBilling.Infrastructure.Data;
using GzsBilling.Infrastructure.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GzsBilling.Sverka.Calculation;

public class UlushTaqsimotHisoblagich
{
    private readonly GzsBillingDbContext _dbContext;
    private readonly ISystemSettingService _settingsService;
    private readonly ILogger<UlushTaqsimotHisoblagich> _logger;

    public UlushTaqsimotHisoblagich(
        GzsBillingDbContext dbContext,
        ISystemSettingService settingsService,
        ILogger<UlushTaqsimotHisoblagich> logger)
    {
        _dbContext = dbContext;
        _settingsService = settingsService;
        _logger = logger;
    }

    /// <summary>
    /// Calculates commission splits and stakeholder distributions
    /// based on the invoice formulas. Commission rates and split ratios
    /// come from the CommissionRates system setting (keyed by PaymentId).
    ///
    /// Formulas (per payment provider):
    ///   C_total = sum(tx.TotalSum * commissionRate) for all tx
    ///   S_net   = S_total - C_total
    ///   BankSplit    = sum(commission * bankSplitRate)
    ///   PlatformSplit = sum(commission * platformSplitRate)
    ///
    /// Stakeholder Split:
    ///   stakeholder_payout = S_net_for_group * (SharePercent / 100)
    /// </summary>
    public async Task<SchetfakturaNatija> CalculateUlushAndKomissiya(List<Tranzaksiya> kunlikTranzaksiyalar)
    {
        var natija = new SchetfakturaNatija();

        if (kunlikTranzaksiyalar.Count == 0)
            return natija;

        // Get commission config from system_settings
        var commissionJson = await _settingsService.GetSettingValueAsync("CommissionRates");
        var commissionConfig = string.IsNullOrEmpty(commissionJson)
            ? new Dictionary<string, CommissionRateConfig>()
            : JsonSerializer.Deserialize<Dictionary<string, CommissionRateConfig>>(commissionJson)
              ?? new Dictionary<string, CommissionRateConfig>();

        var defaultConfig = commissionConfig.GetValueOrDefault("default")
            ?? new CommissionRateConfig { CommissionRate = 0.01m, BankSplitRate = 0.50m, PlatformSplitRate = 0.50m };

        var totalAmount = kunlikTranzaksiyalar.Sum(t => t.TotalSum);

        // Commission and bank/platform splits are per payment provider
        var systemCommission = 0m;
        var bankSplit = 0m;
        var platformSplit = 0m;

        foreach (var group in kunlikTranzaksiyalar.GroupBy(t => t.PaymentId))
        {
            var paymentKey = group.Key.ToString();
            var config = commissionConfig.GetValueOrDefault(paymentKey) ?? defaultConfig;

            var groupTotal = group.Sum(t => t.TotalSum);
            var groupCommission = Math.Round(groupTotal * config.CommissionRate, 2);
            systemCommission += groupCommission;

            bankSplit += Math.Round(groupCommission * config.BankSplitRate, 2);
            platformSplit += Math.Round(groupCommission * config.PlatformSplitRate, 2);
        }

        var netDistribution = Math.Round(totalAmount - systemCommission, 2);

        natija.TotalAmount = totalAmount;
        natija.SystemCommission = systemCommission;
        natija.NetDistributionAmount = netDistribution;
        natija.BankSplit = bankSplit;
        natija.PlatformSplit = platformSplit;

        _logger.LogInformation(
            "Commission split: Bank={BankSplit}, Platform={PlatformSplit}, Total={TotalCommission}",
            bankSplit, platformSplit, systemCommission);

        // Stakeholder split: Look up stakeholders by FillingStationId and PaymentId
        var groupedByStation = kunlikTranzaksiyalar
            .GroupBy(t => new { t.FillingStationId, t.PaymentId });

        foreach (var group in groupedByStation)
        {
            var stakeholders = await _dbContext.Stakeholders
                .Where(s => s.FillingStationId == group.Key.FillingStationId
                            && s.PaymentId == group.Key.PaymentId)
                .ToListAsync();

            var groupTotal = group.Sum(t => t.TotalSum);

            // Net distribution rate = 1.0 - commissionRate for this specific payment
            var paymentKey = group.Key.PaymentId.ToString();
            var config = commissionConfig.GetValueOrDefault(paymentKey) ?? defaultConfig;
            var netRate = 1m - config.CommissionRate;
            var groupNet = Math.Round(groupTotal * netRate, 2);

            foreach (var stakeholder in stakeholders)
            {
                var payout = Math.Round(groupNet * (stakeholder.SharePercent / 100m), 2);

                natija.StakeholderPayouts.Add(new StakeholderLineItem
                {
                    StakeholderId = stakeholder.Id,
                    FullName = stakeholder.FullName,
                    BankAccount = stakeholder.BankAccount,
                    SharePercent = stakeholder.SharePercent,
                    PayoutAmount = payout
                });

                _logger.LogDebug(
                    "Stakeholder {Name} (Share: {Share}%): Payout = {Payout}",
                    stakeholder.FullName, stakeholder.SharePercent, payout);
            }
        }

        return natija;
    }

    private class CommissionRateConfig
    {
        public decimal CommissionRate { get; set; }
        public decimal BankSplitRate { get; set; }
        public decimal PlatformSplitRate { get; set; }
    }

    /// <summary>
    /// Serializes the calculation output, generates a digital Schet-faktura (invoice),
    /// and persists it to the database with authorization status.
    /// </summary>
    public async Task GenerateSchetfakturaAsync(SchetfakturaNatija natija, DateOnly sana)
    {
        var calculationJson = System.Text.Json.JsonSerializer.Serialize(natija, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
        });

        var schetfaktura = new Schetfaktura
        {
            Id = Guid.NewGuid(),
            InvoiceDate = sana,
            TotalAmount = natija.TotalAmount,
            SystemCommission = natija.SystemCommission,
            NetDistributionAmount = natija.NetDistributionAmount,
            CalculationJson = calculationJson,
            IsAuthorized = true,
            IsPaid = false
        };

        _dbContext.Schetfakturalar.Add(schetfaktura);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Schet-faktura {Id} generated: Total={Total}, Commission={Commission}, Net={Net}",
            schetfaktura.Id, schetfaktura.TotalAmount,
            schetfaktura.SystemCommission, schetfaktura.NetDistributionAmount);
    }
}
