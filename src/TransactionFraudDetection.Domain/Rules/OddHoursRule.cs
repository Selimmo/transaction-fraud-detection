namespace TransactionFraudDetection.Domain.Rules;

public class OddHoursRule(TimeSpan? windowStart = null, TimeSpan? windowEnd = null, int ruleScore = 20) : IFraudRule
{
    private readonly TimeSpan _windowStart = windowStart ?? TimeSpan.FromHours(1);
    private readonly TimeSpan _windowEnd = windowEnd ?? TimeSpan.FromHours(5);

    public string Name => "OddHours";

    public FraudRuleResult Evaluate(FraudCheckContext context)
    {
        // DateTimeOffset.TimeOfDay returns the wall-clock time in the timestamp's own
        // offset, not normalized to UTC. A transaction timestamped 02:00 at +05:00 is
        // evaluated as 02:00, not converted to 21:00 UTC.
        var timeOfDay = context.Transaction.Timestamp.TimeOfDay;
        if (timeOfDay < _windowStart || timeOfDay >= _windowEnd)
        {
            return FraudRuleResult.NotTriggered;
        }

        return new FraudRuleResult(
            Triggered: true,
            Reason: $"Transaction occurred at {timeOfDay:hh\\:mm}, within the high-risk overnight window",
            Score: ruleScore);
    }
}
