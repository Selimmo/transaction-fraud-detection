namespace TransactionFraudDetection.Domain.Rules;

public class HighAmountRule : IFraudRule
{
    private const decimal Threshold = 5000m;
    private const int RuleScore = 50;

    public string Name => "HighAmount";

    public FraudRuleResult Evaluate(FraudCheckContext context)
    {
        var transaction = context.Transaction;
        if (transaction.Amount <= Threshold)
        {
            return FraudRuleResult.NotTriggered;
        }

        return new FraudRuleResult(
            Triggered: true,
            Reason: $"Amount {transaction.Amount} {transaction.Currency} exceeds high-value threshold of {Threshold}",
            Score: RuleScore);
    }
}
