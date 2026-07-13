using MediatR;

namespace GzsBilling.Application.Commands.Disputes;

/// <summary>
/// Command to create a new billing dispute against a transaction.
/// </summary>
public class CreateDisputeCommand : IRequest<string>
{
    public string TransactionId { get; }
    public string ContragentId { get; }
    public decimal Amount { get; }
    public string Reason { get; }
    public string CreatedBy { get; }

    public CreateDisputeCommand(
        string transactionId,
        string contragentId,
        decimal amount,
        string reason,
        string createdBy)
    {
        TransactionId = transactionId;
        ContragentId = contragentId;
        Amount = amount;
        Reason = reason;
        CreatedBy = createdBy;
    }
}
