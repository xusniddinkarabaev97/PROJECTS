using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace GzsBilling.Infrastructure.Clients;

public class AbcBankClient : IAbcBankClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AbcBankClient> _logger;
    private readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy;

    public AbcBankClient(HttpClient httpClient, ILogger<AbcBankClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        _retryPolicy = Policy
            .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
            .Or<HttpRequestException>()
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    }

    public async Task<AbcBankStatement> GetDailyStatementAsync(DateOnly date)
    {
        var requestUri = $"/api/v1/statements/daily?date={date:yyyy-MM-dd}";
        _logger.LogInformation("Fetching ABC Bank statement for {Date}", date);

        var response = await _retryPolicy.ExecuteAsync(() => _httpClient.GetAsync(requestUri));
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var statement = JsonSerializer.Deserialize<AbcBankStatement>(content)
            ?? new AbcBankStatement { StatementDate = date };

        return statement;
    }

    public async Task<string> SendDisbursementAsync(string bankAccount, decimal amount, string reference)
    {
        var payload = new
        {
            bank_account = bankAccount,
            amount = amount,
            reference = reference,
            currency = "UZS"
        };

        var json = JsonSerializer.Serialize(payload);
        var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

        _logger.LogInformation("Sending disbursement: Account={Account}, Amount={Amount}, Ref={Ref}",
            bankAccount, amount, reference);

        var response = await _retryPolicy.ExecuteAsync(() =>
            _httpClient.PostAsync("/api/v1/disbursements", httpContent));

        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
        var bankRef = result.GetProperty("transaction_id").GetString() ?? reference;

        _logger.LogInformation("Disbursement successful. Bank reference: {BankRef}", bankRef);
        return bankRef;
    }
}
