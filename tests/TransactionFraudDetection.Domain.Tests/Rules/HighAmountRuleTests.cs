using TransactionFraudDetection.Domain;
using TransactionFraudDetection.Domain.Rules;
using TransactionFraudDetection.Domain.Tests.Support;

namespace TransactionFraudDetection.Domain.Tests.Rules;

public class HighAmountRuleTests
{
    private readonly HighAmountRule _rule = new();

    [Fact]
    public void Does_not_trigger_below_threshold()
    {
        var context = new FraudCheckContext(
            TransactionFactory.Create(amount: 4999.99m),
            RecentTransactions: []);

        var result = _rule.Evaluate(context);

        Assert.False(result.Triggered);
        Assert.Equal(0, result.Score);
    }

    [Fact]
    public void Does_not_trigger_at_threshold()
    {
        var context = new FraudCheckContext(
            TransactionFactory.Create(amount: 5000m),
            RecentTransactions: []);

        var result = _rule.Evaluate(context);

        Assert.False(result.Triggered);
    }

    [Fact]
    public void Triggers_above_threshold_with_score_and_reason()
    {
        var context = new FraudCheckContext(
            TransactionFactory.Create(amount: 5000.01m),
            RecentTransactions: []);

        var result = _rule.Evaluate(context);

        Assert.True(result.Triggered);
        Assert.Equal(50, result.Score);
        Assert.False(string.IsNullOrWhiteSpace(result.Reason));
    }
}
