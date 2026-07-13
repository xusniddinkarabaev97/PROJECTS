using GzsBilling.Domain.Models;

namespace GzsBilling.Infrastructure.Clients;

public class AbcBankStatement
{
    public DateOnly StatementDate { get; set; }
    public List<AbcBankTransaction> Transactions { get; set; } = new();
}

public class AbcBankTransaction
{
    public string Reference { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

public interface IAbcBankClient
{
    Task<AbcBankStatement> GetDailyStatementAsync(DateOnly date);
    Task<string> SendDisbursementAsync(string bankAccount, decimal amount, string reference);
}
