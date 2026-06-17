using TransactionFraudDetection.Domain.Rules;
using TransactionFraudDetection.Domain.Tests.Support;

namespace TransactionFraudDetection.Domain.Tests;

public class FraudDetectionEngineTests
{
    private static FraudCheckContext AnyContext() =>
        new(TransactionFactory.Create(), RecentTransactions: []);

    [Fact]
    public void Not_fraudulent_with_no_rules()
    {
        var engine = new FraudDetectionEngine(rules: [], riskThreshold: 50);

        var result = engine.Evaluate(AnyContext());

        Assert.False(result.IsFraudulent);
        Assert.Equal(0, result.RiskScore);
        Assert.Empty(result.TriggeredReasons);
    }

    [Fact]
    public void Not_fraudulent_when_no_rules_trigger()
    {
        var engine = new FraudDetectionEngine(
            rules: [new FakeRule("A", FraudRuleResult.NotTriggered), new FakeRule("B", FraudRuleResult.NotTriggered)],
            riskThreshold: 50);

        var result = engine.Evaluate(AnyContext());

        Assert.False(result.IsFraudulent);
        Assert.Equal(0, result.RiskScore);
        Assert.Empty(result.TriggeredReasons);
    }

    [Fact]
    public void Sums_scores_of_triggered_rules_but_stays_not_fraudulent_below_threshold()
    {
        var engine = new FraudDetectionEngine(
            rules:
            [
                new FakeRule("A", new FraudRuleResult(true, "reason-a", 20)),
                new FakeRule("B", FraudRuleResult.NotTriggered),
            ],
            riskThreshold: 50);

        var result = engine.Evaluate(AnyContext());

        Assert.False(result.IsFraudulent);
        Assert.Equal(20, result.RiskScore);
        Assert.Equal(new[] { "reason-a" }, result.TriggeredReasons);
    }

    [Fact]
    public void Fraudulent_when_combined_score_meets_threshold()
    {
        var engine = new FraudDetectionEngine(
            rules:
            [
                new FakeRule("A", new FraudRuleResult(true, "reason-a", 30)),
                new FakeRule("B", new FraudRuleResult(true, "reason-b", 20)),
            ],
            riskThreshold: 50);

        var result = engine.Evaluate(AnyContext());

        Assert.True(result.IsFraudulent);
        Assert.Equal(50, result.RiskScore);
        Assert.Equal(new[] { "reason-a", "reason-b" }, result.TriggeredReasons);
    }
}
