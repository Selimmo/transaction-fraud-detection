using TransactionFraudDetection.Domain;
using TransactionFraudDetection.Domain.Models;

namespace TransactionFraudDetection.Worker.Tests;

public class FraudExplanationPromptBuilderTests
{
    private static FraudCheckNotification Notification(int riskScore, IReadOnlyList<string> reasons) =>
        new(
            Transaction: new Transaction(
                Id: "txn-1",
                AccountId: "acct-1",
                Amount: 7500.00m,
                Currency: "USD",
                Timestamp: DateTimeOffset.Parse("2024-01-01T12:00:00Z"),
                MerchantCountry: "RU",
                AccountHomeCountry: "US"),
            Result: new FraudCheckResult(IsFraudulent: true, RiskScore: riskScore, TriggeredReasons: reasons));

    [Fact]
    public void Build_includes_transaction_amount_and_currency()
    {
        var prompt = FraudExplanationPromptBuilder.Build(Notification(80, ["high amount", "geo mismatch"]));

        Assert.Contains("7500", prompt);
        Assert.Contains("USD", prompt);
    }

    [Fact]
    public void Build_includes_triggered_reasons()
    {
        var prompt = FraudExplanationPromptBuilder.Build(Notification(80, ["high amount", "geo mismatch"]));

        Assert.Contains("high amount", prompt);
        Assert.Contains("geo mismatch", prompt);
    }

    [Fact]
    public void Build_includes_risk_score()
    {
        var prompt = FraudExplanationPromptBuilder.Build(Notification(80, ["high amount"]));

        Assert.Contains("80", prompt);
    }
}
