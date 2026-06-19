namespace TransactionFraudDetection.Domain.Rules;

public class HighAmountRule(decimal threshold = 5000m, int ruleScore = 50) : IFraudRule
{
    public string Name => "HighAmount";

    public FraudRuleResult Evaluate(FraudCheckContext context)
    {
        var transaction = context.Transaction;
        if (transaction.Amount <= threshold)
        {
            return FraudRuleResult.NotTriggered;
        }

        return new FraudRuleResult(
            Triggered: true,
            Reason: $"Amount {transaction.Amount} {transaction.Currency} exceeds high-value threshold of {threshold}",
            Score: ruleScore);
    }
}
