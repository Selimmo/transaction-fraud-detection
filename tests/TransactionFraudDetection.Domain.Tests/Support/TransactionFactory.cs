using TransactionFraudDetection.Domain.Models;

namespace TransactionFraudDetection.Domain.Tests.Support;

internal static class TransactionFactory
{
    public static Transaction Create(
        string id = "txn-1",
        string accountId = "acct-1",
        decimal amount = 100m,
        string currency = "USD",
        DateTimeOffset? timestamp = null,
        string merchantCountry = "US",
        string accountHomeCountry = "US") =>
        new(
            id,
            accountId,
            amount,
            currency,
            timestamp ?? new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero),
            merchantCountry,
            accountHomeCountry);
}
