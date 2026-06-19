namespace TransactionFraudDetection.Domain.Rules;

public class VelocityRule(TimeSpan? window = null, int maxTransactionsInWindow = 3, int ruleScore = 40) : IFraudRule
{
    private readonly TimeSpan _window = window ?? TimeSpan.FromMinutes(5);

    public string Name => "Velocity";

    public FraudRuleResult Evaluate(FraudCheckContext context)
    {
        var transaction = context.Transaction;
        var windowStart = transaction.Timestamp - _window;

        var recentCount = context.RecentTransactions.Count(t =>
            t.AccountId == transaction.AccountId &&
            t.Timestamp > windowStart &&
            t.Timestamp <= transaction.Timestamp);

        var totalCount = recentCount + 1;
        if (totalCount < maxTransactionsInWindow)
        {
            return FraudRuleResult.NotTriggered;
        }

        return new FraudRuleResult(
            Triggered: true,
            Reason: $"{totalCount} transactions for account {transaction.AccountId} within the last {_window.TotalMinutes} minutes",
            Score: ruleScore);
    }
}
