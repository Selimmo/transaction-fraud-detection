using TransactionFraudDetection.Domain;
using TransactionFraudDetection.Domain.Rules;
using TransactionFraudDetection.Domain.Tests.Support;

namespace TransactionFraudDetection.Domain.Tests.Rules;

public class OddHoursRuleTests
{
    private readonly OddHoursRule _rule = new();

    private static FraudCheckContext ContextAt(int hour, int minute) =>
        new(
            TransactionFactory.Create(timestamp: new DateTimeOffset(2024, 1, 1, hour, minute, 0, TimeSpan.Zero)),
            RecentTransactions: []);

    [Fact]
    public void Does_not_trigger_during_normal_daytime_hours()
    {
        var result = _rule.Evaluate(ContextAt(hour: 12, minute: 0));

        Assert.False(result.Triggered);
    }

    [Fact]
    public void Does_not_trigger_just_before_the_window_starts()
    {
        var result = _rule.Evaluate(ContextAt(hour: 0, minute: 59));

        Assert.False(result.Triggered);
    }

    [Fact]
    public void Triggers_at_the_start_of_the_window()
    {
        var result = _rule.Evaluate(ContextAt(hour: 1, minute: 0));

        Assert.True(result.Triggered);
        Assert.Equal(20, result.Score);
    }

    [Fact]
    public void Triggers_inside_the_window()
    {
        var result = _rule.Evaluate(ContextAt(hour: 3, minute: 0));

        Assert.True(result.Triggered);
    }

    [Fact]
    public void Does_not_trigger_at_the_end_of_the_window()
    {
        var result = _rule.Evaluate(ContextAt(hour: 5, minute: 0));

        Assert.False(result.Triggered);
    }
}
