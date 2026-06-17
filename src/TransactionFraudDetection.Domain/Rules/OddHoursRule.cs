namespace TransactionFraudDetection.Domain.Rules;

public class OddHoursRule : IFraudRule
{
    private static readonly TimeSpan WindowStart = TimeSpan.FromHours(1);
    private static readonly TimeSpan WindowEnd = TimeSpan.FromHours(5);
    private const int RuleScore = 20;

    public string Name => "OddHours";

    public FraudRuleResult Evaluate(FraudCheckContext context)
    {
        var timeOfDay = context.Transaction.Timestamp.TimeOfDay;
        if (timeOfDay < WindowStart || timeOfDay >= WindowEnd)
        {
            return FraudRuleResult.NotTriggered;
        }

        return new FraudRuleResult(
            Triggered: true,
            Reason: $"Transaction occurred at {timeOfDay:hh\\:mm}, within the high-risk overnight window",
            Score: RuleScore);
    }
}
