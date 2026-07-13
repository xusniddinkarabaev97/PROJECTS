using System.ComponentModel.DataAnnotations;

namespace GzsBilling.Api.Models;

/// <summary>
/// Request payload for the test transaction imitation endpoint.
/// </summary>
public class TestImitationRequest
{
    [Required(ErrorMessage = "Amount is required")]
    [Range(100, 999999999, ErrorMessage = "Amount must be between 100 and 999,999,999")]
    public decimal Amount { get; set; }

    public string? Currency { get; set; } = "UZS";

    [Required(ErrorMessage = "Payment system is required")]
    [StringLength(50)]
    public string? PaymentSystem { get; set; }

    [StringLength(20)]
    public string? CarNumber { get; set; }

    public Dictionary<string, string>? Metadata { get; set; }
}

/// <summary>
/// Response returned after a test transaction is successfully created.
/// </summary>
public class TestTransactionResponse
{
    public string? TransactionId { get; set; }
    public string? PaymentSystem { get; set; }
    public decimal Amount { get; set; }
    public string? Currency { get; set; }
    public string? CarNumber { get; set; }
    public string? Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public bool IsTestMode { get; set; }
}
