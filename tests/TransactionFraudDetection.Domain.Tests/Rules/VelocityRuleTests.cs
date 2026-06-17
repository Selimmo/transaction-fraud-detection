using TransactionFraudDetection.Domain;
using TransactionFraudDetection.Domain.Rules;
using TransactionFraudDetection.Domain.Tests.Support;

namespace TransactionFraudDetection.Domain.Tests.Rules;

public class VelocityRuleTests
{
    private static readonly DateTimeOffset Now = new(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly VelocityRule _rule = new();

    [Fact]
    public void Does_not_trigger_with_no_recent_transactions()
    {
        var context = new FraudCheckContext(
            TransactionFactory.Create(timestamp: Now),
            RecentTransactions: []);

        var result = _rule.Evaluate(context);

        Assert.False(result.Triggered);
    }

    [Fact]
    public void Does_not_trigger_with_only_one_prior_transaction_in_window()
    {
        var context = new FraudCheckContext(
            TransactionFactory.Create(accountId: "acct-1", timestamp: Now),
            RecentTransactions:
            [
                TransactionFactory.Create(accountId: "acct-1", timestamp: Now.AddMinutes(-1)),
            ]);

        var result = _rule.Evaluate(context);

        Assert.False(result.Triggered);
    }

    [Fact]
    public void Triggers_with_two_prior_transactions_in_window()
    {
        var context = new FraudCheckContext(
            TransactionFactory.Create(accountId: "acct-1", timestamp: Now),
            RecentTransactions:
            [
                TransactionFactory.Create(accountId: "acct-1", timestamp: Now.AddMinutes(-1)),
                TransactionFactory.Create(accountId: "acct-1", timestamp: Now.AddMinutes(-2)),
            ]);

        var result = _rule.Evaluate(context);

        Assert.True(result.Triggered);
        Assert.Equal(40, result.Score);
    }

    [Fact]
    public void Ignores_transactions_for_other_accounts()
    {
        var context = new FraudCheckContext(
            TransactionFactory.Create(accountId: "acct-1", timestamp: Now),
            RecentTransactions:
            [
                TransactionFactory.Create(accountId: "acct-2", timestamp: Now.AddMinutes(-1)),
                TransactionFactory.Create(accountId: "acct-2", timestamp: Now.AddMinutes(-2)),
            ]);

        var result = _rule.Evaluate(context);

        Assert.False(result.Triggered);
    }

    [Fact]
    public void Ignores_transactions_outside_the_window()
    {
        var context = new FraudCheckContext(
            TransactionFactory.Create(accountId: "acct-1", timestamp: Now),
            RecentTransactions:
            [
                TransactionFactory.Create(accountId: "acct-1", timestamp: Now.AddMinutes(-10)),
                TransactionFactory.Create(accountId: "acct-1", timestamp: Now.AddMinutes(-20)),
            ]);

        var result = _rule.Evaluate(context);

        Assert.False(result.Triggered);
    }

    [Fact]
    public void Excludes_a_prior_transaction_exactly_at_the_window_boundary()
    {
        var context = new FraudCheckContext(
            TransactionFactory.Create(accountId: "acct-1", timestamp: Now),
            RecentTransactions:
            [
                TransactionFactory.Create(accountId: "acct-1", timestamp: Now.AddMinutes(-5)),
                TransactionFactory.Create(accountId: "acct-1", timestamp: Now.AddMinutes(-1)),
            ]);

        var result = _rule.Evaluate(context);

        Assert.False(result.Triggered);
    }
}
