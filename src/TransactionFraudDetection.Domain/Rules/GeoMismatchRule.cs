namespace TransactionFraudDetection.Domain.Rules;

public class GeoMismatchRule : IFraudRule
{
    private const int RuleScore = 30;

    public string Name => "GeoMismatch";

    public FraudRuleResult Evaluate(FraudCheckContext context)
    {
        var transaction = context.Transaction;
        if (string.Equals(transaction.MerchantCountry, transaction.AccountHomeCountry, StringComparison.OrdinalIgnoreCase))
        {
            return FraudRuleResult.NotTriggered;
        }

        return new FraudRuleResult(
            Triggered: true,
            Reason: $"Merchant country {transaction.MerchantCountry} differs from account home country {transaction.AccountHomeCountry}",
            Score: RuleScore);
    }
}
