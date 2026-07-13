namespace GzsBilling.Application.Commands.Refunds;

/// <summary>
/// Thrown when a refund fails one or more fraud checks.
/// </summary>
public class FraudCheckFailedException : Exception
{
    public IReadOnlyList<string> Reasons { get; }

    public FraudCheckFailedException(IReadOnlyList<string> reasons)
        : base($"Fraud check failed: {string.Join("; ", reasons)}")
    {
        Reasons = reasons;
    }
}
