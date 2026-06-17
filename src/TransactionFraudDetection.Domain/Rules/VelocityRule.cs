namespace TransactionFraudDetection.Domain.Rules;

public class VelocityRule : IFraudRule
{
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(5);
    private const int MaxTransactionsInWindow = 3;
    private const int RuleScore = 40;

    public string Name => "Velocity";

    public FraudRuleResult Evaluate(FraudCheckContext context)
    {
        var transaction = context.Transaction;
        var windowStart = transaction.Timestamp - Window;

        var recentCount = context.RecentTransactions.Count(t =>
            t.AccountId == transaction.AccountId &&
            t.Timestamp > windowStart &&
            t.Timestamp <= transaction.Timestamp);

        var totalCount = recentCount + 1;
        if (totalCount < MaxTransactionsInWindow)
        {
            return FraudRuleResult.NotTriggered;
        }

        return new FraudRuleResult(
            Triggered: true,
            Reason: $"{totalCount} transactions for account {transaction.AccountId} within the last {Window.TotalMinutes} minutes",
            Score: RuleScore);
    }
}
