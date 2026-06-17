namespace TransactionFraudDetection.Domain.Rules;

public interface IFraudRule
{
    string Name { get; }

    FraudRuleResult Evaluate(FraudCheckContext context);
}

public record FraudRuleResult(bool Triggered, string Reason, int Score)
{
    public static FraudRuleResult NotTriggered { get; } = new(false, string.Empty, 0);
}
