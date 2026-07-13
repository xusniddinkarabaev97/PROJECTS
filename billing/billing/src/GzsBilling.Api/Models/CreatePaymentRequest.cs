using System.ComponentModel.DataAnnotations;

namespace GzsBilling.Api.Models;

public class CreatePaymentRequest
{
    [Required]
    [Range(0.01, 999999999999.99)]
    public decimal Amount { get; set; }

    [Required]
    [StringLength(3, MinimumLength = 3)]
    public string? Currency { get; set; } = "UZS";

    [Required]
    [StringLength(50)]
    public string? PaymentSystem { get; set; }

    public string? Description { get; set; }
}

public record TransactionResponse
{
    public Guid Id { get; set; }
    public string? TransactionId { get; set; }
    public string? Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public decimal Amount { get; set; }
    public string? Currency { get; set; }
}

public class DashboardResponse
{
    public int TotalTransactions { get; set; }
    public decimal TodayTotalAmount { get; set; }
    public decimal SuccessfulRate { get; set; }
    public int ActiveContragents { get; set; }
}
